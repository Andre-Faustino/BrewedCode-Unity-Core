using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrewedCode.Events
{
#if UNITY_EDITOR
    /// <summary>
    /// Editor-only capture controller for event debug data.
    /// Toggleable to avoid overhead when not debugging.
    /// </summary>
    public static class EventDebugCapture
    {
        private const int MaxRecordCount = 500;

        /// <summary>
        /// When true, event dispatches are recorded. Disable for performance.
        /// </summary>
        public static bool IsCapturing { get; set; } = false;

        private static readonly List<EventDispatchRecord> s_Records = new(MaxRecordCount);
        private static int s_RecordIndex = 0;

        private static readonly Dictionary<ChannelKey, ChannelDebugInfo> s_ChannelInfos = new();
        private static readonly Dictionary<ChannelKey, List<WeakReference>> s_ListenerRefs = new();
        private static readonly Dictionary<EventScopeKey, string> s_ScopeNames = new();

        /// <summary>
        /// All known event types that have been dispatched. Used for multi-select filtering.
        /// </summary>
        private static readonly HashSet<Type> s_KnownEventTypes = new();

        /// <summary>
        /// Event types that are enabled for capture. If empty, all types are captured.
        /// </summary>
        private static readonly HashSet<Type> s_EnabledEventTypes = new();

        public struct ChannelDebugInfo
        {
            public Type EventType;
            public EventScopeKey ScopeKey;
            public string ScopeName;
            public int DispatchCount;
            public double LastDispatchTime;
        }

        /// <summary>
        /// Called when an event is raised. Records dispatch info if capturing.
        /// </summary>
        internal static void OnEventRaised<T>(Type eventType, T eventData, EventChannel<T> channel)
        {
            // Always track known event types
            s_KnownEventTypes.Add(eventType);

            if (!IsCapturing) return;

            // Check if this event type is enabled for capture
            if (s_EnabledEventTypes.Count > 0 && !s_EnabledEventTypes.Contains(eventType))
                return;

            var scopeKey = FindScopeKeyForChannel(eventType, channel);
            var listenerCount = channel.ListenerCount;

            // Serialize event data to JSON
            string eventDataJson;
            try
            {
                eventDataJson = JsonUtility.ToJson(eventData, true);
                if (string.IsNullOrEmpty(eventDataJson) || eventDataJson == "{}")
                {
                    // Try manual serialization for simple types
                    eventDataJson = SerializeEventData(eventData);
                }
            }
            catch
            {
                eventDataJson = SerializeEventData(eventData);
            }

            var record = new EventDispatchRecord(
                eventType: eventType,
                scopeKey: scopeKey,
                frameCount: Time.frameCount,
                timestamp: UnityEditor.EditorApplication.timeSinceStartup,
                listenersNotified: listenerCount,
                eventDataJson: eventDataJson
            );

            AddRecord(record);

            var key = new ChannelKey(eventType, scopeKey);
            if (s_ChannelInfos.TryGetValue(key, out var info))
            {
                info.DispatchCount++;
                info.LastDispatchTime = UnityEditor.EditorApplication.timeSinceStartup;
                s_ChannelInfos[key] = info;
            }
        }

        /// <summary>
        /// Manually serialize event data using reflection for types that JsonUtility can't handle.
        /// </summary>
        private static string SerializeEventData<T>(T eventData)
        {
            if (eventData == null) return "null";

            var type = typeof(T);
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (fields.Length == 0)
            {
                return $"{{ \"_type\": \"{type.Name}\" }}";
            }

            var parts = new List<string>();
            parts.Add($"\"_type\": \"{type.Name}\"");

            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(eventData);
                    var valueStr = FormatValue(value);
                    parts.Add($"\"{field.Name}\": {valueStr}");
                }
                catch
                {
                    parts.Add($"\"{field.Name}\": \"<error>\"");
                }
            }

            return "{\n  " + string.Join(",\n  ", parts) + "\n}";
        }

        private static string FormatValue(object? value)
        {
            if (value == null) return "null";

            return value switch
            {
                string s => $"\"{EscapeJson(s)}\"",
                bool b => b ? "true" : "false",
                int or long or float or double or decimal => value.ToString()!,
                Enum e => $"\"{e}\"",
                UnityEngine.Object obj => $"\"{obj.name}\" ({obj.GetType().Name})",
                Vector2 v2 => $"\"({v2.x:F2}, {v2.y:F2})\"",
                Vector3 v3 => $"\"({v3.x:F2}, {v3.y:F2}, {v3.z:F2})\"",
                _ => $"\"{value}\""
            };
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static EventScopeKey FindScopeKeyForChannel<T>(Type eventType, EventChannel<T> channel)
        {
            foreach (var kvp in s_ChannelInfos)
            {
                if (kvp.Key.EventType == eventType)
                {
                    return kvp.Key.ScopeKey;
                }
            }
            return EventScopeKey.Global;
        }

        internal static void OnChannelCreated(ChannelKey key)
        {
            s_KnownEventTypes.Add(key.EventType);

            var scopeName = s_ScopeNames.TryGetValue(key.ScopeKey, out var name)
                ? name
                : key.ScopeKey.ToString();

            s_ChannelInfos[key] = new ChannelDebugInfo
            {
                EventType = key.EventType,
                ScopeKey = key.ScopeKey,
                ScopeName = scopeName,
                DispatchCount = 0,
                LastDispatchTime = 0
            };
        }

        internal static void OnListenerAdded(Type eventType, EventScopeKey scopeKey, object listener)
        {
            s_KnownEventTypes.Add(eventType);

            var key = new ChannelKey(eventType, scopeKey);
            if (!s_ListenerRefs.TryGetValue(key, out var list))
            {
                list = new List<WeakReference>();
                s_ListenerRefs[key] = list;
            }
            list.Add(new WeakReference(listener));
        }

        internal static void OnListenerRemoved(Type eventType, EventScopeKey scopeKey, object listener)
        {
            var key = new ChannelKey(eventType, scopeKey);
            if (s_ListenerRefs.TryGetValue(key, out var list))
            {
                list.RemoveAll(wr => !wr.IsAlive || ReferenceEquals(wr.Target, listener));
            }
        }

        internal static void OnScopeCleared(EventScopeKey scopeKey)
        {
            var keysToRemove = s_ChannelInfos.Keys
                .Where(k => k.ScopeKey.Equals(scopeKey))
                .ToList();

            foreach (var key in keysToRemove)
            {
                s_ChannelInfos.Remove(key);
                s_ListenerRefs.Remove(key);
            }

            s_ScopeNames.Remove(scopeKey);
        }

        internal static void OnSceneScopeCreated(string sceneName, EventScopeKey scopeKey)
        {
            s_ScopeNames[scopeKey] = $"Scene:{sceneName}";
        }

        internal static void OnInstanceScopeCreated(string objectName, EventScopeKey scopeKey)
        {
            s_ScopeNames[scopeKey] = $"Instance:{objectName}";
        }

        private static void AddRecord(EventDispatchRecord record)
        {
            if (s_Records.Count < MaxRecordCount)
            {
                s_Records.Add(record);
            }
            else
            {
                s_Records[s_RecordIndex] = record;
                s_RecordIndex = (s_RecordIndex + 1) % MaxRecordCount;
            }
        }

        public static void ClearRecords()
        {
            s_Records.Clear();
            s_RecordIndex = 0;
        }

        public static void ClearAll()
        {
            s_Records.Clear();
            s_RecordIndex = 0;
            s_ChannelInfos.Clear();
            s_ListenerRefs.Clear();
            s_ScopeNames.Clear();
            // Don't clear known event types - they're useful to keep
        }

        public static IReadOnlyList<EventDispatchRecord> GetRecords() => s_Records;

        public static IReadOnlyDictionary<ChannelKey, ChannelDebugInfo> GetChannelInfos() => s_ChannelInfos;

        public static int GetListenerCount(ChannelKey key)
        {
            if (!s_ListenerRefs.TryGetValue(key, out var list)) return 0;
            list.RemoveAll(wr => !wr.IsAlive);
            return list.Count;
        }

        public static string GetScopeName(EventScopeKey scopeKey)
        {
            return s_ScopeNames.TryGetValue(scopeKey, out var name) ? name : scopeKey.ToString();
        }

        /// <summary>
        /// Returns all known event types that have been dispatched or registered.
        /// </summary>
        public static IReadOnlyCollection<Type> GetKnownEventTypes() => s_KnownEventTypes;

        /// <summary>
        /// Returns the set of event types currently enabled for capture.
        /// Empty means all types are captured.
        /// </summary>
        public static IReadOnlyCollection<Type> GetEnabledEventTypes() => s_EnabledEventTypes;

        /// <summary>
        /// Sets which event types should be captured. Pass empty to capture all.
        /// </summary>
        public static void SetEnabledEventTypes(IEnumerable<Type> types)
        {
            s_EnabledEventTypes.Clear();
            foreach (var type in types)
            {
                s_EnabledEventTypes.Add(type);
            }
        }

        /// <summary>
        /// Enables capture for a specific event type.
        /// </summary>
        public static void EnableEventType(Type type)
        {
            s_EnabledEventTypes.Add(type);
        }

        /// <summary>
        /// Disables capture for a specific event type.
        /// </summary>
        public static void DisableEventType(Type type)
        {
            s_EnabledEventTypes.Remove(type);
        }

        /// <summary>
        /// Clears the enabled event types filter (captures all).
        /// </summary>
        public static void ClearEventTypeFilter()
        {
            s_EnabledEventTypes.Clear();
        }

        /// <summary>
        /// Checks if a specific event type is enabled for capture.
        /// Returns true if no filter is set (all enabled) or if the type is in the filter.
        /// </summary>
        public static bool IsEventTypeEnabled(Type type)
        {
            return s_EnabledEventTypes.Count == 0 || s_EnabledEventTypes.Contains(type);
        }
    }
#endif
}
