using System.Collections.Generic;
using System.Text;
using NovaStriker.Bootstrap.Scenes;
using NovaStriker.Contracts.Data;
using NovaStriker.EditorTools.Setup.Ui;
using NovaStriker.UI.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Añade a SC_Game la indicación de tiro del tutorial (nivel 1): dedo animado, anillo de toque, rastro de puntos
    /// y un cartel "Arrastra el dedo hasta el futbolista y suéltalo para lanzar". Nada recibe toques.
    /// Idempotente: si ya existe "TutorialHint" en Canvas_Gameplay, lo rehace.
    /// </summary>
    internal static class TutorialHintBuilder
    {
        private const string CanvasName = "Canvas_Gameplay";
        private const string HintName = "TutorialHint";
        private const string HandSpritePath = ProjectSetupStage0.ProjectRoot + "/Art/UI/UI_Hand.png";
        private const int TrailDots = 6;
        private const string Message = "Arrastra el dedo hasta el futbolista\ny suéltalo para lanzar";

        [MenuItem("NovaStriker/Setup/Tutorial - Indicación de tiro")]
        private static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[NovaStriker Setup] Cancelado: hay escenas sin guardar.");
                return;
            }

            var log = new StringBuilder("[NovaStriker Setup] Tutorial - Indicación de tiro\n");
            bool ok = ConfigureHandSprite(log) && UiKit.PrepareAssets(log) && Build(log);
            if (ok)
            {
                Debug.Log(log.ToString());
            }
            else
            {
                Debug.LogError(log.ToString());
            }
        }

        private static bool ConfigureHandSprite(StringBuilder log)
        {
            var importer = AssetImporter.GetAtPath(HandSpritePath) as TextureImporter;
            if (importer == null)
            {
                log.AppendLine($"- ERROR: falta {HandSpritePath}.");
                return false;
            }

            if (importer.textureType == TextureImporterType.Sprite && !importer.mipmapEnabled)
            {
                return true;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            log.AppendLine($"- Sprite: {HandSpritePath}");
            return true;
        }

        private static bool Build(StringBuilder log)
        {
            Scene scene = EditorSceneManager.OpenScene(ProjectSetupStage0.ScenePath(SceneNames.Game), OpenSceneMode.Single);

            // Regla del proyecto: cargar assets por ruta DESPUÉS de abrir la escena.
            UiKit kit = UiKit.Load(log);
            var hand = AssetDatabase.LoadAssetAtPath<Sprite>(HandSpritePath);
            var root = ProjectSetupStage3.FindRootComponent<GameplayCompositionRoot>(scene);
            GameObject canvas = ProjectSetupStage3.FindRoot(scene, CanvasName);
            if (kit == null || hand == null || root == null || canvas == null)
            {
                log.AppendLine($"- ERROR: faltan la fuente, {HandSpritePath}, GameplayCompositionRoot o {CanvasName} en SC_Game.");
                return false;
            }

            var camera = new SerializedObject(root).FindProperty("_camera").objectReferenceValue as Camera;
            if (camera == null)
            {
                log.AppendLine("- ERROR: GameplayCompositionRoot no tiene cámara asignada.");
                return false;
            }

            Transform old = canvas.transform.Find(HintName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
                log.AppendLine($"- {HintName} anterior eliminado.");
            }

            TutorialHintView view = BuildHint(kit, (RectTransform)canvas.transform, hand, camera);
            ProjectSetupStage2.AssignReference(root, "_tutorialHintView", view);
            ProjectSetupStage2.SaveScene(scene);

            bool ok = ProjectSetupStage3.VerifyNoMissingReferences(view, log)
                      && ProjectSetupStage3.VerifyNoMissingReferences(root, log);
            if (ok)
            {
                log.AppendLine($"- {CanvasName}/{HintName}: dedo, anillo, {TrailDots} puntos de rastro y cartel. Asignado a GameplayCompositionRoot.");
            }

            return ok;
        }

        private static TutorialHintView BuildHint(UiKit kit, RectTransform canvas, Sprite handSprite, Camera camera)
        {
            RectTransform hint = UiKit.Stretch(UiKit.Node(HintName, canvas));
            hint.pivot = new Vector2(0.5f, 0.5f);

            // Encima del HUD, debajo de victoria y resultados.
            Transform hud = canvas.Find("HUD");
            hint.SetSiblingIndex(hud != null ? hud.GetSiblingIndex() + 1 : canvas.childCount - 1);

            var group = hint.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var dots = new List<Object>();
            for (int i = 0; i < TrailDots; i++)
            {
                Image dot = UiKit.CreateImage(hint, $"TrailDot_{i + 1}", UiKit.CircleSprite, new Color(1f, 1f, 1f, 0f), Image.Type.Simple);
                dot.rectTransform.sizeDelta = new Vector2(26f, 26f);
                dots.Add(dot);
            }

            Image ring = UiKit.CreateImage(hint, "TouchRing", UiKit.CircleSprite, new Color(1f, 0.92f, 0.3f, 0f), Image.Type.Simple);
            ring.rectTransform.sizeDelta = new Vector2(150f, 150f);

            Image handImage = UiKit.CreateImage(hint, "Hand", UiKit.CircleSprite, Color.white, Image.Type.Simple);
            handImage.sprite = handSprite;
            handImage.preserveAspect = true;
            RectTransform handRect = handImage.rectTransform;
            handRect.sizeDelta = new Vector2(180f, 180f);
            handRect.pivot = new Vector2(0.5f, 0.92f); // La punta del dedo es el punto que se mueve.

            BuildMessage(kit, hint);

            var view = hint.gameObject.AddComponent<TutorialHintView>();
            ProjectSetupStage2.AssignReference(view, "_camera", camera);
            ProjectSetupStage2.AssignReference(view, "_area", hint);
            ProjectSetupStage2.AssignReference(view, "_group", group);
            ProjectSetupStage2.AssignReference(view, "_hand", handRect);
            ProjectSetupStage2.AssignReference(view, "_handImage", handImage);
            ProjectSetupStage2.AssignReference(view, "_ring", ring.rectTransform);
            ProjectSetupStage2.AssignReference(view, "_ringImage", ring);
            ProjectSetupStage3.AssignArray(view, "_trailDots", dots.ToArray());
            return view;
        }

        /// <summary>Cartel bajo los contadores del HUD, dentro de la zona segura (respeta muesca y banner).</summary>
        private static void BuildMessage(UiKit kit, RectTransform hint)
        {
            RectTransform safe = UiKit.CreateSafeArea(hint);
            Image panel = UiKit.CreateImage(safe, "MessagePanel", UiKit.PanelSprite,
                new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.9f));
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(960f, 190f));

            TextMeshProUGUI label = kit.CreateText(panel.rectTransform, "Label", Message, 58f);
            RectTransform labelRect = UiKit.Stretch(label.rectTransform);
            labelRect.offsetMin = new Vector2(30f, 18f);
            labelRect.offsetMax = new Vector2(-30f, -18f);
        }
    }
}
