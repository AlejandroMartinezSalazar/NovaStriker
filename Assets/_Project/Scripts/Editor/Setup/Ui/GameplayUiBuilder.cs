using System.Text;
using NovaStriker.Bootstrap.Scenes;
using NovaStriker.UI.HUD;
using NovaStriker.UI.Results;
using NovaStriker.UI.Victory;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NovaStriker.EditorTools.Setup.Ui
{
    /// <summary>
    /// Interfaz de SC_Game (Etapa 7): HUD, pantalla de victoria animada y panel de pausa/derrota, dentro de la zona segura.
    /// Si existe un Canvas_Gameplay de la Etapa 7 (con VictoryView) no se toca, para conservar tus ajustes;
    /// el provisional de la Etapa 6 se sustituye.
    /// </summary>
    internal static class GameplayUiBuilder
    {
        private const string CanvasName = "Canvas_Gameplay";

        /// <summary>Crea la UI si hace falta y asigna las vistas a la raíz. Llamar con la escena ya abierta.</summary>
        public static bool Ensure(Scene scene, GameplayCompositionRoot root, StringBuilder log)
        {
            UiKit kit = UiKit.Load(log);
            if (kit == null)
            {
                return false;
            }

            GameObject canvas = ProjectSetupStage3.FindRoot(scene, CanvasName);
            if (canvas != null && canvas.GetComponentInChildren<VictoryView>(true) == null)
            {
                Object.DestroyImmediate(canvas);
                canvas = null;
                log.AppendLine($"- {CanvasName} provisional (Etapa 6) sustituido por la interfaz final.");
            }

            if (canvas == null)
            {
                canvas = Build(kit);
                log.AppendLine($"- {CanvasName}: HUD, victoria animada y panel de pausa/derrota (con zona segura).");
            }
            else
            {
                log.AppendLine($"- {CanvasName} ya tenía la interfaz final, no se modifica.");
            }

            HudView hud = canvas.GetComponentInChildren<HudView>(true);
            ResultPanelView results = canvas.GetComponentInChildren<ResultPanelView>(true);
            VictoryView victory = canvas.GetComponentInChildren<VictoryView>(true);
            if (hud == null || results == null || victory == null)
            {
                log.AppendLine($"- ERROR: a {CanvasName} le falta HudView, ResultPanelView o VictoryView. Bórralo y vuelve a ejecutar.");
                return false;
            }

            ProjectSetupStage2.AssignReference(root, "_hudView", hud);
            ProjectSetupStage2.AssignReference(root, "_resultPanelView", results);
            ProjectSetupStage2.AssignReference(root, "_victoryView", victory);

            return ProjectSetupStage3.VerifyNoMissingReferences(hud, log)
                   & ProjectSetupStage3.VerifyNoMissingReferences(results, log)
                   & ProjectSetupStage3.VerifyNoMissingReferences(victory, log);
        }

        private static GameObject Build(UiKit kit)
        {
            RectTransform canvas = ProjectSetupStage2.CreateCanvas(CanvasName);
            BuildHud(kit, canvas);
            BuildVictory(kit, canvas);
            BuildResults(kit, canvas);
            return canvas.gameObject;
        }

        // ---------- HUD ----------

        private static void BuildHud(UiKit kit, RectTransform canvas)
        {
            RectTransform hudRect = UiKit.Stretch(UiKit.Node("HUD", canvas));
            var hud = hudRect.gameObject.AddComponent<HudView>();
            RectTransform safe = UiKit.CreateSafeArea(hudRect);

            // Fila 1: nivel al centro, pausa a la derecha.
            Image levelPill = UiKit.CreateImage(safe, "LevelPill", UiKit.PanelSprite, UiKit.Teal);
            UiKit.Place(levelPill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(320f, 100f));
            TextMeshProUGUI levelLabel = kit.CreateText(levelPill.rectTransform, "Label_Level", "NIVEL 1", 56f);
            UiKit.Stretch(levelLabel.rectTransform);

            Button pause = kit.CreateButton(safe, "Button_Pause", "II", UiKit.Teal, new Vector2(130f, 130f), 60f, out _);
            pause.GetComponent<Image>().sprite = UiKit.LoadSprite(UiKit.CircleSprite);
            pause.GetComponent<Image>().type = Image.Type.Simple;
            UiKit.Place((RectTransform)pause.transform, new Vector2(1f, 1f), new Vector2(-30f, -20f), new Vector2(130f, 130f));

            // Fila 2: por derribar a la izquierda, balones a la derecha.
            TextMeshProUGUI targetsLabel = Counter(kit, safe, "TargetsPill", UiKit.PlayerIconSprite, "Por derribar: 0/0",
                new Vector2(0f, 1f), new Vector2(30f, -170f), new Vector2(480f, 96f));
            TextMeshProUGUI ballsLabel = Counter(kit, safe, "BallsPill", UiKit.BallSprite, "Balones: 0",
                new Vector2(1f, 1f), new Vector2(-30f, -170f), new Vector2(340f, 96f));

            ProjectSetupStage2.AssignReference(hud, "_levelLabel", levelLabel);
            ProjectSetupStage2.AssignReference(hud, "_targetsLabel", targetsLabel);
            ProjectSetupStage2.AssignReference(hud, "_ballsLabel", ballsLabel);
            ProjectSetupStage2.AssignReference(hud, "_pauseButton", pause);
        }

        private static TextMeshProUGUI Counter(UiKit kit, Transform parent, string name, string icon, string text,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image pill = UiKit.CreateImage(parent, name, UiKit.PanelSprite, new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.9f));
            UiKit.Place(pill.rectTransform, anchor, position, size);

            Image iconImage = UiKit.CreateImage(pill.rectTransform, "Icon", icon, Color.white);
            UiKit.Place(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(66f, 66f));
            iconImage.preserveAspect = true;

            TextMeshProUGUI label = kit.CreateText(pill.rectTransform, "Label", text, 42f, TextAlignmentOptions.Left);
            RectTransform rect = UiKit.Stretch(label.rectTransform);
            rect.offsetMin = new Vector2(96f, 8f);
            rect.offsetMax = new Vector2(-18f, -8f);
            return label;
        }

        // ---------- Victoria ----------

        private static void BuildVictory(UiKit kit, RectTransform canvas)
        {
            RectTransform viewRect = UiKit.Stretch(UiKit.Node("Victory", canvas));
            var view = viewRect.gameObject.AddComponent<VictoryView>();

            RectTransform root = UiKit.Stretch(UiKit.Node("Root", viewRect));
            RectTransform safe = UiKit.CreateSafeArea(root);

            Image banner = UiKit.CreateImage(safe, "Banner", UiKit.BannerSprite, UiKit.Teal);
            UiKit.Place(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 420f), new Vector2(900f, 305f));

            // Balones sobre el cartel (hijos del cartel: crecen con él).
            RectTransform[] balls =
            {
                BannerBall(banner.rectTransform, "Ball_Left", new Vector2(-120f, 150f), 120f),
                BannerBall(banner.rectTransform, "Ball_Right", new Vector2(120f, 150f), 120f),
                BannerBall(banner.rectTransform, "Ball_Center", new Vector2(0f, 175f), 155f),
            };

            TextMeshProUGUI title = kit.CreateText(banner.rectTransform, "Label_Title", "¡VICTORIA!", 130f);
            RectTransform titleRect = UiKit.Stretch(title.rectTransform);
            titleRect.offsetMin = new Vector2(70f, 30f);
            titleRect.offsetMax = new Vector2(-70f, -20f);

            TextMeshProUGUI subtitle = kit.CreateText(safe, "Label_Subtitle", "NIVEL 1 COMPLETADO", 64f);
            UiKit.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(950f, 110f));

            ProjectSetupStage2.AssignReference(view, "_root", root.gameObject);
            ProjectSetupStage2.AssignReference(view, "_banner", banner.rectTransform);
            ProjectSetupStage2.AssignReference(view, "_subtitle", subtitle);
            ProjectSetupStage3.AssignArray(view, "_balls", balls);
            root.gameObject.SetActive(false);
        }

        private static RectTransform BannerBall(RectTransform banner, string name, Vector2 position, float size)
        {
            Image ball = UiKit.CreateImage(banner, name, UiKit.BallSprite, Color.white);
            ball.preserveAspect = true;
            RectTransform rect = ball.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            rect.SetAsFirstSibling(); // Se dibuja sobre el cartel pero detrás del título.
            return rect;
        }

        // ---------- Pausa / derrota ----------

        private static void BuildResults(UiKit kit, RectTransform canvas)
        {
            RectTransform viewRect = UiKit.Stretch(UiKit.Node("Results", canvas));
            var view = viewRect.gameObject.AddComponent<ResultPanelView>();

            Image dim = UiKit.CreateImage(viewRect, "Panel", UiKit.PanelSprite, new Color(0f, 0f, 0f, 0.6f));
            dim.sprite = null; // Fondo liso a pantalla completa.
            dim.type = Image.Type.Simple;
            dim.raycastTarget = true; // Bloquea el apuntado mientras está visible.
            RectTransform panel = UiKit.Stretch(dim.rectTransform);
            RectTransform safe = UiKit.CreateSafeArea(panel);

            Image card = UiKit.CreateImage(safe, "Card", UiKit.PanelSprite, UiKit.Navy);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(920f, 1080f));
            UiKit.HexOverlay(card.rectTransform, 0.08f, 14f);

            Image banner = UiKit.CreateImage(card.rectTransform, "Banner", UiKit.BannerSprite, UiKit.Teal);
            UiKit.Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 120f), new Vector2(860f, 290f));
            TextMeshProUGUI title = kit.CreateText(banner.rectTransform, "Label_Title", "PAUSA", 100f);
            RectTransform titleRect = UiKit.Stretch(title.rectTransform);
            titleRect.offsetMin = new Vector2(70f, 30f);
            titleRect.offsetMax = new Vector2(-70f, -20f);

            TextMeshProUGUI message = kit.CreateText(card.rectTransform, "Label_Message", "", 54f);
            UiKit.Place(message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(820f, 120f));

            Button primary = kit.CreateButton(card.rectTransform, "Button_Primary", "CONTINUAR", UiKit.Green, new Vector2(680f, 170f), 66f, out TextMeshProUGUI primaryLabel);
            UiKit.Place((RectTransform)primary.transform, new Vector2(0.5f, 1f), new Vector2(0f, -380f), new Vector2(680f, 170f));
            Button secondary = kit.CreateButton(card.rectTransform, "Button_Secondary", "REINICIAR", UiKit.Orange, new Vector2(680f, 170f), 66f, out TextMeshProUGUI secondaryLabel);
            UiKit.Place((RectTransform)secondary.transform, new Vector2(0.5f, 1f), new Vector2(0f, -585f), new Vector2(680f, 170f));
            Button menu = kit.CreateButton(card.rectTransform, "Button_Menu", "MENÚ", UiKit.Slate, new Vector2(680f, 170f), 66f, out _);
            UiKit.Place((RectTransform)menu.transform, new Vector2(0.5f, 1f), new Vector2(0f, -790f), new Vector2(680f, 170f));

            ProjectSetupStage2.AssignReference(view, "_panelRoot", panel.gameObject);
            ProjectSetupStage2.AssignReference(view, "_banner", banner);
            ProjectSetupStage2.AssignReference(view, "_titleLabel", title);
            ProjectSetupStage2.AssignReference(view, "_messageLabel", message);
            ProjectSetupStage2.AssignReference(view, "_primaryButton", primary);
            ProjectSetupStage2.AssignReference(view, "_primaryLabel", primaryLabel);
            ProjectSetupStage2.AssignReference(view, "_secondaryButton", secondary);
            ProjectSetupStage2.AssignReference(view, "_secondaryLabel", secondaryLabel);
            ProjectSetupStage2.AssignReference(view, "_menuButton", menu);
            panel.gameObject.SetActive(false);
        }
    }
}
