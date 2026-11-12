using System.Text;
using NovaStriker.Bootstrap;
using NovaStriker.Bootstrap.Scenes;
using NovaStriker.Contracts.Data;
using NovaStriker.UI.MainMenu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Etapa 2: crea Resources/PF_AppRoot, la UI de SC_MainMenu y la raíz de SC_Game.
    /// (Desde la Etapa 6, la UI de SC_Game la crea la Etapa 6.)
    /// Idempotente: si una escena ya tiene su raíz de composición, no la toca.
    /// </summary>
    internal static class ProjectSetupStage2
    {
        private const string ResourcesFolder = ProjectSetupStage0.ProjectRoot + "/Resources";
        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
        private static readonly Color BackgroundColor = new Color(0.07f, 0.12f, 0.09f, 1f);
        private static readonly Color ButtonColor = new Color(0.16f, 0.62f, 0.32f, 1f);

        [MenuItem("NovaStriker/Setup/Etapa 2 - AppRoot y escenas provisionales")]
        private static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[NovaStriker Setup] Cancelado: hay escenas sin guardar.");
                return;
            }

            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                Debug.LogError(
                    "[NovaStriker Setup] Faltan los recursos de TextMeshPro. " +
                    "Ve a Window > TextMeshPro > Import TMP Essential Resources y vuelve a ejecutar este menú.");
                return;
            }

            var log = new StringBuilder("[NovaStriker Setup] Etapa 2\n");

            CreateAppRootPrefab(log);
            SetupMainMenuScene(log);
            SetupGameScene(log);

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ProjectSetupStage0.ScenePath(SceneNames.Boot));
            Debug.Log(log.ToString());
        }

        private static void CreateAppRootPrefab(StringBuilder log)
        {
            string path = $"{ResourcesFolder}/{AppBootstrapper.AppRootResourcePath}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                log.AppendLine($"- Ya existe {path}");
                return;
            }

            var root = new GameObject(AppBootstrapper.AppRootResourcePath);
            root.AddComponent<AppCompositionRoot>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            log.AppendLine($"- Prefab creado: {path}");
        }

        private static void SetupMainMenuScene(StringBuilder log)
        {
            Scene scene = EditorSceneManager.OpenScene(ProjectSetupStage0.ScenePath(SceneNames.MainMenu), OpenSceneMode.Single);
            if (HasRoot<MainMenuCompositionRoot>(scene))
            {
                log.AppendLine("- SC_MainMenu ya estaba configurada.");
                return;
            }

            EnsureEventSystem(scene);
            RectTransform canvas = CreateCanvas("Canvas_MainMenu");
            CreateBackground(canvas);
            CreateLabel(canvas, "Title", "NOVA STRIKER", 120f, new Vector2(0f, 450f), new Vector2(1000f, 220f));
            Button playButton = CreateButton(canvas, "Button_Play", "JUGAR", new Vector2(0f, -250f));

            var view = canvas.gameObject.AddComponent<MainMenuView>();
            AssignReference(view, "_playButton", playButton);

            var root = new GameObject("[MainMenuCompositionRoot]").AddComponent<MainMenuCompositionRoot>();
            AssignReference(root, "_view", view);

            SaveScene(scene);
            log.AppendLine("- SC_MainMenu: EventSystem, Canvas, botón JUGAR y MainMenuCompositionRoot.");
        }

        private static void SetupGameScene(StringBuilder log)
        {
            Scene scene = EditorSceneManager.OpenScene(ProjectSetupStage0.ScenePath(SceneNames.Game), OpenSceneMode.Single);
            if (HasRoot<GameplayCompositionRoot>(scene))
            {
                log.AppendLine("- SC_Game ya estaba configurada.");
                return;
            }

            EnsureEventSystem(scene);
            new GameObject("[GameplayCompositionRoot]").AddComponent<GameplayCompositionRoot>();

            SaveScene(scene);
            log.AppendLine("- SC_Game: EventSystem y GameplayCompositionRoot (el resto lo monta la Etapa 6).");
        }

        internal static bool HasRoot<T>(Scene scene) where T : Component
        {
            foreach (GameObject gameObject in scene.GetRootGameObjects())
            {
                if (gameObject.GetComponent<T>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        internal static void EnsureEventSystem(Scene scene)
        {
            foreach (GameObject gameObject in scene.GetRootGameObjects())
            {
                if (gameObject.GetComponent<EventSystem>() != null)
                {
                    return;
                }
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        internal static RectTransform CreateCanvas(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            gameObject.layer = LayerMask.NameToLayer("UI");

            var canvas = gameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = gameObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // Vertical: se ajusta al ancho para que nada se salga por los lados.

            return (RectTransform)gameObject.transform;
        }

        internal static void CreateBackground(RectTransform parent)
        {
            RectTransform rect = CreateUiObject("Background", parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = BackgroundColor;
            image.raycastTarget = false;
        }

        internal static TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text, float fontSize, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateUiObject(name, parent);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        internal static Button CreateButton(RectTransform parent, string name, string text, Vector2 position)
        {
            RectTransform rect = CreateUiObject(name, parent);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(600f, 180f);

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = ButtonColor;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI label = CreateLabel(rect, "Label", text, 72f, Vector2.zero, Vector2.zero);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        internal static RectTransform CreateUiObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        internal static void AssignReference(Object target, string propertyName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[NovaStriker Setup] {target.GetType().Name} no tiene el campo '{propertyName}'.");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SaveScene(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
