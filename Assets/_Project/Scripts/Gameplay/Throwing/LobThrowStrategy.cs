using System;
using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    [Serializable]
    public struct LobThrowSettings
    {
        [Tooltip("Altura del vértice (m) por encima del punto más alto entre origen y objetivo.")]
        [Min(0.1f)] public float ApexHeight;

        [Tooltip("Además, el vértice sube esta fracción de la distancia horizontal (tiros largos más altos).")]
        [Min(0f)] public float ApexPerMeter;

        [Tooltip("Giro visual del balón (rad/s).")]
        public float SpinRate;

        public static LobThrowSettings Default => new LobThrowSettings
        {
            ApexHeight = 2.5f,
            ApexPerMeter = 0.15f,
            SpinRate = 6f,
        };
    }

    /// <summary>Tiro bombeado: el tiempo de vuelo sale de la altura del vértice deseada.</summary>
    public sealed class LobThrowStrategy : IThrowStrategy
    {
        public const string StrategyId = "lob";

        private readonly LobThrowSettings _settings;

        public LobThrowStrategy(LobThrowSettings settings)
        {
            _settings = settings;
        }

        public string Id => StrategyId;

        public bool TryCreateThrow(in ThrowContext context, out ThrowCommand command)
        {
            command = default;

            // Objetivo degenerado (bajo el propio balón): igual que el resto de estrategias, no se lanza.
            if (Vector3.Distance(context.Origin, context.Target) < 1e-3f)
            {
                return false;
            }

            // La gravedad debe apuntar hacia abajo para que exista un vértice.
            float gravity = -context.Gravity.y;
            if (gravity <= 1e-4f)
            {
                return false;
            }

            Vector3 horizontal = context.Target - context.Origin;
            horizontal.y = 0f;
            float horizontalDistance = horizontal.magnitude;

            float apexY = Mathf.Max(context.Origin.y, context.Target.y)
                          + _settings.ApexHeight
                          + _settings.ApexPerMeter * horizontalDistance;

            float timeUp = Mathf.Sqrt(2f * (apexY - context.Origin.y) / gravity);
            float timeDown = Mathf.Sqrt(2f * (apexY - context.Target.y) / gravity);
            float flightTime = BallisticSolver.QuantizeFlightTime(timeUp + timeDown, context.FixedDeltaTime);

            Vector3 velocity = BallisticSolver.SolveInitialVelocity(
                context.Origin, context.Target, context.Gravity, flightTime, context.FixedDeltaTime);

            Vector3 direction = BallisticSolver.HorizontalDirection(context.Origin, context.Target);
            command = new ThrowCommand(velocity, Vector3.zero, flightTime, BallisticSolver.ForwardSpin(direction, _settings.SpinRate));
            return true;
        }
    }
}
