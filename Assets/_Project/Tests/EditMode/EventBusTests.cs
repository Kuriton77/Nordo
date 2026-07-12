using NUnit.Framework;
using Nordo.Core;

namespace Nordo.Tests
{
    /// <summary>
    /// Verifies the contract of <see cref="EventBus{T}"/>: delivery, unsubscription,
    /// multiple subscribers, and safe raising with no listeners. These guarantees are
    /// load-bearing because the whole project communicates through this bus.
    /// </summary>
    public sealed class EventBusTests
    {
        /// <summary>A tiny value-type event used only by these tests.</summary>
        private readonly struct TestEvent : IGameEvent
        {
            public readonly int Value;
            public TestEvent(int value) => Value = value;
        }

        [TearDown]
        public void TearDown()
        {
            // Ensure no handler leaks between test cases.
            EventBus<TestEvent>.Clear();
        }

        [Test]
        public void Raise_DeliversPayload_ToSubscriber()
        {
            int received = 0;
            void Handler(TestEvent e) => received = e.Value;

            EventBus<TestEvent>.Subscribe(Handler);
            EventBus<TestEvent>.Raise(new TestEvent(42));

            Assert.AreEqual(42, received);

            EventBus<TestEvent>.Unsubscribe(Handler);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int callCount = 0;
            void Handler(TestEvent _) => callCount++;

            EventBus<TestEvent>.Subscribe(Handler);
            EventBus<TestEvent>.Unsubscribe(Handler);
            EventBus<TestEvent>.Raise(new TestEvent(1));

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Raise_NotifiesAllSubscribers()
        {
            int a = 0;
            int b = 0;
            void HandlerA(TestEvent e) => a = e.Value;
            void HandlerB(TestEvent e) => b = e.Value * 2;

            EventBus<TestEvent>.Subscribe(HandlerA);
            EventBus<TestEvent>.Subscribe(HandlerB);
            EventBus<TestEvent>.Raise(new TestEvent(5));

            Assert.AreEqual(5, a);
            Assert.AreEqual(10, b);
        }

        [Test]
        public void Raise_WithNoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => EventBus<TestEvent>.Raise(new TestEvent(0)));
        }
    }
}
