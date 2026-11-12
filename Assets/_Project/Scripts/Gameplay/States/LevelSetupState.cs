using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Core.StateMachine;
using NovaStriker.Gameplay.Level;

namespace NovaStriker.Gameplay.States
{
    /// <summary>Construye el nivel pedido y pasa a apuntar. Si no quedan niveles, va a "Próximamente" (D1).</summary>
    public sealed class LevelSetupState : IPayloadState<int>
    {
        private readonly IStateMachine<GameStateId> _machine;
        private readonly ILevelSession _session;

        public LevelSetupState(IStateMachine<GameStateId> machine, ILevelSession session)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public void Enter(int levelIndex)
        {
            if (!_session.HasLevel(levelIndex))
            {
                _session.Unload();
                _machine.ChangeState(GameStateId.ComingSoon);
                return;
            }

            _session.Load(levelIndex);
            _machine.ChangeState(GameStateId.Aiming);
        }

        public void Exit()
        {
        }
    }
}
