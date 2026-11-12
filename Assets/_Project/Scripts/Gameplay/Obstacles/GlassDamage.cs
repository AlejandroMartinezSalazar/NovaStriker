using NovaStriker.Gameplay.Characters;
using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>Daño progresivo de un vidrio rompible. C# puro.</summary>
    public sealed class GlassDamage
    {
        private readonly float _minBallSpeed;
        private readonly float _minImpactSpeed;

        /// <param name="hitsToBreak">Golpes válidos necesarios para romperlo (mínimo 1).</param>
        /// <param name="minBallSpeed">Velocidad relativa mínima (m/s) del balón para que cuente.</param>
        /// <param name="minImpactSpeed">Velocidad relativa mínima (m/s) de cajas o futbolistas para que cuente.</param>
        public GlassDamage(int hitsToBreak, float minBallSpeed, float minImpactSpeed)
        {
            HitsToBreak = Mathf.Max(1, hitsToBreak);
            _minBallSpeed = minBallSpeed;
            _minImpactSpeed = minImpactSpeed;
        }

        public int HitsToBreak { get; }
        public int Hits { get; private set; }
        public bool IsBroken => Hits >= HitsToBreak;

        /// <summary>Daño de 0 (intacto) a 1 (roto), para la parte visual.</summary>
        public float Damage01 => (float)Hits / HitsToBreak;

        public GlassHitResult RegisterHit(CollisionKind kind, float relativeSpeed)
        {
            if (IsBroken)
            {
                return GlassHitResult.Ignored;
            }

            float threshold;
            switch (kind)
            {
                case CollisionKind.Ball:
                    threshold = _minBallSpeed;
                    break;
                case CollisionKind.Impact:
                    threshold = _minImpactSpeed;
                    break;
                default:
                    return GlassHitResult.Ignored;
            }

            if (relativeSpeed < threshold)
            {
                return GlassHitResult.Ignored;
            }

            Hits++;
            return IsBroken ? GlassHitResult.Broken : GlassHitResult.Cracked;
        }
    }
}
