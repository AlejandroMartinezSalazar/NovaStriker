using System;
using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    [Serializable]
    public struct CurveThrowSettings
    {
        [Tooltip("Parámetros de tiempo de vuelo (igual que el tiro recto). Conviene algo más lento para que se note la curva.")]
        public StraightThrowSettings Timing;

        [Tooltip("Aceleración lateral máxima (m/s²) con el gesto totalmente arqueado.")]
        [Min(0f)] public float MaxLateralAcceleration;

        [Tooltip("Fracción mínima de curva cuando el gesto es casi recto (siempre hay algo de efecto).")]
        [Range(0f, 1f)] public float MinCurveFraction;

        [Tooltip("Lado de la curva cuando el gesto es recto: +1 se abre a la derecha, -1 a la izquierda.")]
        public float DefaultSide;

        [Tooltip("Por debajo de este |bias| el gesto se considera recto.")]
        [Range(0f, 1f)] public float BiasDeadZone;

        public static CurveThrowSettings Default => new CurveThrowSettings
        {
            Timing = new StraightThrowSettings { Speed = 16f, MinFlightTime = 0.45f, MaxFlightTime = 1.6f, SpinRate = 10f },
            MaxLateralAcceleration = 14f,
            MinCurveFraction = 0.35f,
            DefaultSide = 1f,
            BiasDeadZone = 0.1f,
        };
    }

    /// <summary>
    /// Tiro con efecto: aceleración lateral constante. La velocidad inicial la compensa, así que el balón se abre
    /// hacia el lado del arco del gesto y vuelve hasta el objetivo.
    /// </summary>
    public sealed class CurveThrowStrategy : IThrowStrategy
    {
        public const string StrategyId = "curve";

        /// <summary>Velocidad de giro visual alrededor del eje vertical por unidad de bias.</summary>
        private const float SideSpinRate = 25f;

        private readonly CurveThrowSettings _settings;

        public CurveThrowStrategy(CurveThrowSettings settings)
        {
            _settings = settings;
        }

        public string Id => StrategyId;

        /// <summary>
        /// Lado efectivo de la curva en [-1, 1]. Positivo: la trayectoria se abre hacia la derecha.
        /// Se expone para tests.
        /// </summary>
        public float ResolveSide(float curveBias)
        {
            float bias = Mathf.Clamp(curveBias, -1f, 1f);
            if (Mathf.Abs(bias) < _settings.BiasDeadZone)
            {
                return Mathf.Sign(_settings.DefaultSide == 0f ? 1f : _settings.DefaultSide) * _settings.MinCurveFraction;
            }

            return Mathf.Sign(bias) * Mathf.Max(Mathf.Abs(bias), _settings.MinCurveFraction);
        }

        public bool TryCreateThrow(in ThrowContext context, out ThrowCommand command)
        {
            float distance = Vector3.Distance(context.Origin, context.Target);
            if (distance < 1e-3f)
            {
                command = default;
                return false;
            }

            StraightThrowSettings timing = _settings.Timing;
            float flightTime = Mathf.Clamp(distance / timing.Speed, timing.MinFlightTime, timing.MaxFlightTime);
            flightTime = BallisticSolver.QuantizeFlightTime(flightTime, context.FixedDeltaTime);

            Vector3 direction = BallisticSolver.HorizontalDirection(context.Origin, context.Target);
            Vector3 right = Vector3.Cross(Vector3.up, direction);
            float side = ResolveSide(context.CurveBias);

            // Para que la trayectoria se abra a la derecha, la aceleración debe empujar hacia la izquierda (hacia dentro).
            Vector3 lateralAcceleration = -right * (side * _settings.MaxLateralAcceleration);
            Vector3 totalAcceleration = context.Gravity + lateralAcceleration;

            Vector3 velocity = BallisticSolver.SolveInitialVelocity(
                context.Origin, context.Target, totalAcceleration, flightTime, context.FixedDeltaTime);

            Vector3 spin = BallisticSolver.ForwardSpin(direction, timing.SpinRate) + Vector3.up * (-side * SideSpinRate);
            command = new ThrowCommand(velocity, lateralAcceleration, flightTime, spin);
            return true;
        }
    }
}
