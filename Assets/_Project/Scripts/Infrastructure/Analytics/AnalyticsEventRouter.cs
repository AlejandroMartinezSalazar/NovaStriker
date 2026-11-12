using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using NovaStriker.Core.Utils;
using NovaStriker.Infrastructure.Save;
using E = NovaStriker.Infrastructure.Analytics.AnalyticsNames.Events;
using P = NovaStriker.Infrastructure.Analytics.AnalyticsNames.Params;

namespace NovaStriker.Infrastructure.Analytics
{
    /// <summary>
    /// Observer: escucha los eventos del juego y los traduce a eventos de analíticas. El juego no sabe que existe.
    /// Sigue cada intento de nivel (inicio, victoria, derrota, reintento, salida al menú) y registra dónde estaba
    /// el jugador cuando sale de la app (leave_game), que es lo que indica en qué momento abandona.
    /// La derrota se cierra (level_end con success = 0) cuando es definitiva: al reintentar o salir al menú;
    /// si el jugador usa "+2 BALONES" el intento continúa.
    /// </summary>
    public sealed class AnalyticsEventRouter : IDisposable
    {
        public const string SaveKeyName = "analytics.json";

        private readonly IAnalyticsBackend _backend;
        private readonly IGameSettings _settings;
        private readonly IAdsService _ads;
        private readonly SaveSlot<AnalyticsData> _slot;
        private readonly Func<float> _clock;
        private readonly AnalyticsData _data;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

        private AppStateId _app;
        private GameStateId _game;
        private Language _language;
        private bool _adShowing;

        // Intento de nivel en curso.
        private int _levelIndex = -1;
        private int _attempt;
        private float _levelStartTime;
        private int _ballsTotal;
        private int _ballsRemaining;
        private bool _levelActive;
        private bool _startPending;
        private bool _defeatPending;
        private bool _extraBallUsed;
        private bool _offerPending;
        private bool _offerAdReady;
        private bool _tutorialBegun;

        public AnalyticsEventRouter(IAnalyticsBackend backend, IEventBus eventBus, IGameSettings settings, IAdsService ads,
            SaveSlot<AnalyticsData> slot, Func<float> clock)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            if (eventBus == null)
            {
                throw new ArgumentNullException(nameof(eventBus));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            _slot = slot ?? throw new ArgumentNullException(nameof(slot));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));

            _data = _slot.Load();
            _data.Attempts = _data.Attempts ?? Array.Empty<int>();
            _language = _settings.Language;
            _settings.Changed += OnSettingsChanged;

            _subscriptions.Add(eventBus.Subscribe<AppStateChangedEvent>(OnAppStateChanged));
            _subscriptions.Add(eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged));
            _subscriptions.Add(eventBus.Subscribe<LevelStartedEvent>(OnLevelStarted));
            _subscriptions.Add(eventBus.Subscribe<LevelProgressChangedEvent>(OnLevelProgress));
            _subscriptions.Add(eventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted));
            _subscriptions.Add(eventBus.Subscribe<LevelFailedEvent>(_ => OnLevelFailed()));
            _subscriptions.Add(eventBus.Subscribe<TutorialHintEvent>(OnTutorialHint));
            _subscriptions.Add(eventBus.Subscribe<ShotFiredEvent>(_ => OnShotFired()));
            _subscriptions.Add(eventBus.Subscribe<FullscreenAdChangedEvent>(e => _adShowing = e.IsShowing));
            _subscriptions.Add(eventBus.Subscribe<RewardedAdRequestedEvent>(_ => OnRewardedRequested()));
            _subscriptions.Add(eventBus.Subscribe<RewardedAdClosedEvent>(OnRewardedClosed));
            _subscriptions.Add(eventBus.Subscribe<InterstitialShownEvent>(_ => OnInterstitialShown()));
            _subscriptions.Add(eventBus.Subscribe<PlayerLoggedInEvent>(e => _backend.SetUserId(e.PlayerId)));

            if (_data.HighestLevel > 0)
            {
                _backend.SetUserProperty(AnalyticsNames.UserProperties.HighestLevel, _data.HighestLevel.ToString());
            }
        }

        /// <summary>La app pasa a segundo plano (el jugador sale o la cierra). Lo llama la raíz de composición.</summary>
        public void OnAppPaused()
        {
            // Un anuncio a pantalla completa también pausa la app en Android: eso no es que el jugador se vaya.
            if (_adShowing)
            {
                return;
            }

            FlushPendingStart();
            if (_offerPending)
            {
                LogOffer(AnalyticsNames.OfferResults.Left);
            }

            bool inLevel = _app == AppStateId.Gameplay && _levelIndex >= 0;
            if (inLevel)
            {
                Log(E.LeaveGame,
                    AnalyticsParameter.Of(P.Context, Context),
                    AnalyticsParameter.Of(P.Level, LevelNumber),
                    AnalyticsParameter.Of(P.Attempt, _attempt),
                    AnalyticsParameter.Of(P.Seconds, SecondsInLevel));
            }
            else
            {
                Log(E.LeaveGame, AnalyticsParameter.Of(P.Context, Context));
            }
        }

        public void Dispose()
        {
            _settings.Changed -= OnSettingsChanged;
            _subscriptions.Dispose();
        }

        private int LevelNumber => _levelIndex + 1;
        private long SecondsInLevel => (long)Math.Max(0f, Math.Round(_clock() - _levelStartTime));
        private string Context => AnalyticsContext.Describe(_app, _game, _settings.Language != Language.None);

        // ---------- App ----------

        private void OnAppStateChanged(AppStateChangedEvent e)
        {
            if (e.From == AppStateId.Gameplay)
            {
                FlushPendingStart();
                if (_defeatPending)
                {
                    CloseDefeat();
                }
                else if (_levelActive)
                {
                    Log(E.LevelQuit,
                        AnalyticsParameter.Of(P.Level, LevelNumber),
                        AnalyticsParameter.Of(P.Attempt, _attempt),
                        AnalyticsParameter.Of(P.Seconds, SecondsInLevel),
                        AnalyticsParameter.Of(P.BallsLeft, _ballsRemaining));
                }

                _levelActive = false;
                _levelIndex = -1;
                _game = GameStateId.None;
            }

            _app = e.To;
        }

        private void OnSettingsChanged()
        {
            Language current = _settings.Language;
            if (current == _language || current == Language.None)
            {
                return;
            }

            bool firstTime = _language == Language.None;
            _language = current;
            Log(E.LanguageSelected,
                AnalyticsParameter.Of(P.Language, LanguageCode(current)),
                AnalyticsParameter.Of(P.FirstTime, firstTime));
        }

        // ---------- Niveles ----------

        private void OnLevelStarted(LevelStartedEvent e)
        {
            FlushPendingStart();
            bool sameLevel = e.LevelIndex == _levelIndex;
            string retryFrom = null;
            if (_defeatPending)
            {
                CloseDefeat();
                retryFrom = sameLevel ? AnalyticsNames.RetryFrom.Defeat : null;
            }
            else if (_levelActive && sameLevel)
            {
                retryFrom = AnalyticsNames.RetryFrom.Pause;
            }

            if (retryFrom != null)
            {
                Log(E.LevelRetry,
                    AnalyticsParameter.Of(P.Level, LevelNumber),
                    AnalyticsParameter.Of(P.Attempt, _attempt),
                    AnalyticsParameter.Of(P.From, retryFrom));
            }

            _levelIndex = e.LevelIndex;
            _attempt = NextAttempt(e.LevelIndex);
            _levelStartTime = _clock();
            _ballsTotal = 0;
            _ballsRemaining = 0;
            _levelActive = true;
            _startPending = true;
            _defeatPending = false;
            _extraBallUsed = false;
            _offerPending = false;
            _tutorialBegun = false;

            if (LevelNumber > _data.HighestLevel)
            {
                _data.HighestLevel = LevelNumber;
                _backend.SetUserProperty(AnalyticsNames.UserProperties.HighestLevel, LevelNumber.ToString());
            }

            _slot.Save(_data);
        }

        // Los balones del nivel llegan justo después de LevelStarted: level_start se envía con ellos.
        private void OnLevelProgress(LevelProgressChangedEvent e)
        {
            _ballsTotal = e.BallsTotal;
            _ballsRemaining = e.BallsRemaining;
            FlushPendingStart();
        }

        private void FlushPendingStart()
        {
            if (!_startPending)
            {
                return;
            }

            _startPending = false;
            Log(E.LevelStart,
                AnalyticsParameter.Of(P.Level, LevelNumber),
                AnalyticsParameter.Of(P.LevelName, LevelName),
                AnalyticsParameter.Of(P.Attempt, _attempt),
                AnalyticsParameter.Of(P.Balls, _ballsTotal));
        }

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            _game = e.To;
            if (e.To == GameStateId.Defeat && _defeatPending && !_extraBallUsed)
            {
                _offerPending = true;
                _offerAdReady = _ads.IsRewardedReady;
            }
            else if (e.To == GameStateId.Aiming && _defeatPending)
            {
                // Continuó con "+2 BALONES": el intento sigue.
                _defeatPending = false;
            }
        }

        private void OnLevelCompleted(LevelCompletedEvent e)
        {
            FlushPendingStart();
            _ballsRemaining = e.BallsRemaining;
            EndLevel(success: true);
        }

        private void OnLevelFailed()
        {
            FlushPendingStart();
            _defeatPending = true;
        }

        private void CloseDefeat()
        {
            if (_offerPending)
            {
                LogOffer(AnalyticsNames.OfferResults.Declined);
            }

            EndLevel(success: false);
        }

        private void EndLevel(bool success)
        {
            Log(E.LevelEnd,
                AnalyticsParameter.Of(P.Level, LevelNumber),
                AnalyticsParameter.Of(P.LevelName, LevelName),
                AnalyticsParameter.Of(P.Success, success),
                AnalyticsParameter.Of(P.Attempt, _attempt),
                AnalyticsParameter.Of(P.Seconds, SecondsInLevel),
                AnalyticsParameter.Of(P.BallsUsed, Math.Max(0, _ballsTotal - _ballsRemaining)),
                AnalyticsParameter.Of(P.BallsLeft, _ballsRemaining));

            _levelActive = false;
            _defeatPending = false;
            _offerPending = false;
        }

        // ---------- Tutorial ----------

        private void OnTutorialHint(TutorialHintEvent e)
        {
            if (!e.Visible || _data.TutorialDone || _tutorialBegun)
            {
                return;
            }

            _tutorialBegun = true;
            Log(E.TutorialBegin);
        }

        private void OnShotFired()
        {
            if (!_tutorialBegun || _data.TutorialDone)
            {
                return;
            }

            _data.TutorialDone = true;
            _slot.Save(_data);
            Log(E.TutorialComplete);
        }

        // ---------- Anuncios ----------

        private void OnRewardedRequested()
        {
            if (_offerPending)
            {
                LogOffer(AnalyticsNames.OfferResults.Accepted);
            }
        }

        private void OnRewardedClosed(RewardedAdClosedEvent e)
        {
            if (e.Rewarded)
            {
                _extraBallUsed = true;
                Log(E.RewardedComplete, AnalyticsParameter.Of(P.Level, LevelNumber), AnalyticsParameter.Of(P.Attempt, _attempt));
            }
            else if (_defeatPending && !_extraBallUsed)
            {
                // Lo cerró antes de tiempo: el panel de derrota sigue ofreciendo los balones.
                _offerPending = true;
                _offerAdReady = _ads.IsRewardedReady;
            }
        }

        private void OnInterstitialShown()
        {
            Log(E.InterstitialShown, AnalyticsParameter.Of(P.Level, _levelIndex >= 0 ? LevelNumber : 0));
        }

        private void LogOffer(string result)
        {
            _offerPending = false;
            Log(E.RewardedOffer,
                AnalyticsParameter.Of(P.Level, LevelNumber),
                AnalyticsParameter.Of(P.Attempt, _attempt),
                AnalyticsParameter.Of(P.Result, result),
                AnalyticsParameter.Of(P.AdReady, _offerAdReady));
        }

        // ---------- Utilidades ----------

        private string LevelName => $"level_{LevelNumber:00}";

        private int NextAttempt(int levelIndex)
        {
            if (levelIndex < 0)
            {
                return 1;
            }

            if (_data.Attempts.Length <= levelIndex)
            {
                int[] grown = new int[levelIndex + 1];
                Array.Copy(_data.Attempts, grown, _data.Attempts.Length);
                _data.Attempts = grown;
            }

            return ++_data.Attempts[levelIndex];
        }

        internal static string LanguageCode(Language language)
        {
            switch (language)
            {
                case Language.Spanish:
                    return "es";
                case Language.English:
                    return "en";
                case Language.Portuguese:
                    return "pt";
                default:
                    return "none";
            }
        }

        private void Log(string name, params AnalyticsParameter[] parameters)
        {
            try
            {
                _backend.LogEvent(name, parameters);
            }
            catch (Exception exception)
            {
                // Las analíticas nunca deben romper el juego.
                UnityEngine.Debug.LogWarning($"[NovaStriker] Analíticas: no se pudo registrar '{name}': {exception.Message}");
            }
        }
    }
}
