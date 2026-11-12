using UnityEngine;

namespace NovaStriker.Gameplay.Level
{
    /// <summary>Coloca la cámara de juego en el punto que define cada nivel.</summary>
    public interface ILevelCamera
    {
        void MoveTo(Vector3 position, Quaternion rotation);
    }
}
