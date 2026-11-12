using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.StateMachine;
using NovaStriker.Gameplay.Level;
using NovaStriker.Gameplay.Level.Rules;

namespace NovaStriker.Gameplay.States
{
    /// <summary>
    /// Traduce las órdenes de la UI a transiciones de la partida. Cada orden comprueba el estado actual
    /// y se ignora si no corresponde (los botones no tienen que saber en qué estado está el juego).
    /// </summary>
    public sealed class GameCommands : IGameCommands
    {
        private readonly IStateMachine<GameStateId> _machine;
        private readonly ILevelSession _session;
        private readonly IProgressService _progress;
        private readonly LevelRulesTracker _rules;
        private readonly IAppNavigator _navigator;

        public GameCommands(
            IStateMachine<GameStateId> machine,
            ILevelSession session,
            IProgressService progress,
            LevelRulesTracker rules,
            IAppNavigator navigator)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        private GameStateId Current => _machine.CurrentId;

        public void Pause()
        {
            if (IsPlaying(Current))
            {
                _machine.Push(GameStateId.Paused);
            }
        }

        public void Resume()
        {
            if (Current == GameStateId.Paused)
            {
                _machine.Pop();
            }
        }

        public void RestartLevel()
        {
            if ((IsPlaying(Current) || Current == GameStateId.Paused || Current == GameStateId.Defeat)
                && _session.CurrentLevelIndex >= 0)
            {
                _machine.ChangeState(GameStateId.LevelSetup, _session.CurrentLevelIndex);
            }
        }

        public void RestartCampaign()
        {
            if (Current == GameStateId.ComingSoon)
            {
                _progress.RestartCampaign();
                _machine.ChangeState(GameStateId.LevelSetup, _progress.CurrentLevelIndex);
            }
        }

        public bool CanContinueWithExtraBall => Current == GameStateId.Defeat && !_rules.ExtraBallUsed;

        public void ContinueWithExtraBall()
        {
            if (Current == GameStateId.Defeat && _rules.TryGrantExtraBall())
            {
                _machine.ChangeState(GameStateId.Aiming);
            }
        }

        public void GoToMainMenu()
        {
            // Quitar la pausa antes de salir para que la escena siguiente no herede el tiempo congelado.
            if (Current == GameStateId.Paused)
            {
                _machine.Pop();
            }

            _navigator.GoToMainMenu();
        }

        private static bool IsPlaying(GameStateId state) =>
            state == GameStateId.Aiming || state == GameStateId.BallInFlight || state == GameStateId.ShotResolve;
    }
}
