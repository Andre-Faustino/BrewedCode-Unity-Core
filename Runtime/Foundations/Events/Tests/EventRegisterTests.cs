using NUnit.Framework;
using UnityEngine;

namespace BrewedCode.Events.Tests
{
    [TestFixture]
    public class EventRegisterTests
    {
        private struct TestEvent { public int Value; }

        [SetUp]
        public void SetUp()
        {
            EventChannelRegistry.ClearAll();
        }

        [Test]
        public void EventStartListening_Global_RegistersListener()
        {
            var listener = new MockListener();

            listener.EventStartListening<TestEvent>();

            EventChannel<TestEvent>.Trigger(new TestEvent { Value = 42 });

            Assert.AreEqual(1, listener.ReceivedCount);
            Assert.AreEqual(42, listener.LastValue);
        }

        [Test]
        public void EventStopListening_Global_UnregistersListener()
        {
            var listener = new MockListener();

            listener.EventStartListening<TestEvent>();
            listener.EventStopListening<TestEvent>();

            EventChannel<TestEvent>.Trigger(new TestEvent { Value = 42 });

            Assert.AreEqual(0, listener.ReceivedCount);
        }

        [Test]
        public void EventStartListening_Scoped_RegistersOnScope()
        {
            var scopeKey = EventScopeKey.ForInstance(100);
            var listener = new MockListener();

            listener.EventStartListening<TestEvent>(scopeKey);

            // Should not receive global events
            EventChannel<TestEvent>.Trigger(new TestEvent { Value = 1 });
            Assert.AreEqual(0, listener.ReceivedCount);

            // Should receive scoped events
            EventChannel<TestEvent>.Trigger(new TestEvent { Value = 2 }, scopeKey);
            Assert.AreEqual(1, listener.ReceivedCount);
            Assert.AreEqual(2, listener.LastValue);
        }

        [Test]
        public void EventStopListening_Scoped_UnregistersFromScope()
        {
            var scopeKey = EventScopeKey.ForInstance(100);
            var listener = new MockListener();

            listener.EventStartListening<TestEvent>(scopeKey);
            listener.EventStopListening<TestEvent>(scopeKey);

            EventChannel<TestEvent>.Trigger(new TestEvent { Value = 42 }, scopeKey);

            Assert.AreEqual(0, listener.ReceivedCount);
        }

        [Test]
        public void EventStartListening_WithIEventScope_RegistersOnScope()
        {
            var scope = new MockScope(EventScopeKey.ForScene(200));
            var listener = new MockListener();

            listener.EventStartListening<TestEvent>(scope);

            EventChannel<TestEvent>.Trigger(new TestEvent { Value = 77 }, scope);

            Assert.AreEqual(1, listener.ReceivedCount);
            Assert.AreEqual(77, listener.LastValue);
        }

        [Test]
        public void EventStopListening_WithIEventScope_UnregistersFromScope()
        {
            var scope = new MockScope(EventScopeKey.ForScene(200));
            var listener = new MockListener();

            listener.EventStartListening<TestEvent>(scope);
            listener.EventStopListening<TestEvent>(scope);

            EventChannel<TestEvent>.Trigger(new TestEvent { Value = 77 }, scope);

            Assert.AreEqual(0, listener.ReceivedCount);
        }

        [Test]
        public void MultipleListeners_AllReceiveEvents()
        {
            var listener1 = new MockListener();
            var listener2 = new MockListener();
            var listener3 = new MockListener();

            listener1.EventStartListening<TestEvent>();
            listener2.EventStartListening<TestEvent>();
            listener3.EventStartListening<TestEvent>();

            EventChannel<TestEvent>.Trigger(new TestEvent { Value = 99 });

            Assert.AreEqual(1, listener1.ReceivedCount);
            Assert.AreEqual(1, listener2.ReceivedCount);
            Assert.AreEqual(1, listener3.ReceivedCount);
        }

        [Test]
        public void MixedScopes_ListenersReceiveCorrectEvents()
        {
            var scope1 = EventScopeKey.ForInstance(1);
            var scope2 = EventScopeKey.ForInstance(2);

            var globalListener = new MockListener();
            var scope1Listener = new MockListener();
            var scope2Listener = new MockListener();

            globalListener.EventStartListening<TestEvent>();
            scope1Listener.EventStartListening<TestEvent>(scope1);
            scope2Listener.EventStartListening<TestEvent>(scope2);

            // Trigger global
            EventChannel<TestEvent>.Trigger(new TestEvent { Value = 1 });
            Assert.AreEqual(1, globalListener.ReceivedCount);
            Assert.AreEqual(0, scope1Listener.ReceivedCount);
            Assert.AreEqual(0, scope2Listener.ReceivedCount);

            // Trigger scope1
            EventChannel<TestEvent>.Trigger(new TestEvent { Value = 2 }, scope1);
            Assert.AreEqual(1, globalListener.ReceivedCount);
            Assert.AreEqual(1, scope1Listener.ReceivedCount);
            Assert.AreEqual(0, scope2Listener.ReceivedCount);

            // Trigger scope2
            EventChannel<TestEvent>.Trigger(new TestEvent { Value = 3 }, scope2);
            Assert.AreEqual(1, globalListener.ReceivedCount);
            Assert.AreEqual(1, scope1Listener.ReceivedCount);
            Assert.AreEqual(1, scope2Listener.ReceivedCount);
        }

        private class MockListener : EventListener<TestEvent>
        {
            public int ReceivedCount { get; private set; }
            public int LastValue { get; private set; }

            public void OnEvent(TestEvent eventType)
            {
                ReceivedCount++;
                LastValue = eventType.Value;
            }
        }

        private class MockScope : IEventScope
        {
            public EventScopeKey ScopeKey { get; }
            public MockScope(EventScopeKey key) => ScopeKey = key;
        }
    }
}
