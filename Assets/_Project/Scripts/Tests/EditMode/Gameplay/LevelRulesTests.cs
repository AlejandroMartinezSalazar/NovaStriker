using System.Collections.Generic;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Gameplay.Level.Rules;
using NovaStriker.Infrastructure.Progress;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    public class LevelRulesTests
    {
        private EventBus _bus;
        private LevelRulesTracker _rules;
        private List<LevelProgressChangedEvent> _progress;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _rules = new LevelRulesTracker(_bus);
            _progress = new List<LevelProgressChangedEvent>();
            _bus.Subscribe<LevelProgressChangedEvent>(_progress.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _rules.Dispose();
            _bus.Dispose();
        }

        private void KnockDown(int id) =>
            _bus.Publish(new CharacterKnockedDownEvent(id, true, KnockdownCause.BallHit, Vector3.zero));

        // ---------- LevelRulesTracker ----------

        [Test]
        public void Begin_CountsTargetsFromCharacters_AndPublishes()
        {
            _rules.Begin(3, new[] { new LevelCharacter(1, true), new LevelCharacter(2, true), new LevelCharacter(3, false) });

            LevelStatus status = _rules.Status;
            Assert.AreEqual(2, status.TargetsTotal);
            Assert.AreEqual(2, status.TargetsRemaining);
            Assert.AreEqual(3, status.BallsRemaining);
            Assert.AreEqual(1, _progress.Count);
            Assert.AreEqual(2, _progress[0].TargetsRemaining);
            Assert.AreEqual(3, _progress[0].BallsRemaining);
        }

        [Test]
        public void Knockdowns_CountOncePerCharacter_AndIgnoreOtherLevels()
        {
            _rules.Begin(3, new[] { new LevelCharacter(5, true), new LevelCharacter(6, true), new LevelCharacter(7, false) });

            KnockDown(5);
            KnockDown(5);  // Repetido.
            KnockDown(99); // De un nivel anterior.
            KnockDown(7);  // Neutral: no cuenta como objetivo.

            Assert.AreEqual(1, _rules.Status.TargetsDown);
            Assert.AreEqual(1, _rules.Status.NonTargetsDown);
            Assert.IsFalse(_rules.Status.AllTargetsDown);

            KnockDown(6);
            Assert.IsTrue(_rules.Status.AllTargetsDown);
            Assert.AreEqual(0, _progress[_progress.Count - 1].TargetsRemaining);
        }

        [Test]
        public void Shots_SpendBalls_UntilNoneLeft_AndExtraBallsAdd()
        {
            _rules.Begin(2, new[] { new LevelCharacter(1, true) });

            Assert.IsTrue(_rules.RegisterShot());
            Assert.IsTrue(_rules.RegisterShot());
            Assert.IsFalse(_rules.RegisterShot(), "Sin balones no se puede gastar otro.");
            Assert.AreEqual(0, _rules.Status.BallsRemaining);

            _rules.AddBalls(1);
            Assert.AreEqual(1, _rules.Status.BallsRemaining);
            Assert.AreEqual(1, _progress[_progress.Count - 1].BallsRemaining);
        }

        [Test]
        public void RewardedAd_GivesTwoBalls_OncePerAttempt()
        {
            _rules.Begin(1, new[] { new LevelCharacter(1, true) });
            _rules.RegisterShot();

            Assert.IsTrue(_rules.TryGrantExtraBall());
            Assert.AreEqual(LevelRulesTracker.RewardedExtraBalls, _rules.Status.BallsRemaining);
            Assert.AreEqual(2, _rules.Status.BallsRemaining);
            Assert.IsFalse(_rules.TryGrantExtraBall(), "Solo una vez por intento.");
            Assert.AreEqual(2, _rules.Status.BallsRemaining);
        }

        [Test]
        public void BallsInPlay_CountShotsUntilTheySettle()
        {
            _rules.Begin(3, new[] { new LevelCharacter(1, true) });
            _rules.RegisterShot();
            _rules.RegisterShot();
            Assert.AreEqual(2, _rules.BallsInPlay);

            _bus.Publish(new BallSettledEvent(BallSettleReason.OutOfBounds, Vector3.zero));
            Assert.AreEqual(1, _rules.BallsInPlay);
            _bus.Publish(new BallSettledEvent(BallSettleReason.Recycled, Vector3.zero));
            _bus.Publish(new BallSettledEvent(BallSettleReason.Stopped, Vector3.zero)); // De más: no baja de 0.
            Assert.AreEqual(0, _rules.BallsInPlay);

            _rules.RegisterShot();
            _rules.End();
            Assert.AreEqual(0, _rules.BallsInPlay, "Al descargar el nivel no queda nada en juego.");
        }

        [Test]
        public void End_StopsCounting_AndBeginResets()
        {
            _rules.Begin(2, new[] { new LevelCharacter(1, true) });
            _rules.RegisterShot();
            _rules.End();
            KnockDown(1);
            Assert.AreEqual(0, _rules.Status.TargetsDown);
            Assert.IsFalse(_rules.RegisterShot());

            _rules.Begin(4, new[] { new LevelCharacter(2, true) });
            Assert.AreEqual(4, _rules.Status.BallsRemaining);
            Assert.AreEqual(1, _rules.Status.TargetsRemaining);
        }

        [Test]
        public void Begin_RejectsInvalidData()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _rules.Begin(0, new[] { new LevelCharacter(1, true) }));
            Assert.Throws<System.ArgumentException>(() =>
                _rules.Begin(1, new[] { new LevelCharacter(1, true), new LevelCharacter(1, true) }));
        }

        // ---------- LevelOutcomeEvaluator ----------

        [Test]
        public void Outcome_VictoryHasPriority_OverOutOfBalls()
        {
            var evaluator = LevelOutcomeEvaluator.CreateDefault();
            var lastBallLastTarget = new LevelStatus(targetsTotal: 2, targetsDown: 2, nonTargetsDown: 0, ballsTotal: 3, ballsUsed: 3);

            Assert.AreEqual(LevelOutcome.Victory, evaluator.Evaluate(lastBallLastTarget, out DefeatReason reason));
            Assert.AreEqual(DefeatReason.None, reason);
        }

        [Test]
        public void Outcome_DefeatWhenOutOfBalls_OtherwiseContinue()
        {
            var evaluator = LevelOutcomeEvaluator.CreateDefault();

            Assert.AreEqual(LevelOutcome.Defeat, evaluator.Evaluate(new LevelStatus(3, 1, 0, 2, 2), out DefeatReason reason));
            Assert.AreEqual(DefeatReason.OutOfBalls, reason);
            Assert.AreEqual(LevelOutcome.None, evaluator.Evaluate(new LevelStatus(3, 1, 0, 2, 1), out _));
        }

        [Test]
        public void Outcome_IsExtensibleWithMoreDefeatConditions()
        {
            var evaluator = new LevelOutcomeEvaluator(
                new IVictoryCondition[] { new AllTargetsDownCondition() },
                new IDefeatCondition[] { new NoNeutralsDownCondition(), new OutOfBallsCondition() });

            Assert.AreEqual(LevelOutcome.Defeat, evaluator.Evaluate(new LevelStatus(2, 1, 1, 5, 1), out DefeatReason reason));
            Assert.AreEqual((DefeatReason)99, reason);
        }

        private sealed class NoNeutralsDownCondition : IDefeatCondition
        {
            public DefeatReason Check(in LevelStatus status) => status.NonTargetsDown > 0 ? (DefeatReason)99 : DefeatReason.None;
        }

        // ---------- ShotResolveTimer ----------

        [Test]
        public void ResolveTimer_WaitsForQuietTime_ChainKnockdownsExtendIt()
        {
            var timer = new ShotResolveTimer(minTime: 0.5f, quietTime: 1f, maxTime: 3f);
            timer.Begin();

            Assert.IsFalse(timer.Step(0.8f, false));
            timer.NotifyActivity(); // Cae alguien en cadena.
            Assert.IsFalse(timer.Step(0.9f, false));
            Assert.IsTrue(timer.Step(0.2f, false));
        }

        [Test]
        public void ResolveTimer_AllTargetsDown_OnlyNeedsMinTime_AndMaxTimeAlwaysEnds()
        {
            var timer = new ShotResolveTimer(0.5f, 1f, 3f);
            timer.Begin();
            Assert.IsFalse(timer.Step(0.3f, true));
            Assert.IsTrue(timer.Step(0.3f, true));

            timer.Begin();
            for (int i = 0; i < 5; i++)
            {
                timer.NotifyActivity();
                Assert.IsFalse(timer.Step(0.5f, false), $"Paso {i}: sigue habiendo actividad.");
            }

            timer.NotifyActivity();
            Assert.IsTrue(timer.Step(0.5f, false), "A los 3 s se resuelve igualmente.");
        }

        // ---------- Progreso ----------

        [Test]
        public void Progress_OnlyAdvances_AndRestartsCampaign()
        {
            var progress = new InMemoryProgressService();
            Assert.AreEqual(0, progress.CurrentLevelIndex);

            progress.CompleteLevel(0);
            progress.CompleteLevel(1);
            Assert.AreEqual(2, progress.CurrentLevelIndex);

            progress.CompleteLevel(0);
            Assert.AreEqual(2, progress.CurrentLevelIndex, "Repetir un nivel anterior no hace retroceder.");

            progress.RestartCampaign();
            Assert.AreEqual(0, progress.CurrentLevelIndex);
        }
    }
}
