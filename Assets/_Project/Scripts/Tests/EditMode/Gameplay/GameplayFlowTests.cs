using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using NovaStriker.Core.Loop;
using NovaStriker.Gameplay.Aim;
using NovaStriker.Gameplay.Level;
using NovaStriker.Gameplay.Level.Rules;
using NovaStriker.Gameplay.States;
using NovaStriker.Gameplay.Throwing;
using NovaStriker.Infrastructure.Progress;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    /// <summary>Flujo completo de la partida con dobles: sin escena, sin física.</summary>
    public class GameplayFlowTests
    {
        // ---------- Dobles de prueba ----------

        /// <summary>Cada nivel tiene sus objetivos con ids únicos (nivel*100 + n) y sus balones.</summary>
        private sealed class FakeSession : ILevelSession
        {
            private readonly LevelRulesTracker _rules;
            private readonly (int Targets, int Balls)[] _levels;
            public readonly List<int> Loads = new List<int>();
            public int UnloadCount;

            public FakeSession(LevelRulesTracker rules, params (int Targets, int Balls)[] levels)
            {
                _rules = rules;
                _levels = levels;
            }

            public int LevelCount => _levels.Length;
            public int CurrentLevelIndex { get; private set; } = -1;
            public bool HasLevel(int levelIndex) => levelIndex >= 0 && levelIndex < _levels.Length;

            public void Load(int levelIndex)
            {
                Unload();
                Loads.Add(levelIndex);
                CurrentLevelIndex = levelIndex;
                var characters = new List<LevelCharacter>();
                for (int i = 1; i <= _levels[levelIndex].Targets; i++)
                {
                    characters.Add(new LevelCharacter(TargetId(levelIndex, i), true));
                }
                _rules.Begin(_levels[levelIndex].Balls, characters);
            }

            public void Unload()
            {
                UnloadCount++;
                _rules.End();
                CurrentLevelIndex = -1;
            }

            public static int TargetId(int level, int n) => level * 100 + n;
        }

        private sealed class FakeThrowController : IThrowController
        {
            public int PrepareCount;
            public bool HasBallReady { get; private set; }
            public string StrategyId => "fake";
            public void SetStrategy(IThrowStrategy strategy) { }
            public void SetSpawnPosition(Vector3 position) { }
            public bool PrepareBall() { PrepareCount++; HasBallReady = true; return true; }
            public void ClearReadyBall() => HasBallReady = false;
            public bool TryPreview(Vector3 target, float curveBias, out Vector3 origin, out ThrowCommand command)
            {
                origin = default;
                command = default;
                return false;
            }
            public bool TryThrow(Vector3 target, float curveBias) => false;
        }

        private sealed class FakeAim : IAimController
        {
            public bool IsEnabled { get; private set; }
            public void Enable() => IsEnabled = true;
            public void Disable() => IsEnabled = false;
        }

        private sealed class FakeAds : IAdsService
        {
            public readonly List<int> InterstitialRequests = new List<int>();
            public Action PendingInterstitial;
            public bool HoldInterstitial;
            public float BannerHeightPixels => 0f;
            public event Action BannerLayoutChanged { add { } remove { } }
            public bool IsRewardedReady => true;

            public void ShowInterstitialIfDue(int totalVictories, Action onFinished)
            {
                InterstitialRequests.Add(totalVictories);
                if (HoldInterstitial)
                {
                    PendingInterstitial = onFinished;
                }
                else
                {
                    onFinished();
                }
            }

            public void ShowRewarded(Action<bool> onCompleted) => onCompleted(true);
        }

        private sealed class FakeTimeScale : ITimeScale
        {
            public float Scale { get; set; } = 1f;
        }

        private sealed class FakeNavigator : IAppNavigator
        {
            public int MainMenuCount;
            public void GoToMainMenu() => MainMenuCount++;
            public void GoToGameplay() { }
        }

        // ---------- Montaje ----------

        private EventBus _bus;
        private LevelRulesTracker _rules;
        private FakeSession _session;
        private FakeThrowController _throw;
        private FakeAim _aim;
        private FakeTimeScale _time;
        private FakeNavigator _navigator;
        private FakeAds _ads;
        private InMemoryProgressService _progress;
        private GameplayFlow _flow;
        private GameCommands _commands;
        private List<GameStateId> _states;

        private void Build(params (int Targets, int Balls)[] levels)
        {
            _bus = new EventBus();
            _rules = new LevelRulesTracker(_bus);
            _session = new FakeSession(_rules, levels);
            _throw = new FakeThrowController();
            _aim = new FakeAim();
            _time = new FakeTimeScale();
            _navigator = new FakeNavigator();
            _ads = new FakeAds();
            _progress = new InMemoryProgressService();
            _flow = new GameplayFlow(_bus, _session, _rules, _throw, _aim, _progress, _navigator, _ads, _time,
                LevelOutcomeEvaluator.CreateDefault(), GameFlowSettings.Default);
            _commands = new GameCommands(_flow.Machine, _session, _progress, _rules, _navigator);
            _states = new List<GameStateId>();
            _bus.Subscribe<GameStateChangedEvent>(e => _states.Add(e.To));
        }

        [TearDown]
        public void TearDown()
        {
            _flow?.Dispose();
            _rules?.Dispose();
            _bus?.Dispose();
        }

        private GameStateId State => _flow.CurrentState;

        private static float Reload => GameFlowSettings.Default.ReloadDelay;

        /// <summary>Simula un disparo: el balón preparado sale y se publica ShotFiredEvent.</summary>
        private void Shoot()
        {
            _throw.ClearReadyBall();
            _bus.Publish(new ShotFiredEvent("fake", Vector3.zero, Vector3.forward, Vector3.forward));
        }

        private void Settle() => _bus.Publish(new BallSettledEvent(BallSettleReason.Stopped, Vector3.zero));

        private void KnockDown(int level, int n) =>
            _bus.Publish(new CharacterKnockedDownEvent(FakeSession.TargetId(level, n), true, KnockdownCause.BallHit, Vector3.zero));

        /// <summary>Tiro sin derribos que no es el último: dispara y espera a que aparezca el siguiente balón.</summary>
        private void ShootAndReload()
        {
            Shoot();
            _flow.Tick(Reload + 0.05f);
        }

        /// <summary>Último tiro fallado: se detiene el balón y se calma la escena.</summary>
        private void LastShotMissed()
        {
            Shoot();
            Settle();
            _flow.Tick(0.01f);
            _flow.Tick(1.1f);
        }

        // ---------- Tests ----------

        [Test]
        public void Start_BuildsLevel_AndLetsThePlayerAim()
        {
            Build((2, 3));
            _flow.Start(0);

            Assert.AreEqual(GameStateId.Aiming, State);
            CollectionAssert.AreEqual(new[] { 0 }, _session.Loads);
            Assert.IsTrue(_aim.IsEnabled);
            Assert.IsTrue(_throw.HasBallReady);
            CollectionAssert.AreEqual(new[] { GameStateId.LevelSetup, GameStateId.Aiming }, _states);
        }

        [Test]
        public void Shot_SpendsBall_AndNextBallAppearsAlmostInstantly_WithoutWaitingForTheFirst()
        {
            Build((2, 3));
            _flow.Start(0);

            Shoot();
            Assert.AreEqual(GameStateId.Aiming, State, "Se sigue apuntando con el balón anterior aún en el aire.");
            Assert.IsTrue(_aim.IsEnabled);
            Assert.AreEqual(2, _rules.Status.BallsRemaining);
            Assert.AreEqual(1, _rules.BallsInPlay);
            Assert.IsFalse(_throw.HasBallReady);

            _flow.Tick(Reload * 0.5f);
            Assert.IsFalse(_throw.HasBallReady, "Aún no: evita solaparse con el balón que acaba de salir.");
            _flow.Tick(Reload * 0.6f);
            Assert.IsTrue(_throw.HasBallReady);
            Assert.AreEqual(2, _throw.PrepareCount);
        }

        [Test]
        public void LastBall_DefeatIsDecidedOnlyAfterTheShotResolves()
        {
            Build((2, 1));
            var failures = new List<LevelFailedEvent>();
            _bus.Subscribe<LevelFailedEvent>(failures.Add);
            _flow.Start(0);

            Shoot();
            Assert.AreEqual(GameStateId.BallInFlight, State, "No se pierde al lanzar el último balón.");
            _flow.Tick(1f);
            Assert.AreEqual(GameStateId.BallInFlight, State, "El balón sigue en juego.");
            Settle();
            _flow.Tick(0.01f);
            Assert.AreEqual(GameStateId.ShotResolve, State);
            _flow.Tick(1.1f);

            Assert.AreEqual(GameStateId.Defeat, State);
            Assert.AreEqual(1, failures.Count);
            Assert.AreEqual(DefeatReason.OutOfBalls, failures[0].Reason);
            Assert.AreEqual(0, _progress.CurrentLevelIndex);
            Assert.AreEqual(1, _throw.PrepareCount, "Sin balones no se prepara otro.");
        }

        [Test]
        public void LastBall_WaitsForEveryBallStillInPlay()
        {
            Build((2, 2));
            _flow.Start(0);

            ShootAndReload();
            Shoot();
            Assert.AreEqual(GameStateId.BallInFlight, State);
            Assert.AreEqual(2, _rules.BallsInPlay);

            Settle();
            _flow.Tick(0.01f);
            Assert.AreEqual(GameStateId.BallInFlight, State, "Queda un balón rodando.");

            Settle();
            _flow.Tick(0.01f);
            Assert.AreEqual(GameStateId.ShotResolve, State);
        }

        [Test]
        public void LastBallKnocksLastTarget_IsVictory_AndAdvancesProgress()
        {
            Build((1, 1), (1, 1));
            var completed = new List<LevelCompletedEvent>();
            _bus.Subscribe<LevelCompletedEvent>(completed.Add);
            _flow.Start(0);

            Shoot();
            KnockDown(0, 1);
            Assert.AreEqual(GameStateId.ShotResolve, State, "Sin objetivos no hace falta esperar al balón.");
            _flow.Tick(0.8f);

            Assert.AreEqual(GameStateId.Victory, State);
            Assert.AreEqual(1, _progress.CurrentLevelIndex);
            Assert.AreEqual(1, completed.Count);
            Assert.IsFalse(completed[0].IsLastLevel);
        }

        [Test]
        public void KnockingTheLastTarget_WithBallsLeft_WinsWithoutMoreShots()
        {
            Build((2, 5));
            _flow.Start(0);

            ShootAndReload();
            KnockDown(0, 1);
            Shoot();
            KnockDown(0, 2);
            Assert.AreEqual(GameStateId.ShotResolve, State);
            _flow.Tick(0.8f);

            Assert.AreEqual(GameStateId.Victory, State);
            Assert.AreEqual(3, _rules.Status.BallsRemaining);
        }

        [Test]
        public void ChainKnockdowns_DuringResolve_DelayTheDecision_AndCount()
        {
            Build((3, 1));
            _flow.Start(0);

            Shoot();
            KnockDown(0, 1);
            Settle();
            _flow.Tick(0.01f);
            _flow.Tick(0.9f);
            KnockDown(0, 2); // Cae otro en cadena (p. ej. le cae una caja).
            _flow.Tick(0.5f);
            Assert.AreEqual(GameStateId.ShotResolve, State);
            _flow.Tick(0.6f);

            Assert.AreEqual(GameStateId.Defeat, State);
            Assert.AreEqual(1, _rules.Status.TargetsRemaining);
        }

        [Test]
        public void LateKnockdownWhileAiming_OfTheLastTarget_WinsWithoutShooting()
        {
            Build((1, 3));
            _flow.Start(0);

            KnockDown(0, 1); // Se cae de la plataforma móvil, por ejemplo.
            Assert.AreEqual(GameStateId.ShotResolve, State);
            _flow.Tick(0.8f);

            Assert.AreEqual(GameStateId.Victory, State);
            Assert.AreEqual(3, _rules.Status.BallsRemaining);
        }

        [Test]
        public void Victory_ShowsForAFewSeconds_ThenGoesToTheMainMenuOnce()
        {
            Build((1, 2), (1, 2));
            _flow.Start(0);
            KnockDown(0, 1);
            _flow.Tick(0.8f);
            Assert.AreEqual(GameStateId.Victory, State);

            float screenTime = GameFlowSettings.Default.VictoryScreenTime;
            _flow.Tick(screenTime - 0.1f);
            Assert.AreEqual(0, _navigator.MainMenuCount, "Aún se está mostrando la victoria.");
            _flow.Tick(0.2f);
            _flow.Tick(1f);

            Assert.AreEqual(1, _navigator.MainMenuCount);
            Assert.AreEqual(1, _progress.CurrentLevelIndex, "Al volver a jugar toca el nivel 2.");
        }

        [Test]
        public void Victory_AsksForInterstitial_AndWaitsForItToClose()
        {
            Build((1, 2));
            _ads.HoldInterstitial = true;
            _flow.Start(0);
            KnockDown(0, 1);
            _flow.Tick(0.8f);
            _flow.Tick(GameFlowSettings.Default.VictoryScreenTime + 0.1f);

            CollectionAssert.AreEqual(new[] { 1 }, _ads.InterstitialRequests, "Con el total de victorias.");
            Assert.AreEqual(0, _navigator.MainMenuCount, "Espera a que se cierre el anuncio.");

            _ads.PendingInterstitial();
            Assert.AreEqual(1, _navigator.MainMenuCount);
        }

        [Test]
        public void ExtraBall_OnlyOncePerAttempt_ResetByRestart()
        {
            Build((2, 1));
            _flow.Start(0);
            LastShotMissed();
            Assert.IsTrue(_commands.CanContinueWithExtraBall);
            _commands.ContinueWithExtraBall();
            LastShotMissed();
            Assert.AreEqual(GameStateId.Aiming, State, "El anuncio da 2 balones: tras fallar uno queda otro.");
            LastShotMissed();

            Assert.AreEqual(GameStateId.Defeat, State);
            Assert.IsFalse(_commands.CanContinueWithExtraBall);
            _commands.ContinueWithExtraBall();
            Assert.AreEqual(GameStateId.Defeat, State, "La segunda vez no se concede.");

            _commands.RestartLevel();
            LastShotMissed();
            Assert.IsTrue(_commands.CanContinueWithExtraBall, "Un intento nuevo vuelve a permitirlo.");
        }

        [Test]
        public void LastLevelWon_NextVisitGoesToComingSoon_AndRestartCampaignGoesToLevelOne()
        {
            Build((1, 2), (1, 2));
            var campaign = new List<CampaignCompletedEvent>();
            _bus.Subscribe<CampaignCompletedEvent>(campaign.Add);
            var completed = new List<LevelCompletedEvent>();
            _bus.Subscribe<LevelCompletedEvent>(completed.Add);

            _flow.Start(1);
            KnockDown(1, 1);
            _flow.Tick(0.8f);
            Assert.IsTrue(completed[0].IsLastLevel);
            Assert.AreEqual(2, _progress.CurrentLevelIndex);

            _flow.Start(_progress.CurrentLevelIndex);
            Assert.AreEqual(GameStateId.ComingSoon, State);
            Assert.AreEqual(1, campaign.Count);

            _commands.RestartCampaign();
            Assert.AreEqual(GameStateId.Aiming, State);
            Assert.AreEqual(0, _progress.CurrentLevelIndex);
            CollectionAssert.AreEqual(new[] { 1, 0 }, _session.Loads);
        }

        [Test]
        public void Start_WithCampaignAlreadyCompleted_GoesToComingSoon()
        {
            Build((1, 1));
            _progress.CompleteLevel(0);
            _flow.Start(_progress.CurrentLevelIndex);

            Assert.AreEqual(GameStateId.ComingSoon, State);
            Assert.AreEqual(0, _session.Loads.Count);
        }

        [Test]
        public void Pause_FreezesTime_BlocksAim_AndResumeRestores()
        {
            Build((2, 3));
            _flow.Start(0);

            _commands.Pause();
            Assert.AreEqual(GameStateId.Paused, State);
            Assert.AreEqual(0f, _time.Scale);
            Assert.IsFalse(_aim.IsEnabled);

            _commands.Pause(); // Ya en pausa: se ignora.
            Assert.AreEqual(2, _flow.Machine.Depth);

            _commands.Resume();
            Assert.AreEqual(GameStateId.Aiming, State);
            Assert.AreEqual(1f, _time.Scale);
            Assert.IsTrue(_aim.IsEnabled);
        }

        [Test]
        public void Pause_FreezesTheReload()
        {
            Build((2, 3));
            _flow.Start(0);
            Shoot();

            _commands.Pause();
            _flow.Tick(10f);
            _commands.Resume();
            Assert.IsFalse(_throw.HasBallReady);

            _flow.Tick(Reload + 0.05f);
            Assert.IsTrue(_throw.HasBallReady);
        }

        [Test]
        public void Pause_DuringLastBall_KeepsWaitingForIt()
        {
            Build((2, 1));
            _flow.Start(0);
            Shoot();

            _commands.Pause();
            _flow.Tick(100f); // En pausa no avanza el tiempo de espera.
            _commands.Resume();
            Assert.AreEqual(GameStateId.BallInFlight, State);

            Settle();
            _flow.Tick(0.01f);
            Assert.AreEqual(GameStateId.ShotResolve, State);
        }

        [Test]
        public void Pause_IsIgnoredOutsideOfPlay()
        {
            Build((1, 1));
            _flow.Start(0);
            KnockDown(0, 1);
            _flow.Tick(0.8f);

            _commands.Pause();
            _commands.RestartLevel(); // Tras ganar no se repite: solo se avanza.

            Assert.AreEqual(GameStateId.Victory, State);
            Assert.AreEqual(1f, _time.Scale);
        }

        [Test]
        public void RestartLevel_FromPause_RebuildsTheSameLevel_AndRestoresTime()
        {
            Build((2, 3));
            _flow.Start(0);
            ShootAndReload();
            Assert.AreEqual(2, _rules.Status.BallsRemaining);

            _commands.Pause();
            _commands.RestartLevel();

            Assert.AreEqual(GameStateId.Aiming, State);
            Assert.AreEqual(1f, _time.Scale);
            Assert.AreEqual(3, _rules.Status.BallsRemaining);
            Assert.AreEqual(0, _rules.BallsInPlay);
            CollectionAssert.AreEqual(new[] { 0, 0 }, _session.Loads);
        }

        [Test]
        public void RestartLevel_AfterDefeat_StartsOver()
        {
            Build((2, 1));
            _flow.Start(0);
            LastShotMissed();
            Assert.AreEqual(GameStateId.Defeat, State);

            _commands.RestartLevel();

            Assert.AreEqual(GameStateId.Aiming, State);
            Assert.AreEqual(1, _rules.Status.BallsRemaining);
            Assert.AreEqual(2, _rules.Status.TargetsRemaining);
        }

        [Test]
        public void ContinueWithExtraBall_AfterDefeat_KeepsKnockdowns()
        {
            Build((2, 1));
            _flow.Start(0);
            Shoot();
            KnockDown(0, 1);
            Settle();
            _flow.Tick(0.01f);
            _flow.Tick(1.1f);
            Assert.AreEqual(GameStateId.Defeat, State);

            _commands.ContinueWithExtraBall();

            Assert.AreEqual(GameStateId.Aiming, State);
            Assert.AreEqual(LevelRulesTracker.RewardedExtraBalls, _rules.Status.BallsRemaining);
            Assert.AreEqual(1, _rules.Status.TargetsRemaining, "Se sigue con lo ya derribado.");
            Assert.IsTrue(_throw.HasBallReady);
            CollectionAssert.AreEqual(new[] { 0 }, _session.Loads, "No se reconstruye el nivel.");
        }

        [Test]
        public void BallThatNeverSettles_ResolvesAfterMaxFlightTime()
        {
            Build((2, 1));
            _flow.Start(0);
            Shoot();

            _flow.Tick(GameFlowSettings.Default.MaxBallFlightTime + 0.1f);

            Assert.AreEqual(GameStateId.ShotResolve, State);
        }

        [Test]
        public void GoToMainMenu_WhilePaused_UnpausesBeforeLeaving()
        {
            Build((2, 3));
            _flow.Start(0);
            _commands.Pause();

            _commands.GoToMainMenu();

            Assert.AreEqual(1, _navigator.MainMenuCount);
            Assert.AreEqual(1f, _time.Scale);
        }

        [Test]
        public void Dispose_ExitsAllStates_RestoresTime_AndDisablesAim()
        {
            Build((2, 3));
            _flow.Start(0);
            _commands.Pause();

            _flow.Dispose();

            Assert.AreEqual(GameStateId.None, _flow.CurrentState);
            Assert.AreEqual(1f, _time.Scale);
            Assert.IsFalse(_aim.IsEnabled);
            Assert.AreEqual(GameStateId.None, _states[_states.Count - 1]);
        }
    }
}
