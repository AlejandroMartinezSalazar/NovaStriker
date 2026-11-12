using System.Collections.Generic;
using NovaStriker.Contracts.Data;

namespace NovaStriker.Infrastructure.Audio
{
    /// <summary>Limita la frecuencia de cada efecto (C# puro): en una caída en cadena no suenan 20 golpes a la vez.</summary>
    public sealed class SfxThrottle
    {
        private readonly Dictionary<SfxId, float> _lastPlayed = new Dictionary<SfxId, float>();

        public bool TryAcquire(SfxId id, float now, float minInterval)
        {
            if (_lastPlayed.TryGetValue(id, out float last) && now - last < minInterval)
            {
                return false;
            }

            _lastPlayed[id] = now;
            return true;
        }
    }
}
