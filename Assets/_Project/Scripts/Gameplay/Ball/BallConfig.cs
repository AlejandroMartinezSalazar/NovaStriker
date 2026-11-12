using UnityEngine;

namespace NovaStriker.Gameplay.Ball
{
    [CreateAssetMenu(menuName = "NovaStriker/Config/Ball Config", fileName = "SO_BallConfig")]
    public sealed class BallConfig : ScriptableObject
    {
        [Header("Fin del recorrido")]
        [Tooltip("Por debajo de esta rapidez (m/s) el balón cuenta como quieto.")]
        [Min(0f)] public float SettleSpeed = 0.2f;

        [Tooltip("Tiempo seguido (s) quieto para darlo por detenido.")]
        [Min(0f)] public float SettleDuration = 0.5f;

        [Tooltip("Tiempo máximo (s) desde el lanzamiento.")]
        [Min(0.5f)] public float MaxLifetime = 8f;

        [Header("Eventos")]
        [Tooltip("Impulso mínimo (N·s) de un choque para publicar BallCollidedEvent (audio, VFX).")]
        [Min(0f)] public float MinCollisionImpulse = 0.3f;

        [Header("Pool")]
        [Tooltip("Máximo de balones en escena; al superarlo se recicla el más antiguo.")]
        [Min(1)] public int MaxActiveBalls = 8;
    }
}
