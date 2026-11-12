using NovaStriker.Contracts.Data;
using NovaStriker.Core.Events;
using UnityEngine;

namespace NovaStriker.Contracts.Events
{
    /// <summary>Un vidrio rompible recibió un golpe válido y se agrietó (sin romperse aún).</summary>
    public readonly struct GlassCrackedEvent : IGameEvent
    {
        /// <summary>Golpes recibidos (1 = primera grieta).</summary>
        public readonly int Hits;
        public readonly int HitsToBreak;
        public readonly Vector3 Position;

        public GlassCrackedEvent(int hits, int hitsToBreak, Vector3 position)
        {
            Hits = hits;
            HitsToBreak = hitsToBreak;
            Position = position;
        }
    }

    /// <summary>Una caja dinámica chocó con fuerza contra algo que no es el balón (audio de cajas cayendo).</summary>
    public readonly struct ObstacleImpactEvent : IGameEvent
    {
        public readonly ObstacleType Type;
        public readonly Vector3 Point;
        public readonly float Impulse;

        public ObstacleImpactEvent(ObstacleType type, Vector3 point, float impulse)
        {
            Type = type;
            Point = point;
            Impulse = impulse;
        }
    }

    /// <summary>Un obstáculo se destruyó (hoy: vidrio rompible).</summary>
    public readonly struct ObstacleBrokenEvent : IGameEvent
    {
        public readonly ObstacleType Type;
        public readonly Vector3 Position;

        public ObstacleBrokenEvent(ObstacleType type, Vector3 position)
        {
            Type = type;
            Position = position;
        }
    }
}
