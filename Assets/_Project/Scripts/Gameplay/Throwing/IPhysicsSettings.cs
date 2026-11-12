using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    /// <summary>Parámetros de la física que necesita el cálculo del tiro. Inyectable para tests.</summary>
    public interface IPhysicsSettings
    {
        Vector3 Gravity { get; }
        float FixedDeltaTime { get; }
    }
}
