using NovaStriker.Core.Events;

namespace NovaStriker.Contracts.Events
{
    /// <summary>Empieza o termina un anuncio a pantalla completa (el audio se silencia mientras dura).</summary>
    public readonly struct FullscreenAdChangedEvent : IGameEvent
    {
        public readonly bool IsShowing;

        public FullscreenAdChangedEvent(bool isShowing)
        {
            IsShowing = isShowing;
        }
    }

    /// <summary>El jugador pidió ver un anuncio recompensado (p. ej. "+2 BALONES") y empieza a mostrarse.</summary>
    public readonly struct RewardedAdRequestedEvent : IGameEvent
    {
    }

    /// <summary>Se cerró el anuncio recompensado. <see cref="Rewarded"/>: se vio entero y se da la recompensa.</summary>
    public readonly struct RewardedAdClosedEvent : IGameEvent
    {
        public readonly bool Rewarded;

        public RewardedAdClosedEvent(bool rewarded)
        {
            Rewarded = rewarded;
        }
    }

    /// <summary>Se mostró un intersticial.</summary>
    public readonly struct InterstitialShownEvent : IGameEvent
    {
        public readonly int TotalVictories;

        public InterstitialShownEvent(int totalVictories)
        {
            TotalVictories = totalVictories;
        }
    }
}
