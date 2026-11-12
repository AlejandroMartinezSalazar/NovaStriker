using System;

namespace NovaStriker.Infrastructure.Ads
{
    /// <summary>
    /// Cuándo sale un intersticial tras una victoria (C# puro). Por defecto: cada 4 victorias hasta la 16
    /// (4, 8, 12, 16) y después cada 2 (18, 20, 22...).
    /// </summary>
    public sealed class InterstitialPolicy
    {
        private readonly int _earlyInterval;
        private readonly int _switchAt;
        private readonly int _lateInterval;

        public InterstitialPolicy(int earlyInterval, int switchAt, int lateInterval)
        {
            if (earlyInterval < 1 || switchAt < 1 || lateInterval < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(earlyInterval), "Los intervalos deben ser 1 o más.");
            }

            _earlyInterval = earlyInterval;
            _switchAt = switchAt;
            _lateInterval = lateInterval;
        }

        public static InterstitialPolicy Default => new InterstitialPolicy(4, 16, 2);

        public bool IsDue(int totalVictories)
        {
            if (totalVictories <= 0)
            {
                return false;
            }

            if (totalVictories <= _switchAt)
            {
                return totalVictories % _earlyInterval == 0;
            }

            return (totalVictories - _switchAt) % _lateInterval == 0;
        }
    }
}
