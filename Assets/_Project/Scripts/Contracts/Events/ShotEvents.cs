using NovaStriker.Contracts.Data;
using NovaStriker.Core.Events;
using UnityEngine;

namespace NovaStriker.Contracts.Events
{
    /// <summary>El jugador puso el dedo y empezó a apuntar.</summary>
    public readonly struct AimStartedEvent : IGameEvent
    {
    }

    /// <summary>El punto objetivo cambió mientras se arrastra. HasTarget=false si el dedo no apunta a nada válido.</summary>
    public readonly struct AimUpdatedEvent : IGameEvent
    {
        public readonly Vector3 Target;
        public readonly bool HasTarget;

        public AimUpdatedEvent(Vector3 target, bool hasTarget)
        {
            Target = target;
            HasTarget = hasTarget;
        }
    }

    /// <summary>Se levantó el dedo sin disparar (sin objetivo válido o toque demasiado corto).</summary>
    public readonly struct AimCancelledEvent : IGameEvent
    {
    }

    /// <summary>Se lanzó un balón.</summary>
    public readonly struct ShotFiredEvent : IGameEvent
    {
        public readonly string StrategyId;
        public readonly Vector3 Origin;
        public readonly Vector3 Target;
        public readonly Vector3 InitialVelocity;

        public ShotFiredEvent(string strategyId, Vector3 origin, Vector3 target, Vector3 initialVelocity)
        {
            StrategyId = strategyId;
            Origin = origin;
            Target = target;
            InitialVelocity = initialVelocity;
        }
    }

    /// <summary>El balón chocó con algo con un impulso mayor que el umbral configurado.</summary>
    public readonly struct BallCollidedEvent : IGameEvent
    {
        public readonly Vector3 Point;

        /// <summary>Normal del contacto (hacia arriba = suelo; horizontal = pared).</summary>
        public readonly Vector3 Normal;

        public readonly float Impulse;
        public readonly int OtherLayer;

        public BallCollidedEvent(Vector3 point, Vector3 normal, float impulse, int otherLayer)
        {
            Point = point;
            Normal = normal;
            Impulse = impulse;
            OtherLayer = otherLayer;
        }
    }

    /// <summary>Un balón lanzado terminó su recorrido.</summary>
    public readonly struct BallSettledEvent : IGameEvent
    {
        public readonly BallSettleReason Reason;
        public readonly Vector3 Position;

        public BallSettledEvent(BallSettleReason reason, Vector3 position)
        {
            Reason = reason;
            Position = position;
        }
    }
}
