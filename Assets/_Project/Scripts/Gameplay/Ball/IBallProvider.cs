using UnityEngine;

namespace NovaStriker.Gameplay.Ball
{
    /// <summary>Proporciona balones listos en una posición (normalmente desde el pool).</summary>
    public interface IBallProvider
    {
        IBall Spawn(Vector3 position);
    }
}
