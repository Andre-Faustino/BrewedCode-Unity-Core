using System;

namespace BrewedCode.ItemHub
{
    /// <summary>
    /// Value object that identifies an item definition (e.g., "atom.fe", "material.steel").
    /// Kept as a lightweight, immutable wrapper around a string to avoid accidental string usage.
    /// </summary>
    public readonly struct ItemId : IEquatable<ItemId>
    {
        public string Value { get; }

        public ItemId(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static implicit operator string(ItemId id) => id.Value;
        public static implicit operator ItemId(string value) => new ItemId(value);

        public bool Equals(ItemId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ItemId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;
    }

    /// <summary>
    /// Value object that identifies a unique, runtime item instance (non-stackable, mutable).
    /// </summary>
    public readonly struct InstanceId : IEquatable<InstanceId>
    {
        public string Value { get; }

        public InstanceId(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Generates a new unique identifier (GUID-based) for new instances.
        /// </summary>
        public static InstanceId New() => new InstanceId(Guid.NewGuid().ToString("N"));

        public static implicit operator string(InstanceId id) => id.Value;
        public static implicit operator InstanceId(string value) => new InstanceId(value);

        public bool Equals(InstanceId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is InstanceId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;
    }
}