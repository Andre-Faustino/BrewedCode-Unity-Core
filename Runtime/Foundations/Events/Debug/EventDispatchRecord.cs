using System;

namespace BrewedCode.Events
{
#if UNITY_EDITOR
    /// <summary>
    /// Records a single event dispatch for debug inspection.
    /// Stored in a circular buffer by EventDebugCapture.
    /// </summary>
    public readonly struct EventDispatchRecord
    {
        public readonly Type EventType;
        public readonly EventScopeKey ScopeKey;
        public readonly int FrameCount;
        public readonly double Timestamp;
        public readonly int ListenersNotified;
        public readonly string SenderName;

        /// <summary>
        /// JSON representation of the event data for inspection.
        /// </summary>
        public readonly string EventDataJson;

        public EventDispatchRecord(
            Type eventType,
            EventScopeKey scopeKey,
            int frameCount,
            double timestamp,
            int listenersNotified,
            string? senderName = null,
            string? eventDataJson = null)
        {
            EventType = eventType;
            ScopeKey = scopeKey;
            FrameCount = frameCount;
            Timestamp = timestamp;
            ListenersNotified = listenersNotified;
            SenderName = senderName ?? string.Empty;
            EventDataJson = eventDataJson ?? "{}";
        }
    }
#endif
}
