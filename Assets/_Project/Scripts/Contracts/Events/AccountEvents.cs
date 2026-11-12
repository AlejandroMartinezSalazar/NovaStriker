using NovaStriker.Core.Events;

namespace NovaStriker.Contracts.Events
{
    /// <summary>El jugador inició sesión en el backend. <see cref="IsNewPlayer"/>: la cuenta se acaba de crear (primera vez).</summary>
    public readonly struct PlayerLoggedInEvent : IGameEvent
    {
        public readonly string PlayerId;
        public readonly bool IsNewPlayer;

        public PlayerLoggedInEvent(string playerId, bool isNewPlayer)
        {
            PlayerId = playerId;
            IsNewPlayer = isNewPlayer;
        }
    }
}
