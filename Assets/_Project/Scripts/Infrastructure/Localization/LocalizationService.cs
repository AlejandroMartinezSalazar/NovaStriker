using System;
using System.Collections.Generic;
using System.Globalization;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Services;
using UnityEngine;

namespace NovaStriker.Infrastructure.Localization
{
    /// <summary>
    /// ILocalization con la tabla de StringTable. El idioma elegido se guarda en IGameSettings; mientras no se
    /// elige, se usa el del móvil (si es uno de los tres) o inglés. Una clave sin traducir se muestra como
    /// "[clave]" y se avisa en consola, para que se note en pruebas y no rompa el juego.
    /// </summary>
    public sealed class LocalizationService : ILocalization
    {
        private static readonly Language[] Languages = { Language.Spanish, Language.English, Language.Portuguese };

        private readonly IGameSettings _settings;
        private readonly HashSet<string> _reportedMissing = new HashSet<string>();

        public LocalizationService(IGameSettings settings, Language deviceLanguage)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Current = settings.Language != Language.None ? settings.Language : Supported(deviceLanguage);
        }

        public Language Current { get; private set; }
        public bool HasChosenLanguage => _settings.Language != Language.None;
        public IReadOnlyList<Language> Available => Languages;

        public event Action LanguageChanged;

        public string Get(string key)
        {
            string text = StringTable.Find(key, Current);
            if (text != null)
            {
                return text;
            }

            if (_reportedMissing.Add(key ?? "(null)"))
            {
                Debug.LogWarning($"[NovaStriker] Texto sin traducir: '{key}'.");
            }

            return $"[{key}]";
        }

        public string Format(string key, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), args);
        }

        public void SetLanguage(Language language)
        {
            if (Array.IndexOf(Languages, language) < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(language), $"Idioma no disponible: {language}.");
            }

            bool changed = language != Current;
            Current = language;
            _settings.SetLanguage(language);
            _settings.Save();

            if (changed)
            {
                LanguageChanged?.Invoke();
            }
        }

        /// <summary>El idioma del móvil si el juego lo tiene; si no, inglés.</summary>
        public static Language Supported(Language deviceLanguage) =>
            Array.IndexOf(Languages, deviceLanguage) >= 0 ? deviceLanguage : Language.English;

        /// <summary>Traduce el idioma del sistema de Unity a los del juego (None si no es uno de ellos).</summary>
        public static Language FromSystemLanguage(SystemLanguage systemLanguage)
        {
            switch (systemLanguage)
            {
                case SystemLanguage.Spanish:
                    return Language.Spanish;
                case SystemLanguage.English:
                    return Language.English;
                case SystemLanguage.Portuguese:
                    return Language.Portuguese;
                default:
                    return Language.None;
            }
        }
    }
}
