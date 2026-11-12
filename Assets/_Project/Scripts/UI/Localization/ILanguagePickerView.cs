using System;
using NovaStriker.Contracts.Data;

namespace NovaStriker.UI.Localization
{
    /// <summary>Pantalla para elegir idioma la primera vez que se abre el juego.</summary>
    public interface ILanguagePickerView
    {
        event Action<Language> LanguageSelected;

        /// <summary>Muestra la pantalla con <paramref name="preselected"/> resaltado (el idioma del móvil).</summary>
        void Show(Language preselected);

        void Hide();
    }
}
