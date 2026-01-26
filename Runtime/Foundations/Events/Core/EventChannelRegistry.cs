using System;
using System.Collections.Generic;

namespace BrewedCode.Events
{
    /// <summary>
    /// Central registry for event channels. Manages channel creation and caching.
    /// Supports both global (backward compatible) and scoped channels.
    /// </summary>
    public static class EventChannelRegistry
    {
        private static readonly Dictionary<ChannelKey, object> s_Cache = new();
        private static readonly HashSet<ChannelKey> s_ActiveKeys = new();
        private static readonly object s_Lock = new();

        #region Backward Compatible API (Global Scope)

        /// <summary>
        /// Gets or creates a global-scoped channel.
        /// Maintains backward compatibility with existing code.
        /// </summary>
        public static void Get<T>(out EventChannel<T> eventChannel, Action<EventChannel<T>>? callback = null)
        {
            Get(EventScopeKey.Global, out eventChannel, callback);
        }

        /// <summary>
        /// Adds a listener to the global-scoped channel.
        /// </summary>
        public static void AddListener<T>(EventListener<T> caller)
        {
            AddListener(EventScopeKey.Global, caller);
        }

        /// <summary>
        /// Removes a listener from the global-scoped channel.
        /// </summary>
        public static void RemoveListener<T>(EventListener<T> caller)
        {
            RemoveListener(EventScopeKey.Global, caller);
        }

        #endregion

        #region Scoped API (EventScopeKey)

        /// <summary>
        /// Gets or creates a scoped channel.
        /// </summary>
        public static void Get<T>(EventScopeKey scopeKey, out EventChannel<T> eventChannel, Action<EventChannel<T>>? callback = null)
        {
            var key = new ChannelKey(typeof(T), scopeKey);

            lock (s_Lock)
            {
                if (s_Cache.TryGetValue(key, out var cached))
                {
                    eventChannel = cached as EventChannel<T>
                        ?? throw new InvalidOperationException($"Channel type mismatch for {typeof(T).Name}");
                    callback?.Invoke(eventChannel);
                    return;
                }

                eventChannel = new EventChannel<T>();
                s_Cache[key] = eventChannel;
                s_ActiveKeys.Add(key);

#if UNITY_EDITOR
                EventDebugCapture.OnChannelCreated(key);
#endif
            }

            callback?.Invoke(eventChannel);
        }

        /// <summary>
        /// Adds a listener to a scoped channel.
        /// </summary>
        public static void AddListener<T>(EventScopeKey scopeKey, EventListener<T> caller)
        {
            Get(scopeKey, out EventChannel<T> channel);
            channel.OnEventRaised += caller.OnEvent;

#if UNITY_EDITOR
            EventDebugCapture.OnListenerAdded(typeof(T), scopeKey, caller);
#endif
        }

        /// <summary>
        /// Removes a listener from a scoped channel.
        /// </summary>
        public static void RemoveListener<T>(EventScopeKey scopeKey, EventListener<T> caller)
        {
            Get(scopeKey, out EventChannel<T> channel);
            channel.OnEventRaised -= caller.OnEvent;

#if UNITY_EDITOR
            EventDebugCapture.OnListenerRemoved(typeof(T), scopeKey, caller);
#endif
        }

        #endregion

        #region Convenience API (IEventScope)

        /// <summary>
        /// Gets or creates a channel for the given scope.
        /// </summary>
        public static void Get<T>(IEventScope scope, out EventChannel<T> eventChannel, Action<EventChannel<T>>? callback = null)
        {
            Get(scope.ScopeKey, out eventChannel, callback);
        }

        /// <summary>
        /// Adds a listener to the channel for the given scope.
        /// </summary>
        public static void AddListener<T>(IEventScope scope, EventListener<T> caller)
        {
            AddListener(scope.ScopeKey, caller);
        }

        /// <summary>
        /// Removes a listener from the channel for the given scope.
        /// </summary>
        public static void RemoveListener<T>(IEventScope scope, EventListener<T> caller)
        {
            RemoveListener(scope.ScopeKey, caller);
        }

        #endregion

        #region Scope Cleanup

        /// <summary>
        /// Removes all channels for a given scope.
        /// Called by SceneScopeProvider on scene unload.
        /// </summary>
        internal static void ClearScope(EventScopeKey scopeKey)
        {
            lock (s_Lock)
            {
                var keysToRemove = new List<ChannelKey>();

                foreach (var key in s_ActiveKeys)
                {
                    if (key.ScopeKey.Equals(scopeKey))
                    {
                        keysToRemove.Add(key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    s_Cache.Remove(key);
                    s_ActiveKeys.Remove(key);
                }

#if UNITY_EDITOR
                EventDebugCapture.OnScopeCleared(scopeKey);
#endif
            }
        }

        /// <summary>
        /// Clears all cached channels. Use with caution.
        /// Called on domain reload.
        /// </summary>
        internal static void ClearAll()
        {
            lock (s_Lock)
            {
                s_Cache.Clear();
                s_ActiveKeys.Clear();

#if UNITY_EDITOR
                EventDebugCapture.ClearAll();
#endif
            }
        }

        #endregion

        #region Debug Access

#if UNITY_EDITOR
        /// <summary>
        /// Returns all active channel keys. Editor-only for debug window.
        /// </summary>
        public static IReadOnlyCollection<ChannelKey> GetActiveChannelKeys() => s_ActiveKeys;

        /// <summary>
        /// Gets the listener count for a channel. Editor-only.
        /// </summary>
        public static int GetListenerCount<T>(EventScopeKey scopeKey)
        {
            var key = new ChannelKey(typeof(T), scopeKey);
            if (!s_Cache.TryGetValue(key, out var cached)) return 0;

            var channel = cached as EventChannel<T>;
            return channel?.ListenerCount ?? 0;
        }

        /// <summary>
        /// Gets listener count for a channel key. Editor-only.
        /// </summary>
        public static int GetListenerCount(ChannelKey key)
        {
            if (!s_Cache.TryGetValue(key, out var cached)) return 0;

            if (cached is IEventChannel eventChannel)
            {
                return eventChannel.ListenerCount;
            }
            return 0;
        }
#endif

        #endregion
    }
}
