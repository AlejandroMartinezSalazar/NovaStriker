using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Core.StateMachine;
using NovaStriker.Gameplay.Level;

namespace NovaStriker.Gameplay.States
{
    /// <summary>Nivel perdido. Espera RestartLevel (o, desde la Etapa 10, continuar con "+1 balón").</summary>
    public sealed class DefeatState : IPayloadState<DefeatReason>
    {
        private readonly ILevelSession _session;
        private readonly IEventBus _eventBus;

        public DefeatState(ILevelSession session, IEventBus eventBus)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public DefeatReason Reason { get; private set; }

        public void Enter(DefeatReason reason)
        {
            Reason = reason;
            _eventBus.Publish(new LevelFailedEvent(_session.CurrentLevelIndex, reason));
        }

        public void Exit()
        {
            Reason = DefeatReason.None;
        }
    }
}
