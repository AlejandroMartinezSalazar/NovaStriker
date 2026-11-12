using System;
using System.Collections.Generic;

namespace NovaStriker.UI.MainMenu
{
    /// <summary>Menú principal: panel de nivel con cuadraditos y botón Jugar, o la pantalla "Próximamente".</summary>
    public interface IMainMenuView
    {
        event Action PlayClicked;
        event Action RestartCampaignClicked;

        /// <summary>Muestra el menú normal con el texto del nivel y los cuadraditos del grupo actual.</summary>
        void ShowLevel(string levelText, IReadOnlyList<LevelSquareState> squares);

        /// <summary>Muestra la pantalla aparte "Próximamente nuevos niveles" (D1).</summary>
        void ShowComingSoon();
    }
}
