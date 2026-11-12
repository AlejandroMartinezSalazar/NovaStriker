using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using NovaStriker.Core.Loop;
using NovaStriker.Core.StateMachine;
using NovaStriker.Gameplay.Aim;
using NovaStriker.Gameplay.Level;
using NovaStriker.Gameplay.Level.Rules;
using NovaStriker.Gameplay.Throwing;

namespace NovaStriker.Gameplay.States
{
    /// <summary>
    /// Máquina de estados de la partida (State):
    /// LevelSetup → Aiming (se dispara varias veces; el balón siguiente aparece casi al instante)
    /// → BallInFlight (último balón en juego) → ShotResolve → (Victory | Defeat | Aiming); sin niveles → ComingSoon.
    /// Si caen todos los objetivos en cualquier momento: → ShotResolve → Victory → (a los 3 s) menú principal.
    /// Paused se apila encima. Publica GameStateChangedEvent en cada transición.
    /// </summary>
    public sealed class GameplayFlow : ITickable, IDisposable
    {
        private readonly StateMachine<GameStateId> _machine = new StateMachine<GameStateId>();
        private readonly IEventBus _eventBus;
        private bool _isDisposed;

        public GameplayFlow(
            IEventBus eventBus,
            ILevelSession session,
            LevelRulesTracker rules,
            IThrowController throwController,
            IAimController aim,
            IProgressService progress,
            IAppNavigator navigator,
            IAdsService ads,
            ITimeScale timeScale,
            LevelOutcomeEvaluator evaluator,
            GameFlowSettings settings)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _machine.Register(GameStateId.LevelSetup, new LevelSetupState(_machine, session));
            _machine.Register(GameStateId.Aiming, new AimingState(_machine, throwController, aim, rules, eventBus, settings.ReloadDelay));
            _machine.Register(GameStateId.BallInFlight, new BallInFlightState(_machine, rules, settings.MaxBallFlightTime));
            _machine.Register(GameStateId.ShotResolve, new ShotResolveState(_machine, rules, evaluator,
                new ShotResolveTimer(settings.MinResolveTime, settings.QuietTime, settings.MaxResolveTime)));
            _machine.Register(GameStateId.Victory, new VictoryState(session, progress, rules, eventBus, navigator, ads, settings.VictoryScreenTime));
            _machine.Register(GameStateId.Defeat, new DefeatState(session, eventBus));
            _machine.Register(GameStateId.ComingSoon, new ComingSoonState(session, eventBus));
            _machine.Register(GameStateId.Paused, new PausedState(timeScale));

            _machine.StateChanged += OnStateChanged;
        }

        public IStateMachine<GameStateId> Machine => _machine;
        public GameStateId CurrentState => _machine.CurrentId;

        /// <summary>Arranca la partida en el nivel indicado (normalmente el del progreso).</summary>
        public void Start(int levelIndex)
        {
            _machine.ChangeState(GameStateId.LevelSetup, levelIndex);
        }

        public void Tick(float deltaTime)
        {
            _machine.Tick(deltaTime);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _machine.Stop();
            _machine.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameStateId from, GameStateId to)
        {
            _eventBus.Publish(new GameStateChangedEvent(from, to));
        }
    }
}
