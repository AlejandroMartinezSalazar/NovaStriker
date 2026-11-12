using UnityEngine;

namespace NovaStriker.Gameplay.Input
{
    /// <summary>Indica si una posición de pantalla está sobre la UI (p. ej. el botón de pausa).</summary>
    public interface IUiHitTester
    {
        bool IsOverUi(Vector2 screenPosition);
    }
}
