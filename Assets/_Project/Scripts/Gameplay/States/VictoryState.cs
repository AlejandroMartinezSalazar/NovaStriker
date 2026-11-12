using System;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using NovaStriker.Core.StateMachine;
using NovaStriker.Gameplay.Level;
using NovaStriker.Gameplay.Level.Rules;

namespace NovaStriker.Gameplay.States
{
    /// <summary>
    /// Nivel superado: avanza el progreso, lo anuncia (la UI muestra la animación de victoria) y,
    /// pasado DisplayTime, vuelve solo al menú principal (antes, el intersticial si toca: D6).
    /// </summary>
    public sealed class VictoryState : IState, ITickableState
    {
        private readonly ILevelSession _session;
        private readonly IProgressService _progress;
        private readonly LevelRulesTracker _rules;
        private readonly IEventBus _eventBus;
        private readonly IAppNavigator _navigator;
        private readonly IAdsService _ads;
        private readonly float _displayTime;

        private float _elapsed;
        private bool _hasLeft;

        public VictoryState(
            ILevelSession session,
            IProgressService progress,
            LevelRulesTracker rules,
            IEventBus eventBus,
            IAppNavigator navigator,
            IAdsService ads,
            float displayTime)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            _displayTime = displayTime;
        }

        public void Enter()
        {
            _elapsed = 0f;
            _hasLeft = false;
            int levelIndex = _session.CurrentLevelIndex;
            _progress.CompleteLevel(levelIndex);
            bool isLastLevel = levelIndex >= _session.LevelCount - 1;
            _eventBus.Publish(new LevelCompletedEvent(levelIndex, isLastLevel, _rules.Status.BallsRemaining));
        }

        public void Tick(float deltaTime)
        {
            if (_hasLeft)
            {
                return;
            }

            _elapsed += deltaTime;
            if (_elapsed >= _displayTime)
            {
                _hasLeft = true;
                // D6: el intersticial (si toca según las victorias totales) sale antes de volver al menú.
                _ads.ShowInterstitialIfDue(_progress.TotalVictories, _navigator.GoToMainMenu);
            }
        }

        public void Exit()
        {
        }
    }
}
