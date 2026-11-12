using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Core.Loop;
using NovaStriker.Gameplay.Aim;
using NovaStriker.Gameplay.Ball;
using NovaStriker.Gameplay.Input;
using NovaStriker.Gameplay.Throwing;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    public class ThrowAndAimControllerTests
    {
        // ---------- Dobles de prueba ----------

        private sealed class FakeBall : IBall
        {
            public Vector3 Position { get; set; }
            public int LaunchCount;
            public ThrowCommand LastCommand;

            public void Launch(in ThrowCommand command)
            {
                LaunchCount++;
                LastCommand = command;
            }
        }

        private sealed class FakeBallProvider : IBallProvider
        {
            public readonly List<FakeBall> Spawned = new List<FakeBall>();

            public IBall Spawn(Vector3 position)
            {
                var ball = new FakeBall { Position = position };
                Spawned.Add(ball);
                return ball;
            }
        }

        private sealed class FixedPhysics : IPhysicsSettings
        {
            public Vector3 Gravity => ThrowTestUtils.Gravity;
            public float FixedDeltaTime => ThrowTestUtils.FixedDeltaTime;
        }

        private sealed class FakeClock : IClock
        {
            public float Time { get; set; }
        }

        private sealed class FakePointer : IPointerInput
        {
            public event Action<Vector2> Pressed;
            public event Action<Vector2> Dragged;
            public event Action<Vector2> Released;

            public bool HasListeners => Pressed != null || Dragged != null || Released != null;
            public void Press(Vector2 p) => Pressed?.Invoke(p);
            public void Drag(Vector2 p) => Dragged?.Invoke(p);
            public void Release(Vector2 p) => Released?.Invoke(p);
        }

        private sealed class FakeUi : IUiHitTester
        {
            public bool Over;
            public bool IsOverUi(Vector2 screenPosition) => Over;
        }

        /// <summary>Convierte la pantalla en mundo: x→x/100, y→z/100. y &lt; 0 no tiene objetivo.</summary>
        private sealed class FakeResolver : IAimTargetResolver
        {
            public bool TryResolve(Vector2 screenPosition, out Vector3 worldPoint)
            {
                worldPoint = new Vector3(screenPosition.x / 100f, 0f, screenPosition.y / 100f);
                return screenPosition.y >= 0f;
            }
        }

        private sealed class FakePreview : IAimPreview
        {
            public bool Visible;
            public int ShowCount;
            public Vector3 LastTarget;

            public void Show(Vector3[] points, int count, Vector3 target)
            {
                Visible = true;
                ShowCount++;
                LastTarget = target;
            }

            public void Hide() => Visible = false;
        }

        private sealed class EventLog
        {
            public readonly List<string> Names = new List<string>();
            public ShotFiredEvent LastShot;

            public EventLog(IEventBus bus)
            {
                bus.Subscribe<AimStartedEvent>(_ => Names.Add("AimStarted"));
                bus.Subscribe<AimCancelledEvent>(_ => Names.Add("AimCancelled"));
                bus.Subscribe<ShotFiredEvent>(e => { Names.Add("ShotFired"); LastShot = e; });
            }
        }

        private static readonly Vector3 SpawnPosition = new Vector3(0f, 0.12f, 0f);

        private EventBus _bus;
        private EventLog _events;
        private FakeBallProvider _balls;
        private ThrowController _throwController;
        private FakePointer _pointer;
        private FakeUi _ui;
        private FakePreview _preview;
        private FakeClock _clock;
        private AimController _aim;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus(e => throw e);
            _events = new EventLog(_bus);
            _balls = new FakeBallProvider();
            _throwController = new ThrowController(_balls, SpawnPosition, new FixedPhysics(), _bus);
            _throwController.SetStrategy(new StraightThrowStrategy(StraightThrowSettings.Default));
            _pointer = new FakePointer();
            _ui = new FakeUi();
            _preview = new FakePreview();
            _clock = new FakeClock();
            _aim = CreateAim(minAimDuration: 0f);
        }

        [TearDown]
        public void TearDown()
        {
            _aim.Dispose();
            _bus.Dispose();
        }

        private AimController CreateAim(float minAimDuration)
        {
            return new AimController(_pointer, _ui, new FakeResolver(), _throwController, _preview, _bus, _clock,
                new FixedPhysics(), new AimSettings(minAimDuration, 0.25f, 40f, 8));
        }

        // ---------- ThrowController ----------

        [Test]
        public void Throw_WithoutPreparedBall_DoesNothing()
        {
            Assert.IsFalse(_throwController.TryThrow(new Vector3(0f, 0f, 10f), 0f));
            CollectionAssert.AreEqual(new string[0], _events.Names);
        }

        [Test]
        public void PrepareBall_SpawnsOnceAtSpawnPoint()
        {
            Assert.IsTrue(_throwController.PrepareBall());
            Assert.IsTrue(_throwController.PrepareBall());

            Assert.AreEqual(1, _balls.Spawned.Count);
            Assert.AreEqual(SpawnPosition, _balls.Spawned[0].Position);
            Assert.IsTrue(_throwController.HasBallReady);
        }

        [Test]
        public void Throw_LaunchesReadyBall_PublishesShot_AndConsumesBall()
        {
            _throwController.PrepareBall();
            var target = new Vector3(1f, 0f, 12f);

            Assert.IsTrue(_throwController.TryThrow(target, 0f));

            Assert.AreEqual(1, _balls.Spawned[0].LaunchCount);
            Assert.IsFalse(_throwController.HasBallReady);
            Assert.AreEqual(StraightThrowStrategy.StrategyId, _events.LastShot.StrategyId);
            Assert.AreEqual(target, _events.LastShot.Target);
            Assert.AreEqual(SpawnPosition, _events.LastShot.Origin);
        }

        [Test]
        public void Preview_DoesNotLaunch()
        {
            _throwController.PrepareBall();

            Assert.IsTrue(_throwController.TryPreview(new Vector3(0f, 0f, 10f), 0f, out Vector3 origin, out _));

            Assert.AreEqual(SpawnPosition, origin);
            Assert.AreEqual(0, _balls.Spawned[0].LaunchCount);
        }

        [Test]
        public void Throw_WithoutStrategy_Throws()
        {
            var controller = new ThrowController(_balls, SpawnPosition, new FixedPhysics(), _bus);
            controller.PrepareBall();

            Assert.Throws<InvalidOperationException>(() => controller.TryThrow(new Vector3(0f, 0f, 10f), 0f));
        }

        // ---------- AimController ----------

        [Test]
        public void PressDragRelease_FiresAtReleasePoint()
        {
            _throwController.PrepareBall();
            _aim.Enable();

            _pointer.Press(new Vector2(0f, 500f));
            _pointer.Drag(new Vector2(100f, 800f));
            Assert.IsTrue(_preview.Visible);
            _pointer.Release(new Vector2(200f, 1200f));

            CollectionAssert.AreEqual(new[] { "AimStarted", "ShotFired" }, _events.Names);
            Assert.AreEqual(new Vector3(2f, 0f, 12f), _events.LastShot.Target);
            Assert.IsFalse(_preview.Visible);
        }

        [Test]
        public void Press_OverUi_IsIgnored()
        {
            _throwController.PrepareBall();
            _aim.Enable();
            _ui.Over = true;

            _pointer.Press(new Vector2(0f, 500f));
            _pointer.Release(new Vector2(0f, 900f));

            CollectionAssert.AreEqual(new string[0], _events.Names);
            Assert.AreEqual(0, _balls.Spawned[0].LaunchCount);
        }

        [Test]
        public void Press_WithoutReadyBall_IsIgnored()
        {
            _aim.Enable();

            _pointer.Press(new Vector2(0f, 500f));

            Assert.IsFalse(_aim.IsAiming);
        }

        [Test]
        public void Release_WithoutTarget_Cancels()
        {
            _throwController.PrepareBall();
            _aim.Enable();

            _pointer.Press(new Vector2(0f, 500f));
            _pointer.Release(new Vector2(0f, -10f));

            CollectionAssert.AreEqual(new[] { "AimStarted", "AimCancelled" }, _events.Names);
            Assert.IsTrue(_throwController.HasBallReady, "El balón no se gasta.");
        }

        [Test]
        public void Release_TooQuick_Cancels_WhenMinDurationConfigured()
        {
            _aim.Dispose();
            _aim = CreateAim(minAimDuration: 0.2f);
            _throwController.PrepareBall();
            _aim.Enable();

            _clock.Time = 1f;
            _pointer.Press(new Vector2(0f, 500f));
            _clock.Time = 1.1f;
            _pointer.Release(new Vector2(0f, 900f));

            CollectionAssert.AreEqual(new[] { "AimStarted", "AimCancelled" }, _events.Names);
        }

        [Test]
        public void Disable_WhileAiming_CancelsAndUnsubscribes()
        {
            _throwController.PrepareBall();
            _aim.Enable();
            _pointer.Press(new Vector2(0f, 500f));

            _aim.Disable();

            Assert.IsFalse(_aim.IsAiming);
            Assert.IsFalse(_preview.Visible);
            Assert.IsFalse(_pointer.HasListeners);
            CollectionAssert.AreEqual(new[] { "AimStarted", "AimCancelled" }, _events.Names);
        }

        [Test]
        public void Disabled_IgnoresInput()
        {
            _throwController.PrepareBall();

            _pointer.Press(new Vector2(0f, 500f));
            _pointer.Release(new Vector2(0f, 900f));

            CollectionAssert.AreEqual(new string[0], _events.Names);
        }

        // ---------- Punto de saque por nivel ----------

        [Test]
        public void SetSpawnPosition_AppliesToNextPreparedBall()
        {
            var levelSpawn = new Vector3(1f, 0.2f, -3f);
            _throwController.SetSpawnPosition(levelSpawn);

            Assert.IsTrue(_throwController.PrepareBall());
            Assert.AreEqual(levelSpawn, _balls.Spawned[0].Position);

            Assert.IsTrue(_throwController.TryPreview(new Vector3(0f, 0f, 10f), 0f, out Vector3 origin, out _));
            Assert.AreEqual(levelSpawn, origin);
        }
    }
}
