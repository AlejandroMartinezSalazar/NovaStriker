using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NovaStriker.Contracts.Data;
using NovaStriker.Infrastructure.Localization;
using NovaStriker.Infrastructure.Save;
using NovaStriker.Infrastructure.Settings;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Infrastructure
{
    public class LocalizationTests
    {
        private static readonly Language[] Languages = { Language.Spanish, Language.English, Language.Portuguese };

        [Test]
        public void EveryKey_IsTranslated_InAllLanguages()
        {
            foreach (string key in TextKey.All)
            {
                foreach (Language language in Languages)
                {
                    string text = StringTable.Find(key, language);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"Falta '{key}' en {language}.");
                }
            }

            CollectionAssert.AreEquivalent(TextKey.All, StringTable.Keys.ToArray(), "TextKey.All y StringTable deben tener las mismas claves.");
        }

        [Test]
        public void Placeholders_MatchAcrossLanguages()
        {
            foreach (string key in TextKey.All)
            {
                string[] spanish = Placeholders(StringTable.Find(key, Language.Spanish));
                CollectionAssert.AreEquivalent(spanish, Placeholders(StringTable.Find(key, Language.English)), $"{key} (inglés)");
                CollectionAssert.AreEquivalent(spanish, Placeholders(StringTable.Find(key, Language.Portuguese)), $"{key} (portugués)");
            }
        }

        [Test]
        public void NoChoiceYet_UsesDeviceLanguage_OrEnglish()
        {
            var portuguese = new LocalizationService(new MemorySettings(), Language.Portuguese);
            Assert.IsFalse(portuguese.HasChosenLanguage);
            Assert.AreEqual(Language.Portuguese, portuguese.Current);
            Assert.AreEqual("JOGAR", portuguese.Get(TextKey.MenuPlay));

            var other = new LocalizationService(new MemorySettings(), Language.None);
            Assert.AreEqual(Language.English, other.Current, "Un idioma que el juego no tiene → inglés.");
            Assert.AreEqual(Language.Spanish, LocalizationService.FromSystemLanguage(SystemLanguage.Spanish));
            Assert.AreEqual(Language.None, LocalizationService.FromSystemLanguage(SystemLanguage.French));
        }

        [Test]
        public void SetLanguage_ChangesTexts_SavesChoice_AndNotifies()
        {
            var settings = new MemorySettings();
            var localization = new LocalizationService(settings, Language.Spanish);
            int notifications = 0;
            localization.LanguageChanged += () => notifications++;

            localization.SetLanguage(Language.English);

            Assert.IsTrue(localization.HasChosenLanguage);
            Assert.AreEqual(Language.English, settings.Language);
            Assert.AreEqual(1, settings.Saves, "Se guarda al momento.");
            Assert.AreEqual(1, notifications);
            Assert.AreEqual("LEVEL 3", localization.Format(TextKey.MenuLevel, 3));
            Assert.AreEqual("To knock down: 1/4", localization.Format(TextKey.HudTargets, 1, 4));

            localization.SetLanguage(Language.English);
            Assert.AreEqual(1, notifications, "Elegir el mismo idioma no vuelve a avisar.");
        }

        [Test]
        public void ChosenLanguage_WinsOverDevice_AfterRestart()
        {
            var storage = new MemoryStorage();
            var first = new PersistentGameSettings(storage, new JsonUtilitySerializer());
            new LocalizationService(first, Language.Spanish).SetLanguage(Language.Portuguese);

            var reopened = new PersistentGameSettings(storage, new JsonUtilitySerializer());
            var localization = new LocalizationService(reopened, Language.English);

            Assert.IsTrue(localization.HasChosenLanguage);
            Assert.AreEqual(Language.Portuguese, localization.Current);
        }

        [Test]
        public void OldSettingsFile_WithoutLanguage_AsksAgain()
        {
            var storage = new MemoryStorage();
            storage.Write(PersistentGameSettings.SaveKeyName, "{\"SaveVersion\":1,\"MusicVolume\":0.5,\"SfxVolume\":1}");
            var settings = new PersistentGameSettings(storage, new JsonUtilitySerializer());

            Assert.AreEqual(Language.None, settings.Language);
            Assert.AreEqual(0.5f, settings.MusicVolume, 1e-5f, "Los volúmenes guardados se conservan.");
        }

        [Test]
        public void MissingKey_ShowsKeyInBrackets()
        {
            var localization = new LocalizationService(new MemorySettings(), Language.Spanish);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new Regex("sin traducir"));

            Assert.AreEqual("[no.existe]", localization.Get("no.existe"));
        }

        private static string[] Placeholders(string text) =>
            Regex.Matches(text, @"\{\d+\}").Cast<Match>().Select(m => m.Value).ToArray();

        private sealed class MemoryStorage : ISaveStorage
        {
            private readonly Dictionary<string, string> _files = new Dictionary<string, string>();
            public bool Exists(string key) => _files.ContainsKey(key);
            public string Read(string key) => _files.TryGetValue(key, out string value) ? value : null;
            public void Write(string key, string data) => _files[key] = data;
            public void Delete(string key) => _files.Remove(key);
        }
    }
}
