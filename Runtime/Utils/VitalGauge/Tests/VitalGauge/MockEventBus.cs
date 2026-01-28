using System;
using System.Collections.Generic;
using BrewedCode.Events;

namespace BrewedCode.VitalGauge.Tests
{
    /// <summary>
    /// Mock event bus for testing that captures all published events.
    /// Allows verification of event publishing behavior without external dependencies.
    /// </summary>
    public class MockEventBus : IEventBus
    {
        /// <summary>List of all events published, in order.</summary>
        public List<object> PublishedEvents { get; } = new();

        /// <summary>Dictionary mapping event types to subscription counts.</summary>
        private readonly Dictionary<Type, int> _subscriptionCounts = new();

        /// <summary>Publishes an event and records it.</summary>
        public void Publish<TEvent>(TEvent evt)
        {
            PublishedEvents.Add(evt);
        }

        /// <summary>
        /// Subscribes to events (no-op for testing, but implements interface).
        /// </summary>
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);
            if (!_subscriptionCounts.ContainsKey(eventType))
                _subscriptionCounts[eventType] = 0;
            _subscriptionCounts[eventType]++;

            return new UnsubscribeToken(() =>
            {
                _subscriptionCounts[eventType]--;
            });
        }

        /// <summary>Gets all events of a specific type.</summary>
        public List<T> GetEventsOfType<T>() where T : struct
        {
            var result = new List<T>();
            foreach (var evt in PublishedEvents)
            {
                if (evt is T typedEvent)
                    result.Add(typedEvent);
            }
            return result;
        }

        /// <summary>Clears all recorded events.</summary>
        public void Clear() => PublishedEvents.Clear();

        /// <summary>Gets the count of events of a specific type.</summary>
        public int GetEventCount<T>() where T : struct => GetEventsOfType<T>().Count;

        private class UnsubscribeToken : IDisposable
        {
            private readonly Action _onDispose;
            public UnsubscribeToken(Action onDispose) => _onDispose = onDispose;
            public void Dispose() => _onDispose?.Invoke();
        }
    }
}
