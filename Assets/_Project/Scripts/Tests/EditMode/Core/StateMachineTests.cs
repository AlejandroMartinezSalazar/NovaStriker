using System;
using System.Collections.Generic;
using NovaStriker.Core.StateMachine;
using NUnit.Framework;

namespace NovaStriker.Tests.EditMode.Core
{
    public class StateMachineTests
    {
        private enum TestStateId
        {
            None = 0,
            A,
            B,
            Overlay,
            WithPayload,
        }

        private sealed class RecordingState : IState, ITickableState, ISuspendableState
        {
            private readonly string _name;
            private readonly List<string> _log;

            public RecordingState(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public Action OnEnter;
            public float LastTick;

            public void Enter() { _log.Add($"{_name}.Enter"); OnEnter?.Invoke(); }
            public void Exit() => _log.Add($"{_name}.Exit");
            public void Tick(float deltaTime) { LastTick = deltaTime; _log.Add($"{_name}.Tick"); }
            public void OnSuspend() => _log.Add($"{_name}.Suspend");
            public void OnResume() => _log.Add($"{_name}.Resume");
        }

        private sealed class PayloadState : IPayloadState<int>
        {
            public int Received = -1;
            public void Enter(int payload) => Received = payload;
            public void Exit() { }
        }

        private List<string> _log;
        private StateMachine<TestStateId> _machine;
        private RecordingState _a;
        private RecordingState _b;
        private RecordingState _overlay;
        private PayloadState _payload;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _machine = new StateMachine<TestStateId>();
            _a = new RecordingState("A", _log);
            _b = new RecordingState("B", _log);
            _overlay = new RecordingState("O", _log);
            _payload = new PayloadState();
            _machine.Register(TestStateId.A, _a);
            _machine.Register(TestStateId.B, _b);
            _machine.Register(TestStateId.Overlay, _overlay);
            _machine.Register(TestStateId.WithPayload, _payload);
        }

        [Test]
        public void NewMachine_HasNoState()
        {
            Assert.IsFalse(_machine.HasState);
            Assert.AreEqual(TestStateId.None, _machine.CurrentId);
        }

        [Test]
        public void ChangeState_ExitsPreviousThenEntersNext()
        {
            _machine.ChangeState(TestStateId.A);
            _machine.ChangeState(TestStateId.B);

            CollectionAssert.AreEqual(new[] { "A.Enter", "A.Exit", "B.Enter" }, _log);
            Assert.AreEqual(TestStateId.B, _machine.CurrentId);
        }

        [Test]
        public void ChangeState_ToSameState_ReEnters()
        {
            _machine.ChangeState(TestStateId.A);
            _machine.ChangeState(TestStateId.A);

            CollectionAssert.AreEqual(new[] { "A.Enter", "A.Exit", "A.Enter" }, _log);
        }

        [Test]
        public void StateChanged_ReportsFromAndTo()
        {
            var changes = new List<(TestStateId, TestStateId)>();
            _machine.StateChanged += (from, to) => changes.Add((from, to));

            _machine.ChangeState(TestStateId.A);
            _machine.ChangeState(TestStateId.B);
            _machine.Stop();

            CollectionAssert.AreEqual(new[]
            {
                (TestStateId.None, TestStateId.A),
                (TestStateId.A, TestStateId.B),
                (TestStateId.B, TestStateId.None),
            }, changes);
        }

        [Test]
        public void ChangeState_WithPayload_PassesData()
        {
            _machine.ChangeState(TestStateId.WithPayload, 5);
            Assert.AreEqual(5, _payload.Received);
        }

        [Test]
        public void ChangeState_WrongPayloadType_ThrowsWithoutLeavingCurrentState()
        {
            _machine.ChangeState(TestStateId.A);

            Assert.Throws<InvalidOperationException>(() => _machine.ChangeState(TestStateId.WithPayload, "texto"));
            Assert.Throws<InvalidOperationException>(() => _machine.ChangeState(TestStateId.WithPayload));
            Assert.AreEqual(TestStateId.A, _machine.CurrentId);
            CollectionAssert.AreEqual(new[] { "A.Enter" }, _log);
        }

        [Test]
        public void ChangeState_Unregistered_Throws()
        {
            var empty = new StateMachine<TestStateId>();
            Assert.Throws<InvalidOperationException>(() => empty.ChangeState(TestStateId.A));
        }

        [Test]
        public void Register_None_Throws()
        {
            Assert.Throws<ArgumentException>(() => _machine.Register(TestStateId.None, new RecordingState("N", _log)));
        }

        [Test]
        public void Register_Duplicate_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _machine.Register(TestStateId.A, new RecordingState("A2", _log)));
        }

        [Test]
        public void Tick_OnlyUpdatesTopState()
        {
            _machine.ChangeState(TestStateId.A);
            _machine.Push(TestStateId.Overlay);
            _log.Clear();

            _machine.Tick(0.5f);

            CollectionAssert.AreEqual(new[] { "O.Tick" }, _log);
            Assert.AreEqual(0.5f, _overlay.LastTick);
        }

        [Test]
        public void PushPop_SuspendsAndResumesWithoutExitEnter()
        {
            _machine.ChangeState(TestStateId.A);
            _machine.Push(TestStateId.Overlay);
            Assert.AreEqual(2, _machine.Depth);
            Assert.AreEqual(TestStateId.Overlay, _machine.CurrentId);
            Assert.IsTrue(_machine.Contains(TestStateId.A));

            _machine.Pop();

            CollectionAssert.AreEqual(new[] { "A.Enter", "A.Suspend", "O.Enter", "O.Exit", "A.Resume" }, _log);
            Assert.AreEqual(TestStateId.A, _machine.CurrentId);
        }

        [Test]
        public void ChangeState_FromOverlay_ExitsWholeStackTopFirst()
        {
            _machine.ChangeState(TestStateId.A);
            _machine.Push(TestStateId.Overlay);
            _log.Clear();

            _machine.ChangeState(TestStateId.B);

            CollectionAssert.AreEqual(new[] { "O.Exit", "A.Exit", "B.Enter" }, _log);
            Assert.AreEqual(1, _machine.Depth);
        }

        [Test]
        public void Pop_BaseState_Throws()
        {
            _machine.ChangeState(TestStateId.A);
            Assert.Throws<InvalidOperationException>(() => _machine.Pop());
        }

        [Test]
        public void Push_WithoutBaseState_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _machine.Push(TestStateId.Overlay));
        }

        [Test]
        public void Push_StateAlreadyInStack_Throws()
        {
            _machine.ChangeState(TestStateId.A);
            Assert.Throws<InvalidOperationException>(() => _machine.Push(TestStateId.A));
        }

        [Test]
        public void TransitionRequestedInsideEnter_IsQueuedUntilCurrentFinishes()
        {
            _a.OnEnter = () => _machine.ChangeState(TestStateId.B);
            var changes = new List<(TestStateId, TestStateId)>();
            _machine.StateChanged += (from, to) => changes.Add((from, to));

            _machine.ChangeState(TestStateId.A);

            CollectionAssert.AreEqual(new[] { "A.Enter", "A.Exit", "B.Enter" }, _log);
            CollectionAssert.AreEqual(new[]
            {
                (TestStateId.None, TestStateId.A),
                (TestStateId.A, TestStateId.B),
            }, changes);
            Assert.AreEqual(TestStateId.B, _machine.CurrentId);
        }

        [Test]
        public void Stop_ExitsAll_AndMachineIsReusable()
        {
            _machine.ChangeState(TestStateId.A);
            _machine.Push(TestStateId.Overlay);

            _machine.Stop();
            Assert.IsFalse(_machine.HasState);

            _machine.ChangeState(TestStateId.B);
            CollectionAssert.AreEqual(new[] { "A.Enter", "A.Suspend", "O.Enter", "O.Exit", "A.Exit", "B.Enter" }, _log);
        }
    }
}
