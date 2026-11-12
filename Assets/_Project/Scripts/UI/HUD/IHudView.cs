using System;

namespace NovaStriker.UI.HUD
{
    /// <summary>HUD de la partida: textos de nivel, objetivos y balones, y botón de pausa.</summary>
    public interface IHudView
    {
        event Action PauseClicked;

        void SetLevelText(string text);
        void SetTargetsText(string text);
        void SetBallsText(string text);
        void SetPauseVisible(bool visible);
    }
}
