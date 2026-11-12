using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>
    /// Superficie que se mueve y lleva cosas encima (plataforma móvil).
    /// El futbolista la usa para moverse con ella y para medir su propia velocidad relativa al apoyo.
    /// </summary>
    public interface ISupportSurface
    {
        /// <summary>Velocidad (m/s) de la superficie en el último paso de física.</summary>
        Vector3 Velocity { get; }
    }
}
