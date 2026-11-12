using NovaStriker.Core.Events;
using UnityEngine;

namespace NovaStriker.Contracts.Events
{
    /// <summary>El balón entró por un portal y salió por el otro (misma velocidad y dirección).</summary>
    public readonly struct BallTeleportedEvent : IGameEvent
    {
        public readonly Vector3 From;
        public readonly Vector3 To;

        public BallTeleportedEvent(Vector3 from, Vector3 to)
        {
            From = from;
            To = to;
        }
    }
}
