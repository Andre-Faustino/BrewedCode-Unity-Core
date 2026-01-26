using System;

namespace BrewedCode.Events
{
    /// <summary>
    /// Minimal event bus interface for pub/sub communication.
    /// Allows decoupled event publishing and subscription across all systems.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publishes an event to all subscribers.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to publish.</typeparam>
        /// <param name="evt">The event instance to publish.</param>
        void Publish<TEvent>(TEvent evt);

        /// <summary>
        /// Subscribes to events of a specific type.
        /// </summary>
        /// <typeparam name="TEvent">The type of events to subscribe to.</typeparam>
        /// <param name="handler">The callback to invoke when an event is published.</param>
        /// <returns>A disposable that unsubscribes the handler when disposed.</returns>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
    }
}
