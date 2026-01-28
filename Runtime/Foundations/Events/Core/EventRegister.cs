namespace BrewedCode.Events
{
    /// <summary>
    /// Extension methods for easy event subscription.
    /// </summary>
    public static class EventRegister
    {
        #region Backward Compatible API (Global Scope)

        /// <summary>
        /// Starts listening to events on the global scope.
        /// Call this in OnEnable.
        /// </summary>
        public static void EventStartListening<T>(this EventListener<T> caller)
        {
            EventChannelRegistry.AddListener(caller);
        }

        /// <summary>
        /// Stops listening to events on the global scope.
        /// Call this in OnDisable.
        /// </summary>
        public static void EventStopListening<T>(this EventListener<T> caller)
        {
            EventChannelRegistry.RemoveListener(caller);
        }

        #endregion

        #region Scoped API (EventScopeKey)

        /// <summary>
        /// Starts listening to events on the specified scope.
        /// </summary>
        public static void EventStartListening<T>(this EventListener<T> caller, EventScopeKey scopeKey)
        {
            EventChannelRegistry.AddListener(scopeKey, caller);
        }

        /// <summary>
        /// Stops listening to events on the specified scope.
        /// </summary>
        public static void EventStopListening<T>(this EventListener<T> caller, EventScopeKey scopeKey)
        {
            EventChannelRegistry.RemoveListener(scopeKey, caller);
        }

        #endregion

        #region Convenience API (IEventScope)

        /// <summary>
        /// Starts listening to events from the specified emitter.
        /// </summary>
        public static void EventStartListening<T>(this EventListener<T> caller, IEventScope scope)
        {
            EventChannelRegistry.AddListener(scope.ScopeKey, caller);
        }

        /// <summary>
        /// Stops listening to events from the specified emitter.
        /// </summary>
        public static void EventStopListening<T>(this EventListener<T> caller, IEventScope scope)
        {
            EventChannelRegistry.RemoveListener(scope.ScopeKey, caller);
        }

        #endregion
    }

    /// <summary>
    /// Interface for event listeners.
    /// Implement this on MonoBehaviours that should receive events.
    /// </summary>
    public interface EventListener<T>
    {
        void OnEvent(T eventType);
    }
}
