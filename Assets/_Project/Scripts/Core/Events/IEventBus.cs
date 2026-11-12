using System;

namespace NovaStriker.Core.Events
{
    /// <summary>
    /// Bus de eventos (Observer) para comunicar sistemas que no se conocen entre sí.
    /// Es una instancia inyectada, nunca estática.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>Suscribe un handler. Liberar el IDisposable devuelto cancela la suscripción.</summary>
        IDisposable Subscribe<T>(Action<T> handler) where T : struct, IGameEvent;

        /// <summary>
        /// Publica un evento a todos los suscriptores de su tipo.
        /// Una excepción en un handler se reporta y no impide que reciban el evento los demás.
        /// </summary>
        void Publish<T>(T gameEvent) where T : struct, IGameEvent;
    }
}
