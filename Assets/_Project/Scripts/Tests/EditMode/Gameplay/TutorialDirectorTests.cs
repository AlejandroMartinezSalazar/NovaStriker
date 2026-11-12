using System.Collections.Generic;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Gameplay.Level;
using NovaStriker.Gameplay.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    public class TutorialDirectorTests
    {
        private sealed class FakeHints : ILevelHintSource
        {
            public bool HasTarget = true;

            public bool TryGetAimHint(out Vector3 from, out Vector3 to)
            {
                from = new Vector3(0f, 0.12f, 0f);
                to = new Vector3(0f, 1f, 12f);
                return HasTarget;
            }
        }

        private EventBus _bus;
        private FakeHints _hints;
        private TutorialDirector _director;
        private List<TutorialHintEvent> _events;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _hints = new FakeHints();
            _director = new TutorialDirector(_bus, _hints);
            _events = new List<TutorialHintEvent>();
            _bus.Subscribe<TutorialHintEvent>(_events.Add);
        }

        [TearDown]
        public void TearDown() => _director.Dispose();

        private void StartLevel(int index)
        {
            _bus.Publish(new LevelStartedEvent(index, 12, "Nivel"));
            _bus.Publish(new GameStateChangedEvent(GameStateId.LevelSetup, GameStateId.Aiming));
        }

        [Test]
        public void FirstLevel_ShowsHint_WhenAimingStarts_WithBallAndTargetPoints()
        {
            StartLevel(0);

            Assert.AreEqual(1, _events.Count);
            Assert.IsTrue(_events[0].Visible);
            Assert.AreEqual(12f, _events[0].To.z, 1e-4f);
            Assert.IsTrue(_director.IsVisible);
        }

        [Test]
        public void OtherLevels_NeverShowHint()
        {
            StartLevel(1);

            Assert.AreEqual(0, _events.Count);
        }

        [Test]
        public void TouchHides_LiftingWithoutShootingShowsAgain_ShotHidesForGood()
        {
            StartLevel(0);
            _bus.Publish(new AimStartedEvent());
            Assert.IsFalse(_director.IsVisible, "Al poner el dedo se aparta para no tapar.");

            _bus.Publish(new AimCancelledEvent());
            Assert.IsTrue(_director.IsVisible, "Si suelta sin disparar, vuelve a enseñarse.");

            _bus.Publish(new AimStartedEvent());
            _bus.Publish(new ShotFiredEvent("s", Vector3.zero, Vector3.forward, Vector3.forward));
            _bus.Publish(new GameStateChangedEvent(GameStateId.BallInFlight, GameStateId.Aiming));
            Assert.IsFalse(_director.IsVisible, "Tras el primer tiro ya no se muestra en este intento.");
        }

        [Test]
        public void RestartingTheLevel_ShowsItAgain()
        {
            StartLevel(0);
            _bus.Publish(new ShotFiredEvent("s", Vector3.zero, Vector3.forward, Vector3.forward));
            Assert.IsFalse(_director.IsVisible);

            StartLevel(0);
            Assert.IsTrue(_director.IsVisible);
        }

        [Test]
        public void LeavingAiming_HidesIt()
        {
            StartLevel(0);
            _bus.Publish(new GameStateChangedEvent(GameStateId.Aiming, GameStateId.Paused));

            Assert.IsFalse(_director.IsVisible);
        }

        [Test]
        public void NoStandingTarget_NoHint()
        {
            _hints.HasTarget = false;
            StartLevel(0);

            Assert.AreEqual(0, _events.Count);
        }
    }
}
