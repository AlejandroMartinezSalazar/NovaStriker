using System.Collections.Generic;
using System.Text;
using NovaStriker.Bootstrap.Scenes;
using NovaStriker.Contracts.Data;
using NovaStriker.EditorTools.Setup.Ui;
using NovaStriker.UI.Localization;
using NovaStriker.UI.Settings;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Idiomas (español, inglés, portugués):
    /// - SC_MainMenu: pantalla de elegir idioma de la primera vez, fila de idiomas en Ajustes y textos fijos traducibles.
    /// - SC_Game: textos fijos traducibles (victoria, botón Menú, cartel del tutorial).
    /// Los textos que cambian con la partida (nivel, balones, pausa, derrota...) ya los traducen sus presentadores.
    /// Idempotente: se puede volver a ejecutar; rehace la pantalla de idioma y no duplica nada.
    /// </summary>
    internal static class LocalizationSetup
    {
        private const string MenuCanvas = "Canvas_Menu";
        private const string GameCanvas = "Canvas_Gameplay";
        private const string PickerName = "LanguagePicker";
        private const string LanguageRowName = "LanguageRow";
        private const float SettingsCardHeight = 1100f;

        /// <summary>Texto fijo → clave. Se busca por el final de su ruta dentro del canvas (y un tramo obligatorio).</summary>
        private readonly struct Target
        {
            public readonly string Within;
            public readonly string EndsWith;
            public readonly string Key;

            public Target(string within, string endsWith, string key)
            {
                Within = within;
                EndsWith = endsWith;
                Key = key;
            }
        }

        private static readonly Target[] MenuTexts =
        {
            new Target("MenuScreen/", "Button_Play/Label", TextKey.MenuPlay),
            new Target("ComingSoonScreen/", "Banner/Label_Title", TextKey.ComingSoonTitle),
            new Target("ComingSoonScreen/", "Card/Label_Subtitle", TextKey.ComingSoonSubtitle),
            new Target("ComingSoonScreen/", "Card/Label_Message", TextKey.ComingSoonMessage),
            new Target("ComingSoonScreen/", "Button_RestartCampaign/Label", TextKey.ComingSoonRestart),
            new Target("Settings/", "Banner/Label_Title", TextKey.SettingsTitle),
            new Target("Settings/", "Card/Label_Music", TextKey.SettingsMusic),
            new Target("Settings/", "Card/Label_Sfx", TextKey.SettingsSfx),
            new Target("Settings/", "Card/Label_Language", TextKey.SettingsLanguage),
            new Target("Settings/", "Button_Close/Label", TextKey.SettingsClose),
            new Target(PickerName + "/", "Banner/Label_Title", TextKey.LanguagePickerTitle),
        };

        private static readonly Target[] GameTexts =
        {
            new Target("Victory/", "Banner/Label_Title", TextKey.VictoryTitle),
            new Target("Results/", "Button_Menu/Label", TextKey.ResultsMenu),
            new Target("TutorialHint/", "MessagePanel/Label", TextKey.TutorialDrag),
        };

        [MenuItem("NovaStriker/Setup/Idiomas")]
        private static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[NovaStriker Setup] Cancelado: hay escenas sin guardar.");
                return;
            }

            var log = new StringBuilder("[NovaStriker Setup] Idiomas\n");
            int warnings = 0;
            bool ok = UiKit.PrepareAssets(log) && SetupMenu(log, ref warnings) && SetupGame(log, ref warnings);

            if (!ok)
            {
                Debug.LogError(log.ToString());
            }
            else if (warnings > 0)
            {
                Debug.LogWarning(log.ToString());
            }
            else
            {
                Debug.Log(log.ToString());
            }
        }

        // ---------- Menú ----------

        private static bool SetupMenu(StringBuilder log, ref int warnings)
        {
            Scene scene = EditorSceneManager.OpenScene(ProjectSetupStage0.ScenePath(SceneNames.MainMenu), OpenSceneMode.Single);
            UiKit kit = UiKit.Load(log);
            var root = ProjectSetupStage3.FindRootComponent<MainMenuCompositionRoot>(scene);
            GameObject canvas = ProjectSetupStage3.FindRoot(scene, MenuCanvas);
            if (kit == null || root == null || canvas == null)
            {
                log.AppendLine($"- ERROR: SC_MainMenu necesita la fuente, MainMenuCompositionRoot y {MenuCanvas}.");
                return false;
            }

            SettingsView settings = canvas.GetComponentInChildren<SettingsView>(true);
            if (settings == null)
            {
                log.AppendLine("- ERROR: no hay SettingsView en el menú. Ejecuta antes la Etapa 9.");
                return false;
            }

            if (!AddSettingsLanguageRow(kit, settings, log))
            {
                return false;
            }

            LanguagePickerView picker = BuildPicker(kit, (RectTransform)canvas.transform);
            ProjectSetupStage2.AssignReference(root, "_languagePickerView", picker);
            log.AppendLine($"- {MenuCanvas}/{PickerName}: pantalla de idioma de la primera vez.");

            warnings += Localize(canvas.transform, MenuTexts, log);
            ProjectSetupStage2.SaveScene(scene);

            return ProjectSetupStage3.VerifyNoMissingReferences(picker, log)
                   && ProjectSetupStage3.VerifyNoMissingReferences(settings, log)
                   && ProjectSetupStage3.VerifyNoMissingReferences(root, log);
        }

        /// <summary>Añade "IDIOMA" + tres botones al panel de ajustes (agranda la tarjeta para que quepan).</summary>
        private static bool AddSettingsLanguageRow(UiKit kit, SettingsView settings, StringBuilder log)
        {
            Transform card = FindByPath(settings.transform, "Card");
            if (card == null)
            {
                log.AppendLine("- ERROR: no encontré la tarjeta (Card) del panel de ajustes.");
                return false;
            }

            Transform existing = card.Find(LanguageRowName);
            LanguageButtons buttons;
            if (existing != null)
            {
                buttons = existing.GetComponent<LanguageButtons>();
                log.AppendLine("- Ajustes: la fila de idiomas ya existía.");
            }
            else
            {
                var cardRect = (RectTransform)card;
                if (cardRect.sizeDelta.y < SettingsCardHeight)
                {
                    cardRect.sizeDelta = new Vector2(cardRect.sizeDelta.x, SettingsCardHeight);
                }

                TextMeshProUGUI label = kit.CreateText(card, "Label_Language", "IDIOMA", 56f, TextAlignmentOptions.Left);
                UiKit.Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -620f), new Vector2(760f, 80f));

                RectTransform row = UiKit.Node(LanguageRowName, card);
                UiKit.Place(row, new Vector2(0.5f, 1f), new Vector2(0f, -720f), new Vector2(820f, 120f));
                buttons = BuildButtons(kit, row, horizontal: true);
                log.AppendLine("- Ajustes: fila de idiomas (Español, English, Português).");
            }

            ProjectSetupStage2.AssignReference(settings, "_languageButtons", buttons);
            return buttons != null;
        }

        private static LanguagePickerView BuildPicker(UiKit kit, RectTransform canvas)
        {
            Transform old = canvas.Find(PickerName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            RectTransform rootRect = UiKit.Stretch(UiKit.Node(PickerName, canvas));
            rootRect.SetAsLastSibling(); // Por encima de todo el menú, ajustes incluidos.
            var view = rootRect.gameObject.AddComponent<LanguagePickerView>();

            Image dim = UiKit.CreateImage(rootRect, "Panel", UiKit.PanelSprite, new Color(0.03f, 0.1f, 0.15f, 0.96f));
            dim.sprite = null;
            dim.type = Image.Type.Simple;
            dim.raycastTarget = true; // Bloquea el menú hasta elegir.
            RectTransform panel = UiKit.Stretch(dim.rectTransform);
            RectTransform safe = UiKit.CreateSafeArea(panel);

            Image card = UiKit.CreateImage(safe, "Card", UiKit.PanelSprite, UiKit.Navy);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(920f, 900f));
            UiKit.HexOverlay(card.rectTransform, 0.08f, 14f);

            Image banner = UiKit.CreateImage(card.rectTransform, "Banner", UiKit.BannerSprite, UiKit.Teal);
            UiKit.Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 120f), new Vector2(880f, 270f));
            TextMeshProUGUI title = kit.CreateText(banner.rectTransform, "Label_Title", "ELIGE TU IDIOMA", 84f);
            RectTransform titleRect = UiKit.Stretch(title.rectTransform);
            titleRect.offsetMin = new Vector2(70f, 30f);
            titleRect.offsetMax = new Vector2(-70f, -20f);

            RectTransform column = UiKit.Node("Buttons", card.rectTransform);
            UiKit.Place(column, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(720f, 600f));
            LanguageButtons buttons = BuildButtons(kit, column, horizontal: false);

            ProjectSetupStage2.AssignReference(view, "_panel", dim.gameObject);
            ProjectSetupStage2.AssignReference(view, "_buttons", buttons);
            dim.gameObject.SetActive(false); // La enseña el presentador si aún no se eligió idioma.
            return view;
        }

        /// <summary>Tres botones con el nombre de cada idioma en su propio idioma.</summary>
        private static LanguageButtons BuildButtons(UiKit kit, RectTransform parent, bool horizontal)
        {
            var buttons = parent.gameObject.AddComponent<LanguageButtons>();
            Vector2 size = horizontal ? new Vector2(260f, 120f) : new Vector2(720f, 170f);
            float fontSize = horizontal ? 44f : 72f;
            string[] names = { "ESPAÑOL", "ENGLISH", "PORTUGUÊS" };
            string[] fields = { "_spanish", "_english", "_portuguese" };

            for (int i = 0; i < names.Length; i++)
            {
                Button button = kit.CreateButton(parent, $"Button_{fields[i].TrimStart('_')}", names[i], UiKit.Teal, size, fontSize, out _);
                Vector2 position = horizontal
                    ? new Vector2((i - 1) * (size.x + 20f), 0f)
                    : new Vector2(0f, -i * (size.y + 25f));
                Vector2 anchor = horizontal ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f);
                UiKit.Place((RectTransform)button.transform, anchor, position, size);
                ProjectSetupStage2.AssignReference(buttons, fields[i], button);
            }

            return buttons;
        }

        // ---------- Juego ----------

        private static bool SetupGame(StringBuilder log, ref int warnings)
        {
            Scene scene = EditorSceneManager.OpenScene(ProjectSetupStage0.ScenePath(SceneNames.Game), OpenSceneMode.Single);
            GameObject canvas = ProjectSetupStage3.FindRoot(scene, GameCanvas);
            if (canvas == null)
            {
                log.AppendLine($"- ERROR: no existe {GameCanvas} en SC_Game.");
                return false;
            }

            warnings += Localize(canvas.transform, GameTexts, log);
            ProjectSetupStage2.SaveScene(scene);
            return true;
        }

        // ---------- Textos fijos ----------

        /// <summary>Añade (o actualiza) LocalizedText en cada texto fijo. Devuelve cuántos no se encontraron.</summary>
        private static int Localize(Transform canvas, IEnumerable<Target> targets, StringBuilder log)
        {
            TMP_Text[] labels = canvas.GetComponentsInChildren<TMP_Text>(true);
            int missing = 0;
            int done = 0;

            foreach (Target target in targets)
            {
                bool found = false;
                foreach (TMP_Text label in labels)
                {
                    string path = PathOf(label.transform, canvas);
                    if (!path.Contains(target.Within) || !path.EndsWith(target.EndsWith))
                    {
                        continue;
                    }

                    LocalizedText localized = label.GetComponent<LocalizedText>();
                    if (localized == null)
                    {
                        localized = label.gameObject.AddComponent<LocalizedText>();
                    }

                    var serialized = new SerializedObject(localized);
                    serialized.FindProperty("_key").stringValue = target.Key;
                    serialized.FindProperty("_label").objectReferenceValue = label;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    found = true;
                    done++;
                }

                if (!found)
                {
                    log.AppendLine($"- AVISO: no encontré el texto '{target.Within}…/{target.EndsWith}' ({target.Key}) en {canvas.name}. " +
                                   "Añádele a mano el componente LocalizedText con esa clave.");
                    missing++;
                }
            }

            log.AppendLine($"- {canvas.name}: {done} textos fijos traducibles.");
            return missing;
        }

        private static string PathOf(Transform transform, Transform root)
        {
            var parts = new List<string>();
            for (Transform t = transform; t != null && t != root; t = t.parent)
            {
                parts.Add(t.name);
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static Transform FindByPath(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
