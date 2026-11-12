using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using NovaStriker.Core.Utils;

namespace NovaStriker.UI.Results
{
    /// <summary>
    /// Decide qué muestra el panel según el estado de la partida y qué orden da cada botón.
    /// Pausa: Continuar / Reiniciar. Derrota: Reintentar / "+2 BALONES" (anuncio recompensado, una vez por intento;
    /// desactivado si el anuncio aún no está cargado y oculto si ya se usó).
    /// Próximamente (solo si se entra a jugar sin niveles): Empezar desde el nivel 1.
    /// El botón Menú siempre vuelve al menú principal. La victoria tiene su propia pantalla (VictoryPresenter).
    /// </summary>
    public sealed class ResultPanelPresenter : IDisposable
    {
        private readonly IResultPanelView _view;
        private readonly IGameCommands _commands;
        private readonly IAdsService _ads;
        private readonly ILocalization _localization;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

        private int _levelNumber;
        private int _targetsRemaining;
        private DefeatReason _defeatReason;
        private Action _primaryAction;
        private Action _secondaryAction;

        public ResultPanelPresenter(IResultPanelView view, IEventBus eventBus, IGameCommands commands, IAdsService ads,
            ILocalization localization)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            if (eventBus == null)
            {
                throw new ArgumentNullException(nameof(eventBus));
            }

            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));

            _view.PrimaryClicked += OnPrimaryClicked;
            _view.SecondaryClicked += OnSecondaryClicked;
            _view.MenuClicked += OnMenuClicked;
            _subscriptions.Add(eventBus.Subscribe<LevelStartedEvent>(e => _levelNumber = e.LevelNumber));
            _subscriptions.Add(eventBus.Subscribe<LevelProgressChangedEvent>(e => _targetsRemaining = e.TargetsRemaining));
            _subscriptions.Add(eventBus.Subscribe<LevelFailedEvent>(e => _defeatReason = e.Reason));
            _subscriptions.Add(eventBus.Subscribe<GameStateChangedEvent>(OnStateChanged));

            Hide();
        }

        public void Dispose()
        {
            _view.PrimaryClicked -= OnPrimaryClicked;
            _view.SecondaryClicked -= OnSecondaryClicked;
            _view.MenuClicked -= OnMenuClicked;
            _subscriptions.Dispose();
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            switch (e.To)
            {
                case GameStateId.Paused:
                    Show(new ResultPanelContent(T(TextKey.PauseTitle), _localization.Format(TextKey.PauseMessage, _levelNumber),
                            T(TextKey.PauseContinue), T(TextKey.PauseRestart)),
                        _commands.Resume, _commands.RestartLevel);
                    break;

                case GameStateId.Defeat:
                    // D5: "+2 BALONES" con anuncio recompensado; una vez por intento y solo si hay anuncio cargado.
                    bool canContinue = _commands.CanContinueWithExtraBall;
                    Show(new ResultPanelContent(DefeatTitle(_defeatReason),
                            _localization.Format(TextKey.DefeatRemaining, _targetsRemaining),
                            T(TextKey.DefeatRetry), canContinue ? T(TextKey.DefeatExtraBall) : null,
                            secondaryEnabled: canContinue && _ads.IsRewardedReady, ResultPanelStyle.Negative),
                        _commands.RestartLevel, WatchAdForExtraBall);
                    break;

                case GameStateId.ComingSoon:
                    Show(new ResultPanelContent(T(TextKey.ComingSoonTitle), T(TextKey.ComingSoonOnTheWay), T(TextKey.ComingSoonRestart)),
                        _commands.RestartCampaign, null);
                    break;

                default:
                    Hide();
                    break;
            }
        }

        private void WatchAdForExtraBall()
        {
            if (!_commands.CanContinueWithExtraBall || !_ads.IsRewardedReady)
            {
                return;
            }

            _ads.ShowRewarded(rewarded =>
            {
                if (rewarded)
                {
                    _commands.ContinueWithExtraBall();
                }
            });
        }

        private string DefeatTitle(DefeatReason reason)
        {
            switch (reason)
            {
                case DefeatReason.OutOfBalls:
                    return T(TextKey.DefeatOutOfBalls);
                default:
                    return T(TextKey.DefeatFailed);
            }
        }

        private string T(string key) => _localization.Get(key);

        private void Show(in ResultPanelContent content, Action primary, Action secondary)
        {
            _primaryAction = primary;
            _secondaryAction = secondary;
            _view.Show(content);
        }

        private void Hide()
        {
            _primaryAction = null;
            _secondaryAction = null;
            _view.Hide();
        }

        private void OnPrimaryClicked() => _primaryAction?.Invoke();

        private void OnSecondaryClicked() => _secondaryAction?.Invoke();

        private void OnMenuClicked() => _commands.GoToMainMenu();
    }
}
