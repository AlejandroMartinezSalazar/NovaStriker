using System;

namespace NovaStriker.Gameplay.Level.Rules
{
    /// <summary>
    /// Decide cuándo un tiro está resuelto: pasada la espera mínima, cuando lleva QuietTime sin derribos
    /// (las caídas en cadena reinician esa cuenta), o al llegar a la espera máxima.
    /// Si ya no quedan objetivos basta la espera mínima (para ver caer al último).
    /// </summary>
    public sealed class ShotResolveTimer
    {
        private readonly float _minTime;
        private readonly float _quietTime;
        private readonly float _maxTime;
        private float _elapsed;
        private float _sinceActivity;

        public ShotResolveTimer(float minTime, float quietTime, float maxTime)
        {
            if (minTime < 0f || quietTime < 0f || maxTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minTime), "Los tiempos no pueden ser negativos.");
            }

            _minTime = minTime;
            _quietTime = quietTime;
            _maxTime = Math.Max(maxTime, minTime);
        }

        public float Elapsed => _elapsed;

        public void Begin()
        {
            _elapsed = 0f;
            _sinceActivity = 0f;
        }

        /// <summary>Hubo un derribo: la escena todavía no está calmada.</summary>
        public void NotifyActivity()
        {
            _sinceActivity = 0f;
        }

        /// <summary>Avanza el tiempo y devuelve true cuando el tiro ya se puede resolver.</summary>
        public bool Step(float deltaTime, bool allTargetsDown)
        {
            _elapsed += deltaTime;
            _sinceActivity += deltaTime;

            if (_elapsed >= _maxTime)
            {
                return true;
            }

            if (_elapsed < _minTime)
            {
                return false;
            }

            return allTargetsDown || _sinceActivity >= _quietTime;
        }
    }
}
