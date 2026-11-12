using System;
using NovaStriker.Contracts.Data;

namespace NovaStriker.UI.Settings
{
    /// <summary>Botón de ajustes (engranaje) y su panel con las barras de volumen y los botones de idioma.</summary>
    public interface ISettingsView
    {
        event Action OpenClicked;
        event Action CloseClicked;
        event Action<float> MusicVolumeChanged;
        event Action<float> SfxVolumeChanged;
        event Action<Language> LanguageSelected;

        /// <summary>Abre el panel con los valores actuales (sin disparar los eventos de cambio).</summary>
        void ShowPanel(float musicVolume, float sfxVolume);

        void HidePanel();

        /// <summary>Resalta el idioma actual en la fila de idiomas.</summary>
        void SetSelectedLanguage(Language language);
    }
}
