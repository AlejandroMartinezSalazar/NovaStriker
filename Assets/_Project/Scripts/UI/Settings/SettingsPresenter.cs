using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Services;

namespace NovaStriker.UI.Settings
{
    /// <summary>
    /// Ajustes de sonido: las barras cambian el volumen al momento y se guarda al cerrar el panel
    /// (y al salir del menú, por si se cierra la app con el panel abierto). El idioma se aplica y guarda al tocarlo.
    /// </summary>
    public sealed class SettingsPresenter : IDisposable
    {
        private readonly ISettingsView _view;
        private readonly IGameSettings _settings;
        private readonly ILocalization _localization;

        public SettingsPresenter(ISettingsView view, IGameSettings settings, ILocalization localization)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));

            _view.OpenClicked += OnOpen;
            _view.CloseClicked += OnClose;
            _view.MusicVolumeChanged += _settings.SetMusicVolume;
            _view.SfxVolumeChanged += _settings.SetSfxVolume;
            _view.LanguageSelected += OnLanguageSelected;
            _view.HidePanel();
        }

        public void Dispose()
        {
            _view.OpenClicked -= OnOpen;
            _view.CloseClicked -= OnClose;
            _view.MusicVolumeChanged -= _settings.SetMusicVolume;
            _view.SfxVolumeChanged -= _settings.SetSfxVolume;
            _view.LanguageSelected -= OnLanguageSelected;
            _settings.Save();
        }

        private void OnOpen()
        {
            _view.SetSelectedLanguage(_localization.Current);
            _view.ShowPanel(_settings.MusicVolume, _settings.SfxVolume);
        }

        private void OnLanguageSelected(Language language)
        {
            _localization.SetLanguage(language);
            _view.SetSelectedLanguage(language);
        }

        private void OnClose()
        {
            _settings.Save();
            _view.HidePanel();
        }
    }
}
