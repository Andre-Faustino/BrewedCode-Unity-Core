using System;

namespace BrewedCode.Events
{
    /// <summary>
    /// Composite key for channel lookup: combines event type with scope.
    /// Used as dictionary key in EventChannelRegistry.
    /// </summary>
    public readonly struct ChannelKey : IEquatable<ChannelKey>
    {
        public readonly Type EventType;
        public readonly EventScopeKey ScopeKey;

        public ChannelKey(Type eventType, EventScopeKey scopeKey)
        {
            EventType = eventType;
            ScopeKey = scopeKey;
        }

        public bool Equals(ChannelKey other)
        {
            return EventType == other.EventType && ScopeKey.Equals(other.ScopeKey);
        }

        public override bool Equals(object? obj)
        {
            return obj is ChannelKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EventType, ScopeKey);
        }

        public static bool operator ==(ChannelKey left, ChannelKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChannelKey left, ChannelKey right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{EventType.Name}@{ScopeKey}";
        }
    }
}
