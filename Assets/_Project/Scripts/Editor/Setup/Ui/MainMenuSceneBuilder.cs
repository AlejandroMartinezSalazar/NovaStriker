using System.Text;
using NovaStriker.Bootstrap.Scenes;
using NovaStriker.Contracts.Data;
using NovaStriker.Gameplay.Characters;
using NovaStriker.Gameplay.Level;
using NovaStriker.UI.MainMenu;
using NovaStriker.UI.Settings;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NovaStriker.EditorTools.Setup.Ui
{
    /// <summary>
    /// SC_MainMenu (Etapa 7): escenario 3D (campo + futbolista bailando + balón) detrás de la interfaz,
    /// panel de nivel con cuadraditos (grupos de 10), botón JUGAR, la pantalla aparte "Próximamente"
    /// y (Etapa 9) el engranaje de ajustes de sonido.
    /// Lo que ya exista de la Etapa 7 no se toca; el canvas provisional de la Etapa 2 se sustituye.
    /// </summary>
    internal static class MainMenuSceneBuilder
    {
        private const string CanvasName = "Canvas_Menu";
        private const string OldCanvasName = "Canvas_MainMenu";
        private const string StageName = "[MenuStage]";
        private const int SquareCount = 10;

        private static readonly Vector3 CameraPosition = new Vector3(0f, 1.25f, -3.6f);
        private static readonly Vector3 CameraLookAt = new Vector3(0f, 1.1f, 0f);

        public static bool Setup(StringBuilder log)
        {
            Scene scene = EditorSceneManager.OpenScene(ProjectSetupStage0.ScenePath(SceneNames.MainMenu), OpenSceneMode.Single);

            // Regla del proyecto: cargar los assets por ruta DESPUÉS de abrir la escena.
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(ProjectSetupStage6.CatalogPath);
            var footballer = AssetDatabase.LoadAssetAtPath<FootballerController>(ProjectSetupStage4.PrefabPath);
            var ball = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectSetupStage3.BallPrefabPath);
            UiKit kit = UiKit.Load(log);

            var root = ProjectSetupStage3.FindRootComponent<MainMenuCompositionRoot>(scene);
            Camera camera = ProjectSetupStage3.FindInScene<Camera>(scene);
            if (root == null || camera == null || catalog == null || footballer == null || ball == null || kit == null)
            {
                log.AppendLine("- ERROR: SC_MainMenu necesita MainMenuCompositionRoot y cámara (Etapa 2), el catálogo (Etapa 6), " +
                               "PF_Footballer (Etapa 4) y PF_Ball (Etapa 3).");
                return false;
            }

            BuildStage(scene, camera, footballer, ball, log);

            GameObject canvas = ProjectSetupStage3.FindRoot(scene, CanvasName);
            if (canvas == null)
            {
                GameObject old = ProjectSetupStage3.FindRoot(scene, OldCanvasName);
                if (old != null)
                {
                    Object.DestroyImmediate(old);
                    log.AppendLine($"- {OldCanvasName} provisional (Etapa 2) sustituido.");
                }

                canvas = BuildCanvas(kit);
                log.AppendLine($"- {CanvasName}: panel de nivel (grupos de {SquareCount}), JUGAR y pantalla 'Próximamente'.");
            }
            else
            {
                log.AppendLine($"- {CanvasName} ya existía, no se modifica.");
            }

            ProjectSetupStage2.EnsureEventSystem(scene);

            MainMenuView view = canvas.GetComponentInChildren<MainMenuView>(true);
            if (view == null)
            {
                log.AppendLine($"- ERROR: {CanvasName} no tiene MainMenuView. Bórralo y vuelve a ejecutar.");
                return false;
            }

            SettingsView settings = SettingsUiBuilder.Ensure(canvas, kit, log);

            ProjectSetupStage2.AssignReference(root, "_view", view);
            ProjectSetupStage2.AssignReference(root, "_levelCatalog", catalog);
            ProjectSetupStage2.AssignReference(root, "_settingsView", settings);
            ProjectSetupStage2.AssignReference(root, "_camera", camera);
            ProjectSetupStage2.SaveScene(scene);

            bool ok = ProjectSetupStage3.VerifyNoMissingReferences(root, log)
                      & ProjectSetupStage3.VerifyNoMissingReferences(view, log)
                      & ProjectSetupStage3.VerifyNoMissingReferences(settings, log);
            if (ok)
            {
                log.AppendLine("- MainMenuCompositionRoot y MainMenuView: referencias asignadas y verificadas.");
            }

            return ok;
        }

        // ---------- Escenario 3D ----------

        private static void BuildStage(Scene scene, Camera camera, FootballerController footballerPrefab, GameObject ballPrefab, StringBuilder log)
        {
            camera.transform.SetPositionAndRotation(CameraPosition, Quaternion.LookRotation(CameraLookAt - CameraPosition));
            camera.fieldOfView = 50f;

            if (ProjectSetupStage3.FindInScene<Light>(scene) == null)
            {
                var light = new GameObject("Directional Light", typeof(Light)).GetComponent<Light>();
                light.type = LightType.Directional;
                light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            if (ProjectSetupStage3.FindRoot(scene, StageName) != null)
            {
                log.AppendLine($"- {StageName} ya existía, no se modifica.");
                return;
            }

            var stage = new GameObject(StageName);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.layer = GameLayers.Environment;
            ground.transform.SetParent(stage.transform, false);
            ground.transform.localPosition = new Vector3(0f, 0f, 6f);
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = ProjectSetupStage3.LoadMaterial(ProjectSetupStage3.GroundMaterialName);

            // El futbolista no se construye con la Factory: es decorado. Sin Construct no evalúa derribos,
            // el ragdoll queda animado (RagdollRig.Awake) y el Animator reproduce el baile por defecto.
            var footballer = (GameObject)PrefabUtility.InstantiatePrefab(footballerPrefab.gameObject, stage.transform);
            footballer.name = "Footballer_Dancing";
            footballer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            footballer.GetComponent<Rigidbody>().isKinematic = true; // Que no lo mueva la física.

            var ball = (GameObject)PrefabUtility.InstantiatePrefab(ballPrefab, stage.transform);
            ball.name = "Ball_Decoration";
            ball.transform.position = new Vector3(0.55f, ProjectSetupStage3.BallDiameter * 0.5f, -0.45f);

            log.AppendLine($"- {StageName}: campo, futbolista bailando y balón; cámara encuadrando al futbolista.");
        }

        // ---------- Interfaz ----------

        private static GameObject BuildCanvas(UiKit kit)
        {
            RectTransform canvas = ProjectSetupStage2.CreateCanvas(CanvasName);
            RectTransform viewRect = UiKit.Stretch(UiKit.Node("Menu", canvas));
            var view = viewRect.gameObject.AddComponent<MainMenuView>();

            // --- Menú normal ---
            RectTransform menuScreen = UiKit.Stretch(UiKit.Node("MenuScreen", viewRect));
            RectTransform safe = UiKit.CreateSafeArea(menuScreen);

            TextMeshProUGUI title = kit.CreateText(safe, "Label_Title", "NOVA STRIKER", 120f);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1000f, 170f));

            Image panel = UiKit.CreateImage(safe, "LevelPanel", UiKit.PanelSprite, UiKit.Teal);
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 400f), new Vector2(920f, 280f));
            UiKit.HexOverlay(panel.rectTransform, 0.12f, 10f);

            TextMeshProUGUI levelLabel = kit.CreateText(panel.rectTransform, "Label_Level", "NIVEL 1", 72f, TextAlignmentOptions.Left);
            UiKit.Place(levelLabel.rectTransform, new Vector2(0f, 1f), new Vector2(44f, -24f), new Vector2(600f, 100f));

            var squares = new Object[SquareCount];
            const float squareWidth = 66f;
            const float squareHeight = 88f;
            const float spacing = 14f;
            for (int i = 0; i < SquareCount; i++)
            {
                Image square = UiKit.CreateImage(panel.rectTransform, $"Square_{i + 1:00}", UiKit.LevelSquareSprite, Color.white);
                RectTransform rect = square.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0.5f, 0f); // El del nivel actual crece hacia arriba.
                rect.anchoredPosition = new Vector2(44f + squareWidth * 0.5f + i * (squareWidth + spacing), 40f);
                rect.sizeDelta = new Vector2(squareWidth, squareHeight);
                squares[i] = square;
            }

            Button play = kit.CreateButton(safe, "Button_Play", "JUGAR", UiKit.Green, new Vector2(640f, 180f), 86f, out _);
            UiKit.Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(640f, 180f));

            // --- Pantalla aparte "Próximamente" ---
            Image dim = UiKit.CreateImage(viewRect, "ComingSoonScreen", UiKit.PanelSprite, new Color(0f, 0f, 0f, 0.55f));
            dim.sprite = null;
            dim.type = Image.Type.Simple;
            dim.raycastTarget = true;
            RectTransform comingSoon = UiKit.Stretch(dim.rectTransform);
            RectTransform comingSafe = UiKit.CreateSafeArea(comingSoon);

            Image card = UiKit.CreateImage(comingSafe, "Card", UiKit.PanelSprite, UiKit.Navy);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(940f, 820f));
            UiKit.HexOverlay(card.rectTransform, 0.08f, 14f);

            Image banner = UiKit.CreateImage(card.rectTransform, "Banner", UiKit.BannerSprite, UiKit.Teal);
            UiKit.Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 120f), new Vector2(900f, 300f));
            TextMeshProUGUI bannerText = kit.CreateText(banner.rectTransform, "Label_Title", "PRÓXIMAMENTE", 100f);
            RectTransform bannerTextRect = UiKit.Stretch(bannerText.rectTransform);
            bannerTextRect.offsetMin = new Vector2(70f, 30f);
            bannerTextRect.offsetMax = new Vector2(-70f, -20f);

            TextMeshProUGUI subtitle = kit.CreateText(card.rectTransform, "Label_Subtitle", "NUEVOS NIVELES", 80f);
            UiKit.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(860f, 120f));
            TextMeshProUGUI message = kit.CreateText(card.rectTransform, "Label_Message", "Has superado todos los niveles disponibles.", 48f);
            UiKit.Place(message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -350f), new Vector2(820f, 140f));

            Button restart = kit.CreateButton(card.rectTransform, "Button_RestartCampaign", "EMPEZAR DESDE EL NIVEL 1",
                UiKit.Green, new Vector2(820f, 170f), 58f, out _);
            UiKit.Place((RectTransform)restart.transform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(820f, 170f));

            ProjectSetupStage2.AssignReference(view, "_menuScreen", menuScreen.gameObject);
            ProjectSetupStage2.AssignReference(view, "_levelLabel", levelLabel);
            ProjectSetupStage3.AssignArray(view, "_squares", squares);
            ProjectSetupStage2.AssignReference(view, "_playButton", play);
            ProjectSetupStage2.AssignReference(view, "_comingSoonScreen", comingSoon.gameObject);
            ProjectSetupStage2.AssignReference(view, "_restartCampaignButton", restart);
            comingSoon.gameObject.SetActive(false);

            return canvas.gameObject;
        }
    }
}
