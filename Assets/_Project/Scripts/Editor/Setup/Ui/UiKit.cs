using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace NovaStriker.EditorTools.Setup.Ui
{
    /// <summary>
    /// Piezas comunes de la interfaz (Etapa 7): configura la importación de los sprites de Art/UI, crea la fuente TMP
    /// (Lilita One, licencia OFL) y su material con contorno, y ofrece ayudas para montar paneles, textos y botones.
    /// Regla del proyecto: <see cref="PrepareAssets"/> antes de abrir una escena y <see cref="Load"/> después.
    /// </summary>
    internal sealed class UiKit
    {
        private const string Root = ProjectSetupStage0.ProjectRoot;
        private const string SpriteFolder = Root + "/Art/UI/";
        private const string FontFolder = Root + "/Art/Fonts/";
        private const string FontSourcePath = FontFolder + "LilitaOne-Regular.ttf";
        private const string FontAssetPath = FontFolder + "FA_LilitaOne.asset";
        private const string OutlineMaterialPath = FontFolder + "MAT_LilitaOne_Outline.mat";

        // Paleta (inspirada en la referencia: azul petróleo, amarillo, verde).
        public static readonly Color Teal = new Color(0.12f, 0.43f, 0.55f);
        public static readonly Color Navy = new Color(0.06f, 0.2f, 0.28f);
        public static readonly Color Green = new Color(0.24f, 0.76f, 0.24f);
        public static readonly Color Orange = new Color(0.96f, 0.58f, 0.13f);
        public static readonly Color Slate = new Color(0.3f, 0.42f, 0.52f);
        public static readonly Color TextOutline = new Color(0.05f, 0.14f, 0.2f);

        public const string PanelSprite = "UI_Panel";
        public const string ButtonSprite = "UI_Button";
        public const string CircleSprite = "UI_Circle";
        public const string LevelSquareSprite = "UI_LevelSquare";
        public const string HexPatternSprite = "UI_HexPattern";
        public const string BannerSprite = "UI_Banner";
        public const string BallSprite = "UI_Ball";
        public const string PlayerIconSprite = "UI_PlayerIcon";
        public const string GearSprite = "UI_Gear";

        private readonly TMP_FontAsset _font;
        private readonly Material _outlineMaterial;

        private UiKit(TMP_FontAsset font, Material outlineMaterial)
        {
            _font = font;
            _outlineMaterial = outlineMaterial;
        }

        // ---------- Preparación de assets (antes de abrir escenas) ----------

        /// <summary>Configura los sprites y crea fuente y material si faltan. Devuelve false si falta algo.</summary>
        public static bool PrepareAssets(StringBuilder log)
        {
            bool ok = ConfigureSprite(PanelSprite, new Vector4(40, 40, 40, 40), log)
                      & ConfigureSprite(ButtonSprite, new Vector4(44, 52, 44, 44), log)
                      & ConfigureSprite(CircleSprite, Vector4.zero, log)
                      & ConfigureSprite(LevelSquareSprite, new Vector4(14, 20, 14, 14), log)
                      & ConfigureSprite(HexPatternSprite, Vector4.zero, log, TextureWrapMode.Repeat)
                      & ConfigureSprite(BannerSprite, Vector4.zero, log)
                      & ConfigureSprite(BallSprite, Vector4.zero, log)
                      & ConfigureSprite(PlayerIconSprite, Vector4.zero, log)
                      & ConfigureSprite(GearSprite, Vector4.zero, log);

            TMP_FontAsset font = GetOrCreateFontAsset(log);
            if (font == null)
            {
                return false;
            }

            GetOrCreateOutlineMaterial(font, log);
            AssetDatabase.SaveAssets();
            return ok;
        }

        private static bool ConfigureSprite(string name, Vector4 border, StringBuilder log, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            string path = SpriteFolder + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                log.AppendLine($"- ERROR: falta {path}.");
                return false;
            }

            bool changed = importer.textureType != TextureImporterType.Sprite
                           || importer.spriteBorder != border
                           || importer.wrapMode != wrap
                           || importer.mipmapEnabled;
            if (!changed)
            {
                return true;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = wrap;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.spriteBorder = border;

            importer.SaveAndReimport();
            log.AppendLine($"- Sprite configurado: {name}.");
            return true;
        }

        private static TMP_FontAsset GetOrCreateFontAsset(StringBuilder log)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null)
            {
                return existing;
            }

            var source = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
            if (source == null)
            {
                log.AppendLine($"- ERROR: falta la fuente {FontSourcePath}.");
                return null;
            }

            // Mismos parámetros que el menú de TextMeshPro "Create > Font Asset" (atlas dinámico).
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            font.name = "FA_LilitaOne";
            AssetDatabase.CreateAsset(font, FontAssetPath);

            for (int i = 0; i < font.atlasTextures.Length; i++)
            {
                font.atlasTextures[i].name = $"FA_LilitaOne Atlas {i}";
                AssetDatabase.AddObjectToAsset(font.atlasTextures[i], font);
            }

            font.material.name = "FA_LilitaOne Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            EditorUtility.SetDirty(font);
            log.AppendLine($"- Fuente TMP: {FontAssetPath} (Lilita One, atlas dinámico).");
            return font;
        }

        private static void GetOrCreateOutlineMaterial(TMP_FontAsset font, StringBuilder log)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath) != null)
            {
                return;
            }

            var material = new Material(font.material) { name = "MAT_LilitaOne_Outline" };
            material.EnableKeyword("OUTLINE_ON");
            material.SetFloat("_OutlineWidth", 0.22f);
            material.SetColor("_OutlineColor", TextOutline);
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.45f));
            material.SetFloat("_UnderlayOffsetY", -0.6f);
            material.SetFloat("_UnderlayDilate", 0.2f);
            AssetDatabase.CreateAsset(material, OutlineMaterialPath);
            log.AppendLine($"- Material de texto con contorno: {OutlineMaterialPath}.");
        }

        // ---------- Uso (después de abrir la escena) ----------

        /// <summary>Carga fuente y material por ruta. Llamar después de EditorSceneManager.OpenScene.</summary>
        public static UiKit Load(StringBuilder log)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            var outline = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (font == null || outline == null)
            {
                log.AppendLine("- ERROR: faltan la fuente o su material. Ejecuta el setup de la Etapa 7.");
                return null;
            }

            return new UiKit(font, outline);
        }

        public static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + name + ".png");

        public static RectTransform Node(string name, Transform parent)
        {
            return ProjectSetupStage2.CreateUiObject(name, parent);
        }

        public static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>Ancla y pivote en el mismo punto (p. ej. (0.5, 1) = arriba al centro).</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image CreateImage(Transform parent, string name, string sprite, Color color, Image.Type type = Image.Type.Sliced)
        {
            RectTransform rect = Node(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(sprite);
            image.type = image.sprite != null && image.sprite.border == Vector4.zero && type == Image.Type.Sliced
                ? Image.Type.Simple
                : type;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Capa de hexágonos sutil que rellena <paramref name="parent"/> (textura tipo balón).</summary>
        public static void HexOverlay(Transform parent, float alpha, float padding)
        {
            Image hex = CreateImage(parent, "HexPattern", HexPatternSprite, new Color(1f, 1f, 1f, alpha), Image.Type.Tiled);
            RectTransform rect = Stretch(hex.rectTransform);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        public TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = Node(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = _font;
            label.fontSharedMaterial = _outlineMaterial;
            label.text = text;
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = size * 0.5f;
            label.fontSizeMax = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        public Button CreateButton(Transform parent, string name, string text, Color color, Vector2 size, float fontSize, out TextMeshProUGUI label)
        {
            Image image = CreateImage(parent, name, ButtonSprite, color);
            image.raycastTarget = true;
            image.rectTransform.sizeDelta = size;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.75f);
            button.colors = colors;

            label = CreateText(image.rectTransform, "Label", text, fontSize);
            RectTransform labelRect = Stretch(label.rectTransform);
            labelRect.offsetMin = new Vector2(24f, 18f); // Deja libre el labio inferior del sprite.
            labelRect.offsetMax = new Vector2(-24f, -6f);
            return button;
        }

        /// <summary>Contenedor ajustado a la zona segura del móvil.</summary>
        public static RectTransform CreateSafeArea(Transform parent)
        {
            RectTransform rect = Stretch(Node("SafeArea", parent));
            rect.gameObject.AddComponent<NovaStriker.UI.Common.SafeAreaFitter>();
            return rect;
        }
    }
}
