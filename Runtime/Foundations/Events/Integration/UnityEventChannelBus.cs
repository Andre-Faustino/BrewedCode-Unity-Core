using System;

namespace BrewedCode.Events
{
    /// <summary>
    /// IEventBus implementation that bridges events to the EventChannel&lt;T&gt; system.
    /// Provides centralized pub/sub functionality across all systems.
    /// </summary>
    public sealed class UnityEventChannelBus : IEventBus
    {
        public void Publish<TEvent>(TEvent evt)
        {
            EventChannel<TEvent>.Trigger(evt);
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var listener = new ActionEventListener<TEvent>(handler);
            listener.EventStartListening();
            return new Subscription(() => listener.EventStopListening());
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Action _onDispose;
            private bool _disposed;

            public Subscription(Action onDispose) => _onDispose = onDispose;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _onDispose?.Invoke();
            }
        }

        private sealed class ActionEventListener<T> : EventListener<T>
        {
            private readonly Action<T> _onEvent;

            public ActionEventListener(Action<T> onEvent) => _onEvent = onEvent;

            public void OnEvent(T eventType) => _onEvent?.Invoke(eventType);

            public void EventStartListening() => this.EventStartListening<T>();

            public void EventStopListening() => this.EventStopListening<T>();
        }
    }
}
