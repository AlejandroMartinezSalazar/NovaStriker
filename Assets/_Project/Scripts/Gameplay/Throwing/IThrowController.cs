using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    public interface IThrowController
    {
        /// <summary>Hay un balón esperando en el punto de saque.</summary>
        bool HasBallReady { get; }

        string StrategyId { get; }

        void SetStrategy(IThrowStrategy strategy);

        /// <summary>Punto de saque del nivel. Si ya había un balón preparado, se aplica al siguiente.</summary>
        void SetSpawnPosition(Vector3 position);

        /// <summary>Coloca un balón en el punto de saque si no hay uno. Devuelve si quedó uno listo.</summary>
        bool PrepareBall();

        /// <summary>Olvida el balón preparado (p. ej. tras devolver todos al pool al reiniciar el nivel).</summary>
        void ClearReadyBall();

        /// <summary>Calcula el tiro sin lanzar (para la previsualización).</summary>
        bool TryPreview(Vector3 target, float curveBias, out Vector3 origin, out ThrowCommand command);

        /// <summary>Lanza el balón preparado hacia <paramref name="target"/> y publica ShotFiredEvent.</summary>
        bool TryThrow(Vector3 target, float curveBias);
    }
}
