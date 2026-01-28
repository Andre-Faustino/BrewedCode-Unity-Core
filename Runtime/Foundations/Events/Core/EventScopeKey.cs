using System;

namespace BrewedCode.Events
{
    /// <summary>
    /// Defines the type of event scope.
    /// </summary>
    public enum EventScopeType : byte
    {
        Global = 0,
        Scene = 1,
        Instance = 2
    }

    /// <summary>
    /// Lightweight, immutable value type identifying an event scope.
    /// Used as part of composite dictionary keys for channel lookup.
    /// </summary>
    public readonly struct EventScopeKey : IEquatable<EventScopeKey>
    {
        private readonly int _id;
        private readonly EventScopeType _type;

        /// <summary>
        /// Well-known Global scope (id = 0, type = Global).
        /// Use this for broadcast events that reach all listeners.
        /// </summary>
        public static readonly EventScopeKey Global = new(0, EventScopeType.Global);

        public EventScopeType Type => _type;
        public int Id => _id;
        public bool IsGlobal => _type == EventScopeType.Global;

        private EventScopeKey(int id, EventScopeType type)
        {
            _id = id;
            _type = type;
        }

        /// <summary>
        /// Creates a scope key for a scene using its handle.
        /// </summary>
        public static EventScopeKey ForScene(int sceneHandle)
        {
            return new EventScopeKey(sceneHandle, EventScopeType.Scene);
        }

        /// <summary>
        /// Creates a scope key for an instance using its Unity instance ID.
        /// </summary>
        public static EventScopeKey ForInstance(int instanceId)
        {
            return new EventScopeKey(instanceId, EventScopeType.Instance);
        }

        public bool Equals(EventScopeKey other)
        {
            return _id == other._id && _type == other._type;
        }

        public override bool Equals(object? obj)
        {
            return obj is EventScopeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_id, (int)_type);
        }

        public static bool operator ==(EventScopeKey left, EventScopeKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EventScopeKey left, EventScopeKey right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return _type == EventScopeType.Global ? "Global" : $"{_type}:{_id}";
        }
    }
}
