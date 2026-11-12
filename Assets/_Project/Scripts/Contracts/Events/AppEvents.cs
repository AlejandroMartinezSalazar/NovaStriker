using NovaStriker.Contracts.Data;
using NovaStriker.Core.Events;

namespace NovaStriker.Contracts.Events
{
    /// <summary>Se publica tras cada transición de la máquina de estados de la aplicación.</summary>
    public readonly struct AppStateChangedEvent : IGameEvent
    {
        public readonly AppStateId From;
        public readonly AppStateId To;

        public AppStateChangedEvent(AppStateId from, AppStateId to)
        {
            From = from;
            To = to;
        }
    }
}
