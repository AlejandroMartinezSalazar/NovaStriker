using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using NovaStriker.Core.Utils;

namespace NovaStriker.UI.HUD
{
    /// <summary>
    /// Muestra cuántos futbolistas quedan por derribar y cuántos balones quedan (Observer sobre el bus).
    /// El botón de pausa solo se ve mientras se juega.
    /// </summary>
    public sealed class HudPresenter : IDisposable
    {
        private readonly IHudView _view;
        private readonly IGameCommands _commands;
        private readonly ILocalization _localization;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

        public HudPresenter(IHudView view, IEventBus eventBus, IGameCommands commands, ILocalization localization)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            if (eventBus == null)
            {
                throw new ArgumentNullException(nameof(eventBus));
            }

            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));

            _view.PauseClicked += OnPauseClicked;
            _subscriptions.Add(eventBus.Subscribe<LevelStartedEvent>(OnLevelStarted));
            _subscriptions.Add(eventBus.Subscribe<LevelProgressChangedEvent>(OnProgressChanged));
            _subscriptions.Add(eventBus.Subscribe<GameStateChangedEvent>(OnStateChanged));

            _view.SetLevelText(string.Empty);
            _view.SetTargetsText(string.Empty);
            _view.SetBallsText(string.Empty);
            _view.SetPauseVisible(false);
        }

        public string FormatLevel(int levelNumber) => _localization.Format(TextKey.MenuLevel, levelNumber);

        public string FormatTargets(int remaining, int total) => _localization.Format(TextKey.HudTargets, remaining, total);

        public string FormatBalls(int remaining) => _localization.Format(TextKey.HudBalls, remaining);

        public void Dispose()
        {
            _view.PauseClicked -= OnPauseClicked;
            _subscriptions.Dispose();
        }

        private void OnLevelStarted(LevelStartedEvent e) => _view.SetLevelText(FormatLevel(e.LevelNumber));

        private void OnProgressChanged(LevelProgressChangedEvent e)
        {
            _view.SetTargetsText(FormatTargets(e.TargetsRemaining, e.TargetsTotal));
            _view.SetBallsText(FormatBalls(e.BallsRemaining));
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            _view.SetPauseVisible(e.To == GameStateId.Aiming || e.To == GameStateId.BallInFlight || e.To == GameStateId.ShotResolve);
        }

        private void OnPauseClicked() => _commands.Pause();
    }
}
