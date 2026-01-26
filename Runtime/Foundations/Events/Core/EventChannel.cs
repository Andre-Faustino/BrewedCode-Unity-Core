using System;
using UnityEngine.Events;

namespace BrewedCode.Events
{
    /// <summary>
    /// Generic event channel that handles event dispatch.
    /// Scope-agnostic - all scoping logic lives in EventChannelRegistry.
    /// </summary>
    public class EventChannel<T> : IEventChannel
    {
        public UnityAction<T>? OnEventRaised;

#if UNITY_EDITOR
        /// <summary>
        /// Number of active listeners. Editor-only for debug window.
        /// </summary>
        public int ListenerCount => OnEventRaised?.GetInvocationList().Length ?? 0;
#endif

        /// <summary>
        /// Raises the event to all subscribed listeners.
        /// </summary>
        public void RaiseEvent(T eventData)
        {
#if UNITY_EDITOR
            EventDebugCapture.OnEventRaised(typeof(T), eventData, this);
#endif
            OnEventRaised?.Invoke(eventData);
        }

        /// <summary>
        /// Triggers event on Global scope. Maintains backward compatibility.
        /// </summary>
        public static void Trigger(T eventData)
        {
            EventChannelRegistry.Get(out EventChannel<T> channel);
            channel.RaiseEvent(eventData);
        }

        /// <summary>
        /// Triggers event on specified scope key.
        /// </summary>
        public static void Trigger(T eventData, EventScopeKey scopeKey)
        {
            EventChannelRegistry.Get(scopeKey, out EventChannel<T> channel);
            channel.RaiseEvent(eventData);
        }

        /// <summary>
        /// Triggers event on the scope of the given IEventScope.
        /// </summary>
        public static void Trigger(T eventData, IEventScope scope)
        {
            Trigger(eventData, scope.ScopeKey);
        }
    }
}
