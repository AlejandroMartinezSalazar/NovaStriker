using System.Text;
using NovaStriker.UI.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NovaStriker.EditorTools.Setup.Ui
{
    /// <summary>
    /// Ajustes de sonido del menú (Etapa 9): engranaje arriba a la izquierda y panel con barras de Música y Efectos.
    /// Se añade al canvas del menú solo si no existe (se conservan tus ajustes de posición).
    /// </summary>
    internal static class SettingsUiBuilder
    {
        private const string RootName = "Settings";

        public static SettingsView Ensure(GameObject canvas, UiKit kit, StringBuilder log)
        {
            SettingsView existing = canvas.GetComponentInChildren<SettingsView>(true);
            if (existing != null)
            {
                return existing;
            }

            RectTransform viewRect = UiKit.Stretch(UiKit.Node(RootName, canvas.transform));
            viewRect.SetAsLastSibling(); // Por encima del menú y de "Próximamente".
            var view = viewRect.gameObject.AddComponent<SettingsView>();

            // Engranaje.
            RectTransform safe = UiKit.CreateSafeArea(viewRect);
            Button open = kit.CreateButton(safe, "Button_Settings", "", UiKit.Teal, new Vector2(130f, 130f), 10f, out TextMeshProUGUI openLabel);
            Image openImage = open.GetComponent<Image>();
            openImage.sprite = UiKit.LoadSprite(UiKit.CircleSprite);
            openImage.type = Image.Type.Simple;
            Object.DestroyImmediate(openLabel.gameObject);
            Image gear = UiKit.CreateImage(open.transform, "Icon", UiKit.GearSprite, Color.white);
            UiKit.Place(gear.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(76f, 76f));
            UiKit.Place((RectTransform)open.transform, new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(130f, 130f));

            // Panel.
            Image dim = UiKit.CreateImage(viewRect, "Panel", UiKit.PanelSprite, new Color(0f, 0f, 0f, 0.6f));
            dim.sprite = null;
            dim.type = Image.Type.Simple;
            dim.raycastTarget = true;
            RectTransform panel = UiKit.Stretch(dim.rectTransform);
            RectTransform panelSafe = UiKit.CreateSafeArea(panel);

            Image card = UiKit.CreateImage(panelSafe, "Card", UiKit.PanelSprite, UiKit.Navy);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(920f, 820f));
            UiKit.HexOverlay(card.rectTransform, 0.08f, 14f);

            Image banner = UiKit.CreateImage(card.rectTransform, "Banner", UiKit.BannerSprite, UiKit.Teal);
            UiKit.Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 120f), new Vector2(820f, 270f));
            TextMeshProUGUI title = kit.CreateText(banner.rectTransform, "Label_Title", "AJUSTES", 100f);
            RectTransform titleRect = UiKit.Stretch(title.rectTransform);
            titleRect.offsetMin = new Vector2(70f, 30f);
            titleRect.offsetMax = new Vector2(-70f, -20f);

            Slider music = VolumeRow(kit, card.rectTransform, "Music", "MÚSICA", -200f);
            Slider sfx = VolumeRow(kit, card.rectTransform, "Sfx", "EFECTOS", -420f);

            Button close = kit.CreateButton(card.rectTransform, "Button_Close", "CERRAR", UiKit.Green, new Vector2(560f, 160f), 64f, out _);
            UiKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(560f, 160f));

            ProjectSetupStage2.AssignReference(view, "_openButton", open);
            ProjectSetupStage2.AssignReference(view, "_panel", panel.gameObject);
            ProjectSetupStage2.AssignReference(view, "_musicSlider", music);
            ProjectSetupStage2.AssignReference(view, "_sfxSlider", sfx);
            ProjectSetupStage2.AssignReference(view, "_closeButton", close);
            panel.gameObject.SetActive(false);

            log.AppendLine("- Menú: engranaje de ajustes con volumen de Música y Efectos.");
            return view;
        }

        private static Slider VolumeRow(UiKit kit, RectTransform card, string name, string label, float y)
        {
            TextMeshProUGUI text = kit.CreateText(card, $"Label_{name}", label, 56f, TextAlignmentOptions.Left);
            UiKit.Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(760f, 80f));

            GameObject sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderObject.name = $"Slider_{name}";
            sliderObject.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)sliderObject.transform;
            rect.SetParent(card, false);
            UiKit.Place(rect, new Vector2(0.5f, 1f), new Vector2(0f, y - 100f), new Vector2(740f, 56f));

            var slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            Style(rect.Find("Background"), UiKit.PanelSprite, new Color(0.03f, 0.1f, 0.15f));
            Style(rect.Find("Fill Area/Fill"), UiKit.PanelSprite, UiKit.Green);
            Transform handle = rect.Find("Handle Slide Area/Handle");
            Style(handle, UiKit.CircleSprite, Color.white);
            if (handle != null)
            {
                ((RectTransform)handle).sizeDelta = new Vector2(84f, 40f);
                handle.GetComponent<Image>().preserveAspect = true;
            }

            return slider;
        }

        private static void Style(Transform target, string sprite, Color color)
        {
            if (target == null)
            {
                return;
            }

            var image = target.GetComponent<Image>();
            image.sprite = UiKit.LoadSprite(sprite);
            image.type = sprite == UiKit.CircleSprite ? Image.Type.Simple : Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = sprite == UiKit.CircleSprite ? 1f : 2f;
            image.color = color;
        }
    }
}
