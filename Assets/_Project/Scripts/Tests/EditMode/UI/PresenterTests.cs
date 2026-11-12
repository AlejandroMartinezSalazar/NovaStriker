using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using NovaStriker.UI.HUD;
using NovaStriker.UI.MainMenu;
using NovaStriker.Infrastructure.Progress;
using NovaStriker.UI.Results;
using NovaStriker.UI.Victory;
using NUnit.Framework;

namespace NovaStriker.Tests.EditMode.UI
{
    public class PresenterTests
    {
        private sealed class FakeNavigator : IAppNavigator
        {
            public int MainMenuCount;
            public int GameplayCount;
            public void GoToMainMenu() => MainMenuCount++;
            public void GoToGameplay() => GameplayCount++;
        }

        private sealed class FakeMainMenuView : IMainMenuView
        {
            public event Action PlayClicked;
            public event Action RestartCampaignClicked;
            public string LevelText;
            public List<LevelSquareState> Squares = new List<LevelSquareState>();
            public bool IsComingSoon;
            public void ClickPlay() => PlayClicked?.Invoke();
            public void ClickRestartCampaign() => RestartCampaignClicked?.Invoke();
            public bool HasListeners => PlayClicked != null || RestartCampaignClicked != null;

            public void ShowLevel(string levelText, IReadOnlyList<LevelSquareState> squares)
            {
                IsComingSoon = false;
                LevelText = levelText;
                Squares = new List<LevelSquareState>(squares);
            }

            public void ShowComingSoon() => IsComingSoon = true;
        }

        private sealed class FakeVictoryView : IVictoryView
        {
            public bool IsVisible = true;
            public string Subtitle;
            public void Show(string subtitle) { Subtitle = subtitle; IsVisible = true; }
            public void Hide() => IsVisible = false;
        }

        private sealed class FakeCommands : IGameCommands
        {
            public readonly List<string> Calls = new List<string>();
            public void Pause() => Calls.Add(nameof(Pause));
            public void Resume() => Calls.Add(nameof(Resume));
            public void RestartLevel() => Calls.Add(nameof(RestartLevel));
            public void RestartCampaign() => Calls.Add(nameof(RestartCampaign));
            public void ContinueWithExtraBall() => Calls.Add(nameof(ContinueWithExtraBall));
            public bool CanContinueWithExtraBall { get; set; } = true;
            public void GoToMainMenu() => Calls.Add(nameof(GoToMainMenu));
        }

        private sealed class FakeAds : IAdsService
        {
            public bool RewardedReady = true;
            public bool RewardResult = true;
            public int RewardedShown;
            public float BannerHeightPixels => 0f;
            public event Action BannerLayoutChanged { add { } remove { } }
            public bool IsRewardedReady => RewardedReady;
            public void ShowInterstitialIfDue(int totalVictories, Action onFinished) => onFinished();
            public void ShowRewarded(Action<bool> onCompleted) { RewardedShown++; onCompleted(RewardResult); }
        }

        private sealed class FakeHudView : IHudView
        {
            public event Action PauseClicked;
            public string Level, Targets, Balls;
            public bool PauseVisible = true;
            public void SetLevelText(string text) => Level = text;
            public void SetTargetsText(string text) => Targets = text;
            public void SetBallsText(string text) => Balls = text;
            public void SetPauseVisible(bool visible) => PauseVisible = visible;
            public void ClickPause() => PauseClicked?.Invoke();
            public bool HasListeners => PauseClicked != null;
        }

        private sealed class FakeResultView : IResultPanelView
        {
            public event Action PrimaryClicked;
            public event Action SecondaryClicked;
            public event Action MenuClicked;
            public bool IsVisible;
            public ResultPanelContent Content;
            public void Show(in ResultPanelContent content) { Content = content; IsVisible = true; }
            public void Hide() => IsVisible = false;
            public void ClickPrimary() => PrimaryClicked?.Invoke();
            public void ClickSecondary() => SecondaryClicked?.Invoke();
            public void ClickMenu() => MenuClicked?.Invoke();
            public bool HasListeners => PrimaryClicked != null || SecondaryClicked != null || MenuClicked != null;
        }

        // ---------- Menú principal ----------

        private const LevelSquareState Done = LevelSquareState.Completed;
        private const LevelSquareState Now = LevelSquareState.Current;
        private const LevelSquareState Todo = LevelSquareState.Locked;

        [Test]
        public void MainMenu_Play_GoesToGameplay()
        {
            var view = new FakeMainMenuView();
            var navigator = new FakeNavigator();
            using (new MainMenuPresenter(view, navigator, new InMemoryProgressService(), levelCount: 3, TestLocalization.Spanish()))
            {
                view.ClickPlay();
            }

            Assert.AreEqual(1, navigator.GameplayCount);
        }

        [Test]
        public void MainMenu_ShowsCurrentLevel_AndItsGroupOfTen()
        {
            var view = new FakeMainMenuView();
            using (new MainMenuPresenter(view, new FakeNavigator(), new InMemoryProgressService(6), levelCount: 25, TestLocalization.Spanish()))
            {
                Assert.AreEqual("NIVEL 7", view.LevelText);
                CollectionAssert.AreEqual(new[] { Done, Done, Done, Done, Done, Done, Now, Todo, Todo, Todo }, view.Squares);
            }
        }

        [Test]
        public void MainMenu_CampaignCompleted_ShowsComingSoon_AndRestartGoesBackToLevelOne()
        {
            var view = new FakeMainMenuView();
            var navigator = new FakeNavigator();
            var progress = new InMemoryProgressService(3);
            using (new MainMenuPresenter(view, navigator, progress, levelCount: 3, TestLocalization.Spanish()))
            {
                Assert.IsTrue(view.IsComingSoon);
                view.ClickPlay(); // No hay nivel que jugar.
                Assert.AreEqual(0, navigator.GameplayCount);

                view.ClickRestartCampaign();
                Assert.IsFalse(view.IsComingSoon);
                Assert.AreEqual(0, progress.CurrentLevelIndex);
                Assert.AreEqual("NIVEL 1", view.LevelText);
                CollectionAssert.AreEqual(new[] { Now, Todo, Todo }, view.Squares);
            }
        }

        [Test]
        public void MainMenu_Dispose_Unsubscribes()
        {
            var view = new FakeMainMenuView();
            var navigator = new FakeNavigator();
            new MainMenuPresenter(view, navigator, new InMemoryProgressService(), 3, TestLocalization.Spanish()).Dispose();

            view.ClickPlay();

            Assert.IsFalse(view.HasListeners);
            Assert.AreEqual(0, navigator.GameplayCount);
        }

        [Test]
        public void Presenters_RejectNullDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new MainMenuPresenter(null, new FakeNavigator(), new InMemoryProgressService(), 3, TestLocalization.Spanish()));
            Assert.Throws<ArgumentNullException>(() => new MainMenuPresenter(new FakeMainMenuView(), null, new InMemoryProgressService(), 3, TestLocalization.Spanish()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MainMenuPresenter(new FakeMainMenuView(), new FakeNavigator(), new InMemoryProgressService(), 0, TestLocalization.Spanish()));
            Assert.Throws<ArgumentNullException>(() => new VictoryPresenter(null, new EventBus(), TestLocalization.Spanish()));
            Assert.Throws<ArgumentNullException>(() => new HudPresenter(null, new EventBus(), new FakeCommands(), TestLocalization.Spanish()));
            Assert.Throws<ArgumentNullException>(() => new HudPresenter(new FakeHudView(), null, new FakeCommands(), TestLocalization.Spanish()));
            Assert.Throws<ArgumentNullException>(() => new ResultPanelPresenter(new FakeResultView(), new EventBus(), null, new FakeAds(), TestLocalization.Spanish()));
        }

        // ---------- HUD ----------

        [Test]
        public void Hud_ShowsLevel_TargetsRemaining_AndBallsRemaining()
        {
            var bus = new EventBus();
            var view = new FakeHudView();
            using (new HudPresenter(view, bus, new FakeCommands(), TestLocalization.Spanish()))
            {
                Assert.IsFalse(view.PauseVisible, "Sin partida no hay pausa.");

                bus.Publish(new LevelStartedEvent(1, 3, "Vidrio"));
                bus.Publish(new LevelProgressChangedEvent(2, 3, 4, 5));

                Assert.AreEqual("NIVEL 2", view.Level);
                Assert.AreEqual("Por derribar: 2/3", view.Targets);
                Assert.AreEqual("Balones: 4", view.Balls);
            }
        }

        [Test]
        public void Hud_PauseVisibleOnlyWhilePlaying_AndClickPauses()
        {
            var bus = new EventBus();
            var view = new FakeHudView();
            var commands = new FakeCommands();
            var presenter = new HudPresenter(view, bus, commands, TestLocalization.Spanish());

            bus.Publish(new GameStateChangedEvent(GameStateId.LevelSetup, GameStateId.Aiming));
            Assert.IsTrue(view.PauseVisible);
            bus.Publish(new GameStateChangedEvent(GameStateId.ShotResolve, GameStateId.Victory));
            Assert.IsFalse(view.PauseVisible);

            view.ClickPause();
            presenter.Dispose();
            view.ClickPause();
            bus.Publish(new LevelProgressChangedEvent(0, 3, 0, 5));

            CollectionAssert.AreEqual(new[] { "Pause" }, commands.Calls);
            Assert.IsFalse(view.HasListeners);
            Assert.AreEqual(string.Empty, view.Targets, "Tras Dispose el HUD ya no escucha el bus.");
        }

        // ---------- Panel de resultado ----------

        [Test]
        public void Result_HiddenWhilePlaying()
        {
            var bus = new EventBus();
            var view = new FakeResultView { IsVisible = true };
            using (new ResultPanelPresenter(view, bus, new FakeCommands(), new FakeAds(), TestLocalization.Spanish()))
            {
                Assert.IsFalse(view.IsVisible);
                bus.Publish(new GameStateChangedEvent(GameStateId.Paused, GameStateId.Aiming));
                Assert.IsFalse(view.IsVisible);
            }
        }

        [Test]
        public void Result_Pause_ContinueAndRestart()
        {
            var bus = new EventBus();
            var view = new FakeResultView();
            var commands = new FakeCommands();
            using (new ResultPanelPresenter(view, bus, commands, new FakeAds(), TestLocalization.Spanish()))
            {
                bus.Publish(new GameStateChangedEvent(GameStateId.Aiming, GameStateId.Paused));
                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual("PAUSA", view.Content.Title);

                view.ClickPrimary();
                view.ClickSecondary();
                view.ClickMenu();
            }

            CollectionAssert.AreEqual(new[] { "Resume", "RestartLevel", "GoToMainMenu" }, commands.Calls);
        }

        [Test]
        public void Result_Defeat_Retry_AndExtraBallWithRewardedAd()
        {
            var bus = new EventBus();
            var view = new FakeResultView();
            var commands = new FakeCommands();
            var ads = new FakeAds();
            using (new ResultPanelPresenter(view, bus, commands, ads, TestLocalization.Spanish()))
            {
                bus.Publish(new LevelProgressChangedEvent(2, 4, 0, 3));
                bus.Publish(new LevelFailedEvent(1, DefeatReason.OutOfBalls));
                bus.Publish(new GameStateChangedEvent(GameStateId.ShotResolve, GameStateId.Defeat));
                Assert.AreEqual("¡SIN BALONES!", view.Content.Title);
                Assert.AreEqual("Quedaron 2 por derribar", view.Content.Message);
                Assert.AreEqual("+2 BALONES", view.Content.SecondaryLabel);
                Assert.IsTrue(view.Content.SecondaryEnabled);
                Assert.AreEqual(ResultPanelStyle.Negative, view.Content.Style);

                view.ClickSecondary();
                view.ClickPrimary();
            }

            Assert.AreEqual(1, ads.RewardedShown);
            CollectionAssert.AreEqual(new[] { "ContinueWithExtraBall", "RestartLevel" }, commands.Calls);
        }

        [Test]
        public void Result_ExtraBall_NoRewardIfClosedEarly_DisabledIfNotLoaded_HiddenIfUsed()
        {
            var bus = new EventBus();
            var view = new FakeResultView();
            var commands = new FakeCommands();
            var ads = new FakeAds { RewardResult = false };
            using (new ResultPanelPresenter(view, bus, commands, ads, TestLocalization.Spanish()))
            {
                bus.Publish(new GameStateChangedEvent(GameStateId.ShotResolve, GameStateId.Defeat));
                view.ClickSecondary();
                Assert.AreEqual(0, commands.Calls.Count, "Cerró el anuncio antes: sin balón.");

                ads.RewardedReady = false;
                bus.Publish(new GameStateChangedEvent(GameStateId.ShotResolve, GameStateId.Defeat));
                Assert.IsFalse(view.Content.SecondaryEnabled, "Sin anuncio cargado el botón se ve desactivado.");

                commands.CanContinueWithExtraBall = false;
                bus.Publish(new GameStateChangedEvent(GameStateId.ShotResolve, GameStateId.Defeat));
                Assert.IsNull(view.Content.SecondaryLabel, "Ya usado en este intento: no se ofrece.");
            }
        }

        [Test]
        public void Result_IsHiddenDuringVictory()
        {
            var bus = new EventBus();
            var view = new FakeResultView();
            using (new ResultPanelPresenter(view, bus, new FakeCommands(), new FakeAds(), TestLocalization.Spanish()))
            {
                bus.Publish(new GameStateChangedEvent(GameStateId.ShotResolve, GameStateId.Victory));
                Assert.IsFalse(view.IsVisible);
            }
        }

        // ---------- Victoria ----------

        [Test]
        public void Victory_ShowsLevelCompleted_OnlyInVictory()
        {
            var bus = new EventBus();
            var view = new FakeVictoryView();
            var presenter = new VictoryPresenter(view, bus, TestLocalization.Spanish());
            Assert.IsFalse(view.IsVisible);

            bus.Publish(new LevelStartedEvent(3, 5, "X"));
            bus.Publish(new GameStateChangedEvent(GameStateId.ShotResolve, GameStateId.Victory));
            Assert.IsTrue(view.IsVisible);
            Assert.AreEqual("NIVEL 4 COMPLETADO", view.Subtitle);

            bus.Publish(new GameStateChangedEvent(GameStateId.Victory, GameStateId.None));
            Assert.IsFalse(view.IsVisible);

            presenter.Dispose();
            Assert.AreEqual(0, bus.GetSubscriberCount<GameStateChangedEvent>());
        }

        [Test]
        public void Result_ComingSoon_RestartsCampaign_AndDisposeUnsubscribes()
        {
            var bus = new EventBus();
            var view = new FakeResultView();
            var commands = new FakeCommands();
            var presenter = new ResultPanelPresenter(view, bus, commands, new FakeAds(), TestLocalization.Spanish());

            bus.Publish(new GameStateChangedEvent(GameStateId.LevelSetup, GameStateId.ComingSoon));
            Assert.AreEqual("PRÓXIMAMENTE", view.Content.Title);
            Assert.AreEqual("EMPEZAR DESDE EL NIVEL 1", view.Content.PrimaryLabel);
            view.ClickPrimary();

            presenter.Dispose();
            view.ClickPrimary();
            Assert.IsFalse(view.HasListeners);
            Assert.AreEqual(0, bus.GetSubscriberCount<GameStateChangedEvent>());
            CollectionAssert.AreEqual(new[] { "RestartCampaign" }, commands.Calls);
        }
    }
}
