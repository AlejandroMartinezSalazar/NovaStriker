using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Services;

namespace NovaStriker.UI.Localization
{
    /// <summary>
    /// La primera vez que se abre el juego (sin idioma elegido) muestra la pantalla de idioma con el del móvil
    /// resaltado; al tocar uno se aplica, se guarda y se cierra. Después ya no vuelve a salir: se cambia en Ajustes.
    /// </summary>
    public sealed class LanguagePickerPresenter : IDisposable
    {
        private readonly ILanguagePickerView _view;
        private readonly ILocalization _localization;

        public LanguagePickerPresenter(ILanguagePickerView view, ILocalization localization)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));

            _view.LanguageSelected += OnLanguageSelected;
            if (_localization.HasChosenLanguage)
            {
                _view.Hide();
            }
            else
            {
                _view.Show(_localization.Current);
            }
        }

        public void Dispose()
        {
            _view.LanguageSelected -= OnLanguageSelected;
        }

        private void OnLanguageSelected(Language language)
        {
            _localization.SetLanguage(language);
            _view.Hide();
        }
    }
}
