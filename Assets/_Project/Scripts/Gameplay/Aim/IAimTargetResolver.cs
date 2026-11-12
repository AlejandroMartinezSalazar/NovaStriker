using UnityEngine;

namespace NovaStriker.Gameplay.Aim
{
    /// <summary>Convierte una posición de pantalla en el punto del mundo al que apunta el dedo.</summary>
    public interface IAimTargetResolver
    {
        bool TryResolve(Vector2 screenPosition, out Vector3 worldPoint);
    }
}
