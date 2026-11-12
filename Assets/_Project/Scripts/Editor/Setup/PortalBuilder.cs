using System.Text;
using NovaStriker.Contracts.Data;
using NovaStriker.Gameplay.Obstacles.Portals;
using NovaStriker.Infrastructure.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Genera PF_PortalPair: dos portales unidos (azul A y naranja B). El balón que entra por uno sale por el centro
    /// del otro con la misma velocidad y dirección. Además:
    ///  - crea la capa 14 "Portal", que solo choca con la capa Ball (cajas, futbolistas y fragmentos la atraviesan);
    ///  - añade el sonido Portal a SO_SoundLibrary (usa los clips de Whoosh; cámbialos ahí si quieres otro).
    /// Jerarquía:
    ///   PF_PortalPair (PortalPair)
    ///   ├─ Portal_A  trigger esférico + Rigidbody cinemático + Portal  (añade PathMover aquí para moverlo)
    ///   │  ├─ Ring   disco de color (sin colisión)
    ///   │  └─ Core   interior oscuro que "respira" (PortalPulse)
    ///   └─ Portal_B  igual, en naranja
    /// Si el prefab ya existe no se toca (solo se revisan la capa y el sonido).
    /// </summary>
    internal static class PortalBuilder
    {
        internal const string PrefabPath = ProjectSetupStage0.ProjectRoot + "/Prefabs/Obstacles/PF_PortalPair.prefab";
        internal const string PortalAName = "Portal_A";
        internal const string PortalBName = "Portal_B";
        private const string MaterialsFolder = ProjectSetupStage0.ProjectRoot + "/Art/Materials";
        private const string LibraryPath = ProjectSetupStage0.ProjectRoot + "/Settings/Audio/SO_SoundLibrary.asset";

        private const float Radius = 0.9f;
        private static readonly Vector3 DefaultA = new Vector3(-2f, 1.5f, 0f);
        private static readonly Vector3 DefaultB = new Vector3(2f, 1.5f, 0f);
        private static readonly Color Blue = new Color(0.15f, 0.55f, 1f);
        private static readonly Color Orange = new Color(1f, 0.55f, 0.1f);

        [MenuItem("NovaStriker/Obstáculos/Crear portales (PF_PortalPair)")]
        private static void Run()
        {
            var log = new StringBuilder("[NovaStriker Obstáculos] Portales\n");
            if (EnsureAll(log))
            {
                log.AppendLine("- Uso: arrastra PF_PortalPair dentro del prefab del nivel y coloca Portal_A y Portal_B. " +
                               "Para que un portal se mueva, añade PathMover a Portal_A o Portal_B.");
                Debug.Log(log.ToString());
            }
            else
            {
                Debug.LogError(log.ToString());
            }
        }

        /// <summary>Capa, colisiones, sonido y prefab. Lo usan este menú y los generadores de niveles.</summary>
        internal static bool EnsureAll(StringBuilder log)
        {
            if (!EnsureLayer(log))
            {
                return false;
            }

            ConfigureCollisions(log);
            EnsureSound(log);
            return EnsurePrefab(log);
        }

        // ---------- Capa y colisiones ----------

        private static bool EnsureLayer(StringBuilder log)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                log.AppendLine("- ERROR: no se pudo cargar TagManager.asset.");
                return false;
            }

            var tagManager = new SerializedObject(assets[0]);
            SerializedProperty slot = tagManager.FindProperty("layers").GetArrayElementAtIndex(GameLayers.Portal);
            if (slot.stringValue == GameLayers.PortalName)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(slot.stringValue))
            {
                log.AppendLine($"- ERROR: la capa {GameLayers.Portal} ya se llama '{slot.stringValue}'. Libérala o avísame para usar otra.");
                return false;
            }

            slot.stringValue = GameLayers.PortalName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine($"- Capa {GameLayers.Portal} = {GameLayers.PortalName}");
            return true;
        }

        private static void ConfigureCollisions(StringBuilder log)
        {
            for (int layer = 0; layer < 32; layer++)
            {
                Physics.IgnoreLayerCollision(GameLayers.Portal, layer, layer != GameLayers.Ball);
            }

            Object[] dynamics = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset");
            if (dynamics != null && dynamics.Length > 0)
            {
                EditorUtility.SetDirty(dynamics[0]);
            }

            AssetDatabase.SaveAssets();
            log.AppendLine("- Colisiones: Portal solo detecta Ball.");
        }

        // ---------- Sonido ----------

        private static void EnsureSound(StringBuilder log)
        {
            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            if (library == null)
            {
                log.AppendLine($"- AVISO: falta {LibraryPath}; los portales no sonarán (Etapa 9).");
                return;
            }

            if (library.GetSfx(SfxId.Portal) != null)
            {
                return;
            }

            SfxEntry whoosh = library.GetSfx(SfxId.Whoosh);
            if (whoosh == null || whoosh.Clips == null || whoosh.Clips.Length == 0)
            {
                log.AppendLine("- AVISO: SO_SoundLibrary no tiene clips de Whoosh; añade a mano un efecto con Id Portal.");
                return;
            }

            var serialized = new SerializedObject(library);
            SerializedProperty effects = serialized.FindProperty("_effects");
            int index = effects.arraySize;
            effects.arraySize = index + 1;
            SerializedProperty entry = effects.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative(nameof(SfxEntry.Id)).intValue = (int)SfxId.Portal;
            SerializedProperty clips = entry.FindPropertyRelative(nameof(SfxEntry.Clips));
            clips.arraySize = whoosh.Clips.Length;
            for (int i = 0; i < whoosh.Clips.Length; i++)
            {
                clips.GetArrayElementAtIndex(i).objectReferenceValue = whoosh.Clips[i];
            }

            entry.FindPropertyRelative(nameof(SfxEntry.Volume)).floatValue = 0.8f;
            entry.FindPropertyRelative(nameof(SfxEntry.PitchJitter)).floatValue = 0.1f;
            entry.FindPropertyRelative(nameof(SfxEntry.VolumeJitter)).floatValue = 0.05f;
            entry.FindPropertyRelative(nameof(SfxEntry.MinInterval)).floatValue = 0.1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            log.AppendLine("- SO_SoundLibrary: efecto Portal añadido (clips de Whoosh, volumen 0,8).");
        }

        // ---------- Prefab ----------

        private static bool EnsurePrefab(StringBuilder log)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                log.AppendLine($"- {PrefabPath} ya existe; no se modifica (bórralo para regenerarlo).");
                return true;
            }

            Material ringA = GetOrCreateMaterial("MAT_PortalRing_Blue", Blue, log);
            Material coreA = GetOrCreateMaterial("MAT_PortalCore_Blue", Color.Lerp(Blue, Color.black, 0.7f), log);
            Material ringB = GetOrCreateMaterial("MAT_PortalRing_Orange", Orange, log);
            Material coreB = GetOrCreateMaterial("MAT_PortalCore_Orange", Color.Lerp(Orange, Color.black, 0.7f), log);
            if (ringA == null || coreA == null || ringB == null || coreB == null)
            {
                log.AppendLine("- ERROR: no se pudieron crear los materiales (¿falta URP?).");
                return false;
            }

            var root = new GameObject("PF_PortalPair");
            try
            {
                Portal a = BuildPortal(root.transform, PortalAName, DefaultA, ringA, coreA);
                Portal b = BuildPortal(root.transform, PortalBName, DefaultB, ringB, coreB);

                var pair = root.AddComponent<PortalPair>();
                ProjectSetupStage2.AssignReference(pair, "_a", a);
                ProjectSetupStage2.AssignReference(pair, "_b", b);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                log.AppendLine($"- Prefab: {PrefabPath} (A azul, B naranja, radio {Radius} m).");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Portal BuildPortal(Transform parent, string name, Vector3 position, Material ring, Material core)
        {
            var mouth = new GameObject(name) { layer = GameLayers.Portal };
            mouth.transform.SetParent(parent, false);
            mouth.transform.localPosition = position;

            var trigger = mouth.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = Radius;

            // Cinemático: así se puede mover con PathMover sin que la física lo empuje.
            var body = mouth.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            Portal portal = mouth.AddComponent<Portal>();

            // Discos de cara a la cámara (eje Z). El interior es un poco más grueso para que se vea por delante y por detrás.
            Disc(mouth.transform, "Ring", ring, Radius * 2f, 0.03f);
            GameObject inner = Disc(mouth.transform, "Core", core, Radius * 1.6f, 0.04f);
            inner.AddComponent<PortalPulse>();
            return portal;
        }

        private static GameObject Disc(Transform parent, string name, Material material, float diameter, float halfThickness)
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            disc.layer = GameLayers.Portal;
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(parent, false);
            disc.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            disc.transform.localScale = new Vector3(diameter, halfThickness, diameter);

            var renderer = disc.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return disc;
        }

        private static Material GetOrCreateMaterial(string name, Color color, StringBuilder log)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            log.AppendLine($"- Material: {path}");
            return material;
        }
    }
}
