using System;
using System.Collections.Generic;

namespace NovaStriker.Gameplay.Obstacles.Portals
{
    /// <summary>
    /// Límite de viajes por portal en una ventana de tiempo. Evita el bucle infinito de un balón que cae en un portal,
    /// sale por el otro encima del primero y gana velocidad cada vuelta. C# puro.
    /// </summary>
    public sealed class TeleportBudget
    {
        private readonly int _max;
        private readonly float _window;
        private readonly Queue<float> _times = new Queue<float>();

        public TeleportBudget(int max, float window)
        {
            if (max < 1 || window <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(max), "Al menos 1 viaje y una ventana positiva.");
            }

            _max = max;
            _window = window;
        }

        /// <summary>true si cabe otro viaje ahora (y lo cuenta).</summary>
        public bool TryConsume(float now)
        {
            while (_times.Count > 0 && now - _times.Peek() >= _window)
            {
                _times.Dequeue();
            }

            if (_times.Count >= _max)
            {
                return false;
            }

            _times.Enqueue(now);
            return true;
        }
    }
}
