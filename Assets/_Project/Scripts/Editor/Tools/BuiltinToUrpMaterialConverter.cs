using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NovaStriker.EditorTools.Tools
{
    /// <summary>
    /// Convierte a URP los materiales del Soccer Stadium Pack que usan shaders del Built-in Render Pipeline
    /// (Mobile/VertexLit, Legacy, Unlit...), que en URP se ven morados y que el Render Pipeline Converter no cubre.
    /// Conserva textura, tiling, color y modo de transparencia. Admite Deshacer (Ctrl+Z).
    /// No toca el shader propio del paquete (público animado), los skybox ni los materiales que ya son de URP.
    /// </summary>
    internal static class BuiltinToUrpMaterialConverter
    {
        private const string Folder = "Assets/SoccerStadiumPack";
        private const string LitShaderName = "Universal Render Pipeline/Simple Lit";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

        private static readonly string[] KeepPrefixes =
        {
            "Universal Render Pipeline/",
            "Shader Graphs/",
            "Custom/SoccerStadiumPack/",
            "Skybox/",
        };

        private enum Surface { Opaque, Cutout, Transparent }

        [MenuItem("NovaStriker/Arte/Convertir materiales del estadio a URP")]
        private static void Run()
        {
            Shader lit = Shader.Find(LitShaderName);
            Shader unlit = Shader.Find(UnlitShaderName);
            if (lit == null || unlit == null)
            {
                Debug.LogError($"[NovaStriker Arte] No encontré los shaders '{LitShaderName}' o '{UnlitShaderName}'. ¿Está instalado URP?");
                return;
            }

            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Debug.LogError($"[NovaStriker Arte] No existe la carpeta {Folder}.");
                return;
            }

            var log = new StringBuilder($"[NovaStriker Arte] Materiales de {Folder} convertidos a URP:\n");
            int converted = 0;
            int kept = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    continue;
                }

                string oldShader = material.shader != null ? material.shader.name : "(sin shader)";
                if (!NeedsConversion(oldShader))
                {
                    kept++;
                    continue;
                }

                Surface surface = Convert(material, oldShader, lit, unlit);
                log.AppendLine($"- {path}: {oldShader} → {material.shader.name} ({surface})");
                converted++;
            }

            AssetDatabase.SaveAssets();
            log.AppendLine($"Convertidos: {converted}. Sin cambios (ya compatibles): {kept}.");
            Debug.Log(log.ToString());
        }

        private static bool NeedsConversion(string shaderName)
        {
            foreach (string prefix in KeepPrefixes)
            {
                if (shaderName.StartsWith(prefix))
                {
                    return false;
                }
            }

            return true;
        }

        private static Surface Convert(Material material, string oldShader, Shader lit, Shader unlit)
        {
            // Leer todo ANTES de cambiar el shader: al cambiarlo, las propiedades que el nuevo no tiene dejan de leerse.
            Texture texture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            Vector2 scale = material.HasProperty("_MainTex") ? material.GetTextureScale("_MainTex") : Vector2.one;
            Vector2 offset = material.HasProperty("_MainTex") ? material.GetTextureOffset("_MainTex") : Vector2.zero;
            Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            float cutoff = material.HasProperty("_Cutoff") ? material.GetFloat("_Cutoff") : 0.5f;

            bool premultiply = material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
            Surface surface = oldShader.Contains("Cutout") || material.IsKeywordEnabled("_ALPHATEST_ON")
                ? Surface.Cutout
                : oldShader.Contains("Transparent") || premultiply || material.IsKeywordEnabled("_ALPHABLEND_ON")
                    ? Surface.Transparent
                    : Surface.Opaque;

            Undo.RecordObject(material, "Convertir material a URP");
            material.shader = oldShader.Contains("Unlit") ? unlit : lit;
            material.shaderKeywords = new string[0];

            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", scale);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetColor("_BaseColor", color);
            SetFloat(material, "_Cutoff", cutoff);

            switch (surface)
            {
                case Surface.Cutout:
                    SetFloat(material, "_Surface", 0f);
                    SetFloat(material, "_AlphaClip", 1f);
                    SetBlend(material, BlendMode.One, BlendMode.Zero, zWrite: true);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                    break;

                case Surface.Transparent:
                    SetFloat(material, "_Surface", 1f);
                    SetFloat(material, "_Blend", premultiply ? 1f : 0f);
                    SetFloat(material, "_AlphaClip", 0f);
                    SetBlend(material, premultiply ? BlendMode.One : BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, zWrite: false);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    if (premultiply)
                    {
                        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    }
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;

                default:
                    SetFloat(material, "_Surface", 0f);
                    SetFloat(material, "_AlphaClip", 0f);
                    SetBlend(material, BlendMode.One, BlendMode.Zero, zWrite: true);
                    material.SetOverrideTag("RenderType", "Opaque");
                    material.renderQueue = -1; // El del shader.
                    break;
            }

            EditorUtility.SetDirty(material);
            return surface;
        }

        private static void SetBlend(Material material, BlendMode source, BlendMode destination, bool zWrite)
        {
            SetFloat(material, "_SrcBlend", (float)source);
            SetFloat(material, "_DstBlend", (float)destination);
            SetFloat(material, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloat(material, "_DstBlendAlpha", (float)destination);
            SetFloat(material, "_ZWrite", zWrite ? 1f : 0f);
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }
    }
}
