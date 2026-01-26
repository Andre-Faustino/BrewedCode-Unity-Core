using System;
using System.Collections.Generic;
using BrewedCode.Events;

namespace BrewedCode.Logging.Tests
{
    internal sealed class MockEventBus : IEventBus
    {
        public List<object> PublishedEvents { get; } = new();

        public void Publish<TEvent>(TEvent evt)
        {
            PublishedEvents.Add(evt!);
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            return new MockSubscription();
        }

        private sealed class MockSubscription : IDisposable
        {
            public void Dispose() { }
        }
    }
}
