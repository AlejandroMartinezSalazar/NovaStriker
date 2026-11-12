using System;

namespace NovaStriker.Gameplay.Level
{
    /// <summary>
    /// Cuenta el tiempo de asentamiento de un nivel recién construido y avisa UNA sola vez cuando termina.
    /// Lógica pura (sin Unity) para poder probarla.
    /// </summary>
    public sealed class SettleCountdown
    {
        private readonly float _duration;
        private float _elapsed;

        public SettleCountdown(float duration)
        {
            if (duration < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), "La duración no puede ser negativa.");
            }

            _duration = duration;
        }

        public bool IsFinished { get; private set; }

        /// <summary>Avanza el tiempo. Devuelve true solo en el paso en que se cumple la duración.</summary>
        public bool Step(float deltaTime)
        {
            if (IsFinished)
            {
                return false;
            }

            _elapsed += Math.Max(0f, deltaTime);
            if (_elapsed < _duration)
            {
                return false;
            }

            IsFinished = true;
            return true;
        }
    }
}
