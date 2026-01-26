using NUnit.Framework;

namespace BrewedCode.Events.Tests
{
    [TestFixture]
    public class EventChannelRegistryTests
    {
        private struct EventA { public int Value; }
        private struct EventB { public string Name; }

        [SetUp]
        public void SetUp()
        {
            EventChannelRegistry.ClearAll();
        }

        [Test]
        public void Get_GlobalScope_ReturnsSameChannel()
        {
            EventChannelRegistry.Get(out EventChannel<EventA> channel1);
            EventChannelRegistry.Get(out EventChannel<EventA> channel2);

            Assert.AreSame(channel1, channel2);
        }

        [Test]
        public void Get_DifferentTypes_ReturnsDifferentChannels()
        {
            EventChannelRegistry.Get(out EventChannel<EventA> channelA);
            EventChannelRegistry.Get(out EventChannel<EventB> channelB);

            Assert.AreNotSame(channelA, channelB);
        }

        [Test]
        public void Get_ScopedChannel_ReturnsSameForSameScope()
        {
            var scopeKey = EventScopeKey.ForScene(100);

            EventChannelRegistry.Get(scopeKey, out EventChannel<EventA> channel1);
            EventChannelRegistry.Get(scopeKey, out EventChannel<EventA> channel2);

            Assert.AreSame(channel1, channel2);
        }

        [Test]
        public void Get_DifferentScopes_ReturnsDifferentChannels()
        {
            var scope1 = EventScopeKey.ForScene(100);
            var scope2 = EventScopeKey.ForScene(200);

            EventChannelRegistry.Get(scope1, out EventChannel<EventA> channel1);
            EventChannelRegistry.Get(scope2, out EventChannel<EventA> channel2);

            Assert.AreNotSame(channel1, channel2);
        }

        [Test]
        public void Get_GlobalVsScoped_ReturnsDifferentChannels()
        {
            var scopeKey = EventScopeKey.ForInstance(500);

            EventChannelRegistry.Get(out EventChannel<EventA> globalChannel);
            EventChannelRegistry.Get(scopeKey, out EventChannel<EventA> scopedChannel);

            Assert.AreNotSame(globalChannel, scopedChannel);
        }

        [Test]
        public void Get_WithCallback_InvokesCallback()
        {
            bool callbackInvoked = false;
            EventChannel<EventA>? receivedChannel = null;

            EventChannelRegistry.Get(out EventChannel<EventA> channel, c =>
            {
                callbackInvoked = true;
                receivedChannel = c;
            });

            Assert.IsTrue(callbackInvoked);
            Assert.AreSame(channel, receivedChannel);
        }

        [Test]
        public void Get_WithIEventScope_UsesCorrectScopeKey()
        {
            var scope = new MockEventScope(EventScopeKey.ForInstance(777));

            EventChannelRegistry.Get(scope, out EventChannel<EventA> channel1);
            EventChannelRegistry.Get(scope.ScopeKey, out EventChannel<EventA> channel2);

            Assert.AreSame(channel1, channel2);
        }

        [Test]
        public void AddListener_RegistersListener()
        {
            var listener = new MockListener<EventA>();
            EventChannelRegistry.AddListener(listener);

            EventChannelRegistry.Get(out EventChannel<EventA> channel);
            channel.RaiseEvent(new EventA { Value = 42 });

            Assert.AreEqual(1, listener.ReceivedEvents.Count);
            Assert.AreEqual(42, listener.ReceivedEvents[0].Value);
        }

        [Test]
        public void AddListener_Scoped_RegistersOnCorrectChannel()
        {
            var scopeKey = EventScopeKey.ForScene(100);
            var globalListener = new MockListener<EventA>();
            var scopedListener = new MockListener<EventA>();

            EventChannelRegistry.AddListener(globalListener);
            EventChannelRegistry.AddListener(scopeKey, scopedListener);

            // Raise on global
            EventChannelRegistry.Get(out EventChannel<EventA> globalChannel);
            globalChannel.RaiseEvent(new EventA { Value = 1 });

            // Raise on scoped
            EventChannelRegistry.Get(scopeKey, out EventChannel<EventA> scopedChannel);
            scopedChannel.RaiseEvent(new EventA { Value = 2 });

            Assert.AreEqual(1, globalListener.ReceivedEvents.Count);
            Assert.AreEqual(1, globalListener.ReceivedEvents[0].Value);

            Assert.AreEqual(1, scopedListener.ReceivedEvents.Count);
            Assert.AreEqual(2, scopedListener.ReceivedEvents[0].Value);
        }

        [Test]
        public void RemoveListener_UnregistersListener()
        {
            var listener = new MockListener<EventA>();
            EventChannelRegistry.AddListener(listener);
            EventChannelRegistry.RemoveListener(listener);

            EventChannelRegistry.Get(out EventChannel<EventA> channel);
            channel.RaiseEvent(new EventA { Value = 42 });

            Assert.AreEqual(0, listener.ReceivedEvents.Count);
        }

        [Test]
        public void RemoveListener_Scoped_UnregistersFromCorrectChannel()
        {
            var scopeKey = EventScopeKey.ForScene(100);
            var listener = new MockListener<EventA>();

            EventChannelRegistry.AddListener(scopeKey, listener);
            EventChannelRegistry.RemoveListener(scopeKey, listener);

            EventChannelRegistry.Get(scopeKey, out EventChannel<EventA> channel);
            channel.RaiseEvent(new EventA { Value = 42 });

            Assert.AreEqual(0, listener.ReceivedEvents.Count);
        }

        [Test]
        public void ClearScope_RemovesChannelsForScope()
        {
            var scopeKey = EventScopeKey.ForScene(100);

            EventChannelRegistry.Get(scopeKey, out EventChannel<EventA> channel1);
            var listener = new MockListener<EventA>();
            channel1.OnEventRaised += listener.OnEvent;
            channel1.RaiseEvent(new EventA { Value = 1 });
            Assert.AreEqual(1, listener.ReceivedEvents.Count);

            EventChannelRegistry.ClearScope(scopeKey);

            // Getting channel again should return a new instance
            EventChannelRegistry.Get(scopeKey, out EventChannel<EventA> channel2);
            Assert.AreNotSame(channel1, channel2);

            // Old listener should not receive events on new channel
            channel2.RaiseEvent(new EventA { Value = 2 });
            Assert.AreEqual(1, listener.ReceivedEvents.Count);
        }

        [Test]
        public void ClearAll_RemovesAllChannels()
        {
            EventChannelRegistry.Get(out EventChannel<EventA> globalChannel1);
            EventChannelRegistry.Get(EventScopeKey.ForScene(1), out EventChannel<EventA> scopedChannel1);

            EventChannelRegistry.ClearAll();

            EventChannelRegistry.Get(out EventChannel<EventA> globalChannel2);
            EventChannelRegistry.Get(EventScopeKey.ForScene(1), out EventChannel<EventA> scopedChannel2);

            Assert.AreNotSame(globalChannel1, globalChannel2);
            Assert.AreNotSame(scopedChannel1, scopedChannel2);
        }

        [Test]
        public void GetActiveChannelKeys_ReturnsRegisteredKeys()
        {
            EventChannelRegistry.Get(out EventChannel<EventA> _);
            EventChannelRegistry.Get(EventScopeKey.ForScene(100), out EventChannel<EventB> _);

            var keys = EventChannelRegistry.GetActiveChannelKeys();

            Assert.AreEqual(2, keys.Count);
        }

        [Test]
        public void GetListenerCount_ReturnsCorrectCount()
        {
            var scopeKey = EventScopeKey.ForInstance(500);
            var key = new ChannelKey(typeof(EventA), scopeKey);

            Assert.AreEqual(0, EventChannelRegistry.GetListenerCount(key));

            var listener1 = new MockListener<EventA>();
            var listener2 = new MockListener<EventA>();

            EventChannelRegistry.AddListener(scopeKey, listener1);
            Assert.AreEqual(1, EventChannelRegistry.GetListenerCount(key));

            EventChannelRegistry.AddListener(scopeKey, listener2);
            Assert.AreEqual(2, EventChannelRegistry.GetListenerCount(key));
        }

        private class MockEventScope : IEventScope
        {
            public EventScopeKey ScopeKey { get; }
            public MockEventScope(EventScopeKey scopeKey) => ScopeKey = scopeKey;
        }

        private class MockListener<T> : EventListener<T>
        {
            public System.Collections.Generic.List<T> ReceivedEvents { get; } = new();

            public void OnEvent(T eventType)
            {
                ReceivedEvents.Add(eventType);
            }
        }
    }
}
