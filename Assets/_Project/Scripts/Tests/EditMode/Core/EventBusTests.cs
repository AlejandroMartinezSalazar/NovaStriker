using System;
using System.Collections.Generic;
using NovaStriker.Core.Events;
using NUnit.Framework;

namespace NovaStriker.Tests.EditMode.Core
{
    public class EventBusTests
    {
        private readonly struct TestEvent : IGameEvent
        {
            public readonly int Value;
            public TestEvent(int value) => Value = value;
        }

        private readonly struct OtherEvent : IGameEvent
        {
        }

        private EventBus _bus;
        private List<Exception> _errors;

        [SetUp]
        public void SetUp()
        {
            _errors = new List<Exception>();
            _bus = new EventBus(_errors.Add);
        }

        [TearDown]
        public void TearDown() => _bus.Dispose();

        [Test]
        public void Publish_DeliversToSubscribers()
        {
            int received = 0;
            _bus.Subscribe<TestEvent>(e => received = e.Value);

            _bus.Publish(new TestEvent(42));

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Publish_WithoutSubscribers_DoesNothing()
        {
            Assert.DoesNotThrow(() => _bus.Publish(new TestEvent(1)));
        }

        [Test]
        public void Publish_OnlyReachesSameEventType()
        {
            int calls = 0;
            _bus.Subscribe<OtherEvent>(_ => calls++);

            _bus.Publish(new TestEvent(1));

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Publish_PreservesSubscriptionOrder()
        {
            var order = new List<int>();
            _bus.Subscribe<TestEvent>(_ => order.Add(1));
            _bus.Subscribe<TestEvent>(_ => order.Add(2));
            _bus.Subscribe<TestEvent>(_ => order.Add(3));

            _bus.Publish(new TestEvent(0));

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, order);
        }

        [Test]
        public void DisposeSubscription_StopsDelivery()
        {
            int calls = 0;
            IDisposable subscription = _bus.Subscribe<TestEvent>(_ => calls++);

            subscription.Dispose();
            _bus.Publish(new TestEvent(0));

            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, _bus.GetSubscriberCount<TestEvent>());
        }

        [Test]
        public void DisposeSubscription_Twice_IsSafe()
        {
            IDisposable subscription = _bus.Subscribe<TestEvent>(_ => { });
            subscription.Dispose();
            Assert.DoesNotThrow(subscription.Dispose);
        }

        [Test]
        public void HandlerException_IsReported_AndOthersStillReceive()
        {
            int received = 0;
            _bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("boom"));
            _bus.Subscribe<TestEvent>(e => received = e.Value);

            _bus.Publish(new TestEvent(7));

            Assert.AreEqual(7, received);
            Assert.AreEqual(1, _errors.Count);
            Assert.IsInstanceOf<InvalidOperationException>(_errors[0]);
        }

        [Test]
        public void SubscribeDuringPublish_ReceivesFromNextPublish()
        {
            int lateCalls = 0;
            _bus.Subscribe<TestEvent>(_ => _bus.Subscribe<TestEvent>(__ => lateCalls++));

            _bus.Publish(new TestEvent(0));
            Assert.AreEqual(0, lateCalls);

            _bus.Publish(new TestEvent(0));
            Assert.AreEqual(1, lateCalls);
        }

        [Test]
        public void UnsubscribeDuringPublish_LaterHandlerIsSkipped()
        {
            int secondCalls = 0;
            IDisposable second = null;
            _bus.Subscribe<TestEvent>(_ => second.Dispose());
            second = _bus.Subscribe<TestEvent>(_ => secondCalls++);

            _bus.Publish(new TestEvent(0));

            Assert.AreEqual(0, secondCalls);
            Assert.AreEqual(1, _bus.GetSubscriberCount<TestEvent>());
        }

        [Test]
        public void UnsubscribeSelfDuringPublish_Works()
        {
            int calls = 0;
            IDisposable self = null;
            self = _bus.Subscribe<TestEvent>(_ => { calls++; self.Dispose(); });

            _bus.Publish(new TestEvent(0));
            _bus.Publish(new TestEvent(0));

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void NestedPublish_IsDelivered()
        {
            var values = new List<int>();
            _bus.Subscribe<TestEvent>(e =>
            {
                values.Add(e.Value);
                if (e.Value < 3)
                {
                    _bus.Publish(new TestEvent(e.Value + 1));
                }
            });

            _bus.Publish(new TestEvent(1));

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, values);
        }

        [Test]
        public void Dispose_ClearsSubscriptions_AndRejectsNewUse()
        {
            int calls = 0;
            IDisposable subscription = _bus.Subscribe<TestEvent>(_ => calls++);

            _bus.Dispose();

            Assert.Throws<ObjectDisposedException>(() => _bus.Publish(new TestEvent(0)));
            Assert.Throws<ObjectDisposedException>(() => _bus.Subscribe<TestEvent>(_ => { }));
            Assert.DoesNotThrow(subscription.Dispose);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Subscribe_NullHandler_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _bus.Subscribe<TestEvent>(null));
        }
    }
}
