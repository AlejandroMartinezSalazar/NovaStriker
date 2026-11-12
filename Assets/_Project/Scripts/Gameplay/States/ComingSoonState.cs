using System;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Core.StateMachine;
using NovaStriker.Gameplay.Level;

namespace NovaStriker.Gameplay.States
{
    /// <summary>No quedan niveles (D1). Espera RestartCampaign o volver al menú.</summary>
    public sealed class ComingSoonState : IState
    {
        private readonly ILevelSession _session;
        private readonly IEventBus _eventBus;

        public ComingSoonState(ILevelSession session, IEventBus eventBus)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void Enter()
        {
            _eventBus.Publish(new CampaignCompletedEvent(_session.LevelCount));
        }

        public void Exit()
        {
        }
    }
}
