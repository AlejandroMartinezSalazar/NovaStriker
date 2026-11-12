using System;
using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    [Serializable]
    public struct StraightThrowSettings
    {
        [Tooltip("Velocidad media de referencia (m/s). El tiempo de vuelo es distancia / velocidad.")]
        [Min(0.1f)] public float Speed;

        [Tooltip("Tiempo de vuelo mínimo (s), para tiros muy cortos.")]
        [Min(0.02f)] public float MinFlightTime;

        [Tooltip("Tiempo de vuelo máximo (s), para tiros muy largos.")]
        [Min(0.02f)] public float MaxFlightTime;

        [Tooltip("Giro visual del balón (rad/s).")]
        public float SpinRate;

        public static StraightThrowSettings Default => new StraightThrowSettings
        {
            Speed = 22f,
            MinFlightTime = 0.25f,
            MaxFlightTime = 1.4f,
            SpinRate = 12f,
        };
    }

    /// <summary>Tiro tenso: tiempo de vuelo corto, proporcional a la distancia, solo con gravedad.</summary>
    public sealed class StraightThrowStrategy : IThrowStrategy
    {
        public const string StrategyId = "straight";

        private readonly StraightThrowSettings _settings;

        public StraightThrowStrategy(StraightThrowSettings settings)
        {
            _settings = settings;
        }

        public string Id => StrategyId;

        public bool TryCreateThrow(in ThrowContext context, out ThrowCommand command)
        {
            float distance = Vector3.Distance(context.Origin, context.Target);
            if (distance < 1e-3f)
            {
                command = default;
                return false;
            }

            float flightTime = Mathf.Clamp(distance / _settings.Speed, _settings.MinFlightTime, _settings.MaxFlightTime);
            flightTime = BallisticSolver.QuantizeFlightTime(flightTime, context.FixedDeltaTime);

            Vector3 velocity = BallisticSolver.SolveInitialVelocity(
                context.Origin, context.Target, context.Gravity, flightTime, context.FixedDeltaTime);

            Vector3 direction = BallisticSolver.HorizontalDirection(context.Origin, context.Target);
            command = new ThrowCommand(velocity, Vector3.zero, flightTime, BallisticSolver.ForwardSpin(direction, _settings.SpinRate));
            return true;
        }
    }
}
