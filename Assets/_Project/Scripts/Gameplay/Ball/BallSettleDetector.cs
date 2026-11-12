using NovaStriker.Contracts.Data;

namespace NovaStriker.Gameplay.Ball
{
    /// <summary>
    /// Decide cuándo un balón lanzado terminó su recorrido. C# puro: el balón le pasa su velocidad cada paso de física.
    /// </summary>
    public sealed class BallSettleDetector
    {
        private readonly float _settleSpeed;
        private readonly float _settleDuration;
        private readonly float _maxLifetime;

        private float _lifetime;
        private float _slowTime;
        private bool _isActive;

        /// <param name="settleSpeed">Por debajo de esta rapidez (m/s) el balón cuenta como quieto.</param>
        /// <param name="settleDuration">Tiempo seguido (s) por debajo de esa rapidez para darlo por detenido.</param>
        /// <param name="maxLifetime">Tiempo máximo (s) desde el lanzamiento.</param>
        public BallSettleDetector(float settleSpeed, float settleDuration, float maxLifetime)
        {
            _settleSpeed = settleSpeed;
            _settleDuration = settleDuration;
            _maxLifetime = maxLifetime;
        }

        public bool IsActive => _isActive;

        public void Begin()
        {
            _lifetime = 0f;
            _slowTime = 0f;
            _isActive = true;
        }

        public void Stop()
        {
            _isActive = false;
        }

        /// <summary>Avanza un paso. Devuelve el motivo de fin, o None si sigue en juego. Tras devolver un motivo se detiene.</summary>
        public BallSettleReason Step(float speed, float deltaTime)
        {
            if (!_isActive)
            {
                return BallSettleReason.None;
            }

            _lifetime += deltaTime;
            _slowTime = speed < _settleSpeed ? _slowTime + deltaTime : 0f;

            BallSettleReason reason = BallSettleReason.None;
            if (_slowTime >= _settleDuration)
            {
                reason = BallSettleReason.Stopped;
            }
            else if (_lifetime >= _maxLifetime)
            {
                reason = BallSettleReason.Timeout;
            }

            if (reason != BallSettleReason.None)
            {
                _isActive = false;
            }

            return reason;
        }
    }
}
