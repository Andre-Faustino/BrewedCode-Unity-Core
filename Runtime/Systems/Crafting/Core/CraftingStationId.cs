using System;

namespace BrewedCode.Crafting
{
    /// <summary>
    /// Value object representing the unique identifier of a crafting station.
    /// Provides strong typing and prevents ID confusion.
    /// </summary>
    public readonly struct CraftingStationId : IEquatable<CraftingStationId>
    {
        private readonly Guid _value;

        /// <summary>
        /// Creates a new unique crafting station ID.
        /// </summary>
        public static CraftingStationId New() => new(Guid.NewGuid());

        /// <summary>
        /// Creates a crafting station ID from an existing GUID.
        /// Useful for serialization/deserialization.
        /// </summary>
        public static CraftingStationId FromGuid(Guid guid) => new(guid);

        private CraftingStationId(Guid value) => _value = value;

        /// <summary>
        /// Returns the underlying GUID value.
        /// </summary>
        public Guid Value => _value;

        public override bool Equals(object? obj) => obj is CraftingStationId id && Equals(id);

        public bool Equals(CraftingStationId other) => _value.Equals(other._value);

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => _value.ToString();

        public static bool operator ==(CraftingStationId left, CraftingStationId right) => left.Equals(right);

        public static bool operator !=(CraftingStationId left, CraftingStationId right) => !left.Equals(right);
    }
}
