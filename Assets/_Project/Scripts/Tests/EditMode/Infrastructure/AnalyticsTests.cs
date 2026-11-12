using System;
using System.Collections.Generic;
using System.Linq;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using NovaStriker.Infrastructure.Ads;
using NovaStriker.Infrastructure.Analytics;
using NovaStriker.Infrastructure.Save;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Infrastructure
{
    public class AnalyticsTests
    {
        private sealed class RecordingBackend : IAnalyticsBackend
        {
            public readonly List<(string Name, AnalyticsParameter[] Params)> Events = new List<(string, AnalyticsParameter[])>();
            public readonly Dictionary<string, string> UserProperties = new Dictionary<string, string>();
            public string UserId;
            public bool Throw;

            public void Initialize()
            {
            }

            public void LogEvent(string name, IReadOnlyList<AnalyticsParameter> parameters)
            {
                if (Throw)
                {
                    throw new InvalidOperationException("SDK roto");
                }

                Events.Add((name, parameters.ToArray()));
            }

            public void SetUserProperty(string name, string value) => UserProperties[name] = value;
            public void SetUserId(string userId) => UserId = userId;

            public string[] Names => Events.Select(e => e.Name).ToArray();

            public (string Name, AnalyticsParameter[] Params) Last(string name) => Events.Last(e => e.Name == name);
        }

        private sealed class FakeAds : IAdsService
        {
            public bool RewardedReady = true;
            public float BannerHeightPixels => 0f;
            public event Action BannerLayoutChanged { add { } remove { } }
            public bool IsRewardedReady => RewardedReady;
            public void ShowInterstitialIfDue(int totalVictories, Action onFinished) => onFinished();
            public void ShowRewarded(Action<bool> onCompleted) => onCompleted(true);
        }

        private sealed class MemoryStorage : ISaveStorage
        {
            public readonly Dictionary<string, string> Data = new Dictionary<string, string>();
            public bool Exists(string key) => Data.ContainsKey(key);
            public string Read(string key) => Data.TryGetValue(key, out string value) ? value : null;
            public void Write(string key, string data) => Data[key] = data;
            public void Delete(string key) => Data.Remove(key);
        }

        private RecordingBackend _backend;
        private EventBus _bus;
        private MemorySettings _settings;
        private FakeAds _ads;
        private MemoryStorage _storage;
        private float _time;
        private AnalyticsEventRouter _router;

        [SetUp]
        public void SetUp()
        {
            _backend = new RecordingBackend();
            _bus = new EventBus();
            _settings = new MemorySettings();
            _settings.SetLanguage(Language.Spanish);
            _ads = new FakeAds();
            _storage = new MemoryStorage();
            _time = 0f;
            _router = CreateRouter();
        }

        [TearDown]
        public void TearDown()
        {
            _router.Dispose();
            _bus.Dispose();
        }

        private AnalyticsEventRouter CreateRouter() => new AnalyticsEventRouter(_backend, _bus, _settings, _ads,
            new SaveSlot<AnalyticsData>(_storage, new JsonUtilitySerializer(), AnalyticsEventRouter.SaveKeyName, () => new AnalyticsData()),
            () => _time);

        // ---------- Ayudas: simulan lo que publica el juego ----------

        private void EnterGameplay() => _bus.Publish(new AppStateChangedEvent(AppStateId.MainMenu, AppStateId.Gameplay));
        private void ToMenu() => _bus.Publish(new AppStateChangedEvent(AppStateId.Gameplay, AppStateId.MainMenu));
        private void State(GameStateId from, GameStateId to) => _bus.Publish(new GameStateChangedEvent(from, to));

        private void StartLevel(int index, int balls = 3)
        {
            State(GameStateId.None, GameStateId.LevelSetup);
            _bus.Publish(new LevelStartedEvent(index, 12, "Nivel"));
            _bus.Publish(new LevelProgressChangedEvent(1, 1, balls, balls));
            State(GameStateId.LevelSetup, GameStateId.Aiming);
        }

        private void Shoot(int ballsLeft, int ballsTotal = 3)
        {
            _bus.Publish(new ShotFiredEvent("arc", Vector3.zero, Vector3.zero, Vector3.zero));
            _bus.Publish(new LevelProgressChangedEvent(1, 1, ballsLeft, ballsTotal));
        }

        private void Lose(int index)
        {
            _bus.Publish(new LevelFailedEvent(index, DefeatReason.OutOfBalls));
            State(GameStateId.ShotResolve, GameStateId.Defeat);
        }

        private static long Number(AnalyticsParameter[] ps, string name) => ps.Single(p => p.Name == name).Number;
        private static string Text(AnalyticsParameter[] ps, string name) => ps.Single(p => p.Name == name).Text;

        // ---------- Pruebas ----------

        [Test]
        public void Victory_LogsStartAndEnd_WithBallsAndTime()
        {
            EnterGameplay();
            StartLevel(0);
            _time = 12.4f;
            Shoot(2);
            _bus.Publish(new LevelCompletedEvent(0, false, 2));

            CollectionAssert.AreEqual(new[] { "level_start", "level_end" }, _backend.Names);
            var start = _backend.Last("level_start").Params;
            Assert.AreEqual(1, Number(start, "level"));
            Assert.AreEqual("level_01", Text(start, "level_name"));
            Assert.AreEqual(1, Number(start, "attempt"));
            Assert.AreEqual(3, Number(start, "balls"));

            var end = _backend.Last("level_end").Params;
            Assert.AreEqual(1, Number(end, "success"));
            Assert.AreEqual(12, Number(end, "seconds"));
            Assert.AreEqual(1, Number(end, "balls_used"));
            Assert.AreEqual(2, Number(end, "balls_left"));
            Assert.AreEqual("1", _backend.UserProperties["highest_level"]);
        }

        [Test]
        public void DefeatThenRetry_ClosesDefeat_DeclinesOffer_AndCountsAttempts()
        {
            EnterGameplay();
            StartLevel(4);
            Shoot(0);
            Lose(4);
            Assert.IsFalse(_backend.Names.Contains("level_end"), "La derrota aún no es definitiva (puede usar +2 balones).");

            StartLevel(4);

            CollectionAssert.AreEqual(
                new[] { "level_start", "rewarded_offer", "level_end", "level_retry", "level_start" }, _backend.Names);
            Assert.AreEqual("declined", Text(_backend.Last("rewarded_offer").Params, "result"));
            Assert.AreEqual(0, Number(_backend.Last("level_end").Params, "success"));
            Assert.AreEqual("defeat", Text(_backend.Last("level_retry").Params, "from"));
            Assert.AreEqual(2, Number(_backend.Last("level_start").Params, "attempt"));
        }

        [Test]
        public void ExtraBall_Accepted_AttemptContinues_AndEndsOnceAsVictory()
        {
            EnterGameplay();
            StartLevel(2);
            Shoot(0);
            Lose(2);

            _bus.Publish(new RewardedAdRequestedEvent());
            _bus.Publish(new RewardedAdClosedEvent(true));
            State(GameStateId.Defeat, GameStateId.Aiming);
            Shoot(1, 5);
            _bus.Publish(new LevelCompletedEvent(2, false, 1));

            CollectionAssert.AreEqual(new[] { "level_start", "rewarded_offer", "rewarded_complete", "level_end" }, _backend.Names);
            Assert.AreEqual("accepted", Text(_backend.Last("rewarded_offer").Params, "result"));
            Assert.AreEqual(1, Number(_backend.Last("level_end").Params, "success"));
        }

        [Test]
        public void QuitFromPause_LogsLevelQuit_ButDefeatThenMenu_LogsLevelEnd()
        {
            EnterGameplay();
            StartLevel(1);
            State(GameStateId.Aiming, GameStateId.Paused);
            _time = 30f;
            ToMenu();
            Assert.AreEqual("level_quit", _backend.Names.Last());
            Assert.AreEqual(30, Number(_backend.Last("level_quit").Params, "seconds"));

            EnterGameplay();
            StartLevel(1);
            Shoot(0);
            Lose(1);
            ToMenu();
            Assert.AreEqual("level_end", _backend.Names.Last());
            Assert.AreEqual(1, _backend.Names.Count(n => n == "level_quit"));
        }

        [Test]
        public void RestartFromPause_IsRetryFromPause()
        {
            EnterGameplay();
            StartLevel(3);
            State(GameStateId.Aiming, GameStateId.Paused);
            StartLevel(3);

            Assert.AreEqual("pause", Text(_backend.Last("level_retry").Params, "from"));
        }

        [Test]
        public void LeaveGame_ReportsWhereThePlayerWas()
        {
            EnterGameplay();
            StartLevel(8);
            Shoot(0);
            Lose(8);
            _time = 7f;
            _router.OnAppPaused();

            var leave = _backend.Last("leave_game").Params;
            Assert.AreEqual("defeat", Text(leave, "context"));
            Assert.AreEqual(9, Number(leave, "level"));
            Assert.AreEqual(7, Number(leave, "seconds"));
            Assert.AreEqual("left", Text(_backend.Last("rewarded_offer").Params, "result"));
        }

        [Test]
        public void LeaveGame_DuringFullscreenAd_IsNotLogged()
        {
            EnterGameplay();
            StartLevel(0);
            _bus.Publish(new FullscreenAdChangedEvent(true));
            _router.OnAppPaused();

            Assert.IsFalse(_backend.Names.Contains("leave_game"));
        }

        [Test]
        public void LeaveGame_OnLanguagePicker()
        {
            _router.Dispose();
            _settings = new MemorySettings();
            _router = CreateRouter();
            _bus.Publish(new AppStateChangedEvent(AppStateId.Boot, AppStateId.MainMenu));

            _router.OnAppPaused();

            Assert.AreEqual("language_picker", Text(_backend.Last("leave_game").Params, "context"));
        }

        [Test]
        public void LanguageSelected_FirstTimeAndLaterChange()
        {
            _router.Dispose();
            _settings = new MemorySettings();
            _router = CreateRouter();

            _settings.SetLanguage(Language.Portuguese);
            _settings.SetLanguage(Language.English);

            var events = _backend.Events.Where(e => e.Name == "language_selected").ToArray();
            Assert.AreEqual(2, events.Length);
            Assert.AreEqual("pt", Text(events[0].Params, "language"));
            Assert.AreEqual(1, Number(events[0].Params, "first_time"));
            Assert.AreEqual("en", Text(events[1].Params, "language"));
            Assert.AreEqual(0, Number(events[1].Params, "first_time"));
        }

        [Test]
        public void Tutorial_BeginAndComplete_OnlyOnceEver()
        {
            EnterGameplay();
            StartLevel(0);
            _bus.Publish(new TutorialHintEvent(true, Vector3.zero, Vector3.one));
            Shoot(2);

            _router.Dispose();
            _router = CreateRouter();
            StartLevel(0);
            _bus.Publish(new TutorialHintEvent(true, Vector3.zero, Vector3.one));
            Shoot(2);

            Assert.AreEqual(1, _backend.Names.Count(n => n == "tutorial_begin"));
            Assert.AreEqual(1, _backend.Names.Count(n => n == "tutorial_complete"));
        }

        [Test]
        public void Attempts_AndHighestLevel_PersistBetweenSessions()
        {
            EnterGameplay();
            StartLevel(5);
            ToMenu();

            _router.Dispose();
            _backend.UserProperties.Clear();
            _router = CreateRouter();
            Assert.AreEqual("6", _backend.UserProperties["highest_level"], "Se vuelve a enviar al arrancar.");

            EnterGameplay();
            StartLevel(5);
            Assert.AreEqual(2, Number(_backend.Last("level_start").Params, "attempt"));
        }

        [Test]
        public void Ads_AndLogin_AreForwarded()
        {
            EnterGameplay();
            StartLevel(6);
            _bus.Publish(new InterstitialShownEvent(8));
            _bus.Publish(new PlayerLoggedInEvent("PF42", true));

            Assert.AreEqual(7, Number(_backend.Last("interstitial_shown").Params, "level"));
            Assert.AreEqual("PF42", _backend.UserId);
        }

        [Test]
        public void BrokenBackend_NeverBreaksTheGame()
        {
            _backend.Throw = true;
            Assert.DoesNotThrow(() =>
            {
                EnterGameplay();
                StartLevel(0);
                _router.OnAppPaused();
            });
        }

        [Test]
        public void Context_Descriptions()
        {
            Assert.AreEqual("aiming", AnalyticsContext.Describe(AppStateId.Gameplay, GameStateId.Aiming, true));
            Assert.AreEqual("shot", AnalyticsContext.Describe(AppStateId.Gameplay, GameStateId.ShotResolve, true));
            Assert.AreEqual("menu", AnalyticsContext.Describe(AppStateId.MainMenu, GameStateId.None, true));
            Assert.AreEqual("loading", AnalyticsContext.Describe(AppStateId.Boot, GameStateId.None, true));
        }

        [Test]
        public void AdsService_PublishesAdEvents()
        {
            var provider = new InstantProvider();
            var ads = new AdsService(provider, InterstitialPolicy.Default, _bus);
            var seen = new List<string>();
            _bus.Subscribe<RewardedAdRequestedEvent>(_ => seen.Add("requested"));
            _bus.Subscribe<RewardedAdClosedEvent>(e => seen.Add("closed:" + e.Rewarded));
            _bus.Subscribe<InterstitialShownEvent>(e => seen.Add("interstitial:" + e.TotalVictories));

            ads.ShowRewarded(_ => { });
            ads.ShowInterstitialIfDue(4, () => { });

            CollectionAssert.AreEqual(new[] { "requested", "closed:True", "interstitial:4" }, seen);
            ads.Dispose();
        }

        private sealed class InstantProvider : IAdsProvider
        {
            public event Action BannerChanged { add { } remove { } }
            public float BannerHeightPixels => 0f;
            public bool IsInterstitialReady => true;
            public bool IsRewardedReady => true;
            public void Initialize(Action onReady) => onReady();
            public void ShowBanner() { }
            public void LoadInterstitial() { }
            public void ShowInterstitial(Action onClosed) => onClosed();
            public void LoadRewarded() { }
            public void ShowRewarded(Action<bool> onClosed) => onClosed(true);
        }
    }
}
