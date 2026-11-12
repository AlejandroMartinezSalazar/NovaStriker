using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    /// <summary>Resultado de una estrategia: lo que el balón necesita para llegar al objetivo.</summary>
    public readonly struct ThrowCommand
    {
        /// <summary>Velocidad inicial (ya compensada para la integración discreta de la física).</summary>
        public readonly Vector3 InitialVelocity;

        /// <summary>Aceleración constante adicional a la gravedad (curva). Cero en tiros sin curva.</summary>
        public readonly Vector3 ExtraAcceleration;

        /// <summary>Tiempo hasta pasar por el objetivo, múltiplo exacto del paso fijo de la física.</summary>
        public readonly float FlightTime;

        /// <summary>Giro visual del balón (rad/s). No afecta a la trayectoria.</summary>
        public readonly Vector3 VisualSpin;

        public ThrowCommand(Vector3 initialVelocity, Vector3 extraAcceleration, float flightTime, Vector3 visualSpin)
        {
            InitialVelocity = initialVelocity;
            ExtraAcceleration = extraAcceleration;
            FlightTime = flightTime;
            VisualSpin = visualSpin;
        }
    }
}
