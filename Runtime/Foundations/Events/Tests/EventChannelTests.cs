using NUnit.Framework;

namespace BrewedCode.Events.Tests
{
    [TestFixture]
    public class EventChannelTests
    {
        private struct TestEvent
        {
            public int Value;
            public string Message;

            public TestEvent(int value, string message)
            {
                Value = value;
                Message = message;
            }
        }

        [SetUp]
        public void SetUp()
        {
            // Clear all channels before each test
            EventChannelRegistry.ClearAll();
        }

        [Test]
        public void RaiseEvent_WithSubscriber_InvokesSubscriber()
        {
            var channel = new EventChannel<TestEvent>();
            TestEvent? received = null;

            channel.OnEventRaised += e => received = e;
            channel.RaiseEvent(new TestEvent(42, "hello"));

            Assert.IsNotNull(received);
            Assert.AreEqual(42, received.Value.Value);
            Assert.AreEqual("hello", received.Value.Message);
        }

        [Test]
        public void RaiseEvent_WithMultipleSubscribers_InvokesAll()
        {
            var channel = new EventChannel<TestEvent>();
            int callCount = 0;

            channel.OnEventRaised += _ => callCount++;
            channel.OnEventRaised += _ => callCount++;
            channel.OnEventRaised += _ => callCount++;

            channel.RaiseEvent(new TestEvent(1, "test"));

            Assert.AreEqual(3, callCount);
        }

        [Test]
        public void RaiseEvent_WithNoSubscribers_DoesNotThrow()
        {
            var channel = new EventChannel<TestEvent>();

            Assert.DoesNotThrow(() => channel.RaiseEvent(new TestEvent(1, "test")));
        }

        [Test]
        public void RaiseEvent_AfterUnsubscribe_DoesNotInvoke()
        {
            var channel = new EventChannel<TestEvent>();
            int callCount = 0;
            void Handler(TestEvent e) => callCount++;

            channel.OnEventRaised += Handler;
            channel.RaiseEvent(new TestEvent(1, "first"));
            Assert.AreEqual(1, callCount);

            channel.OnEventRaised -= Handler;
            channel.RaiseEvent(new TestEvent(2, "second"));
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Trigger_Static_RaisesOnGlobalChannel()
        {
            TestEvent? received = null;
            EventChannelRegistry.Get(out EventChannel<TestEvent> channel);
            channel.OnEventRaised += e => received = e;

            EventChannel<TestEvent>.Trigger(new TestEvent(100, "global"));

            Assert.IsNotNull(received);
            Assert.AreEqual(100, received.Value.Value);
        }

        [Test]
        public void Trigger_WithScopeKey_RaisesOnScopedChannel()
        {
            var scopeKey = EventScopeKey.ForInstance(999);
            TestEvent? globalReceived = null;
            TestEvent? scopedReceived = null;

            EventChannelRegistry.Get(out EventChannel<TestEvent> globalChannel);
            globalChannel.OnEventRaised += e => globalReceived = e;

            EventChannelRegistry.Get(scopeKey, out EventChannel<TestEvent> scopedChannel);
            scopedChannel.OnEventRaised += e => scopedReceived = e;

            EventChannel<TestEvent>.Trigger(new TestEvent(50, "scoped"), scopeKey);

            Assert.IsNull(globalReceived);
            Assert.IsNotNull(scopedReceived);
            Assert.AreEqual(50, scopedReceived.Value.Value);
        }

        [Test]
        public void Trigger_WithIEventScope_RaisesOnScopedChannel()
        {
            var scope = new MockEventScope(EventScopeKey.ForInstance(888));
            TestEvent? received = null;

            EventChannelRegistry.Get(scope.ScopeKey, out EventChannel<TestEvent> channel);
            channel.OnEventRaised += e => received = e;

            EventChannel<TestEvent>.Trigger(new TestEvent(77, "interface"), scope);

            Assert.IsNotNull(received);
            Assert.AreEqual(77, received.Value.Value);
        }

        [Test]
        public void ListenerCount_ReturnsCorrectCount()
        {
            var channel = new EventChannel<TestEvent>();

            Assert.AreEqual(0, channel.ListenerCount);

            channel.OnEventRaised += _ => { };
            Assert.AreEqual(1, channel.ListenerCount);

            channel.OnEventRaised += _ => { };
            Assert.AreEqual(2, channel.ListenerCount);
        }

        private class MockEventScope : IEventScope
        {
            public EventScopeKey ScopeKey { get; }

            public MockEventScope(EventScopeKey scopeKey)
            {
                ScopeKey = scopeKey;
            }
        }
    }
}
