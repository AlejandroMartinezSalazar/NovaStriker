using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Data;

namespace NovaStriker.Contracts.Services
{
    /// <summary>
    /// Textos del juego en el idioma elegido (español, inglés o portugués).
    /// Mientras el jugador no elige, se usa el idioma del móvil (o inglés si no es uno de los tres).
    /// </summary>
    public interface ILocalization
    {
        Language Current { get; }

        /// <summary>False hasta que el jugador elige idioma (primera vez que abre el juego).</summary>
        bool HasChosenLanguage { get; }

        IReadOnlyList<Language> Available { get; }

        /// <summary>Se dispara al cambiar de idioma: las vistas vuelven a pedir sus textos.</summary>
        event Action LanguageChanged;

        string Get(string key);

        string Format(string key, params object[] args);

        /// <summary>Cambia de idioma y lo guarda como elegido por el jugador.</summary>
        void SetLanguage(Language language);
    }
}
