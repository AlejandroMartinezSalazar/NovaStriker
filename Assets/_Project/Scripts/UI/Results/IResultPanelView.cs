using System;

namespace NovaStriker.UI.Results
{
    /// <summary>Panel superpuesto para pausa, victoria, derrota y "Próximamente".</summary>
    public interface IResultPanelView
    {
        event Action PrimaryClicked;
        event Action SecondaryClicked;
        event Action MenuClicked;

        void Show(in ResultPanelContent content);
        void Hide();
    }
}
