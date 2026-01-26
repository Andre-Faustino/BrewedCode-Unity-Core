using System;

namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Strong-typed ID for timers backed by a Guid.
    ///
    /// Ensures type safety and prevents accidental ID misuse.
    /// Immutable value object with equality comparison.
    /// </summary>
    public readonly struct TimerId : IEquatable<TimerId>
    {
        private readonly Guid _value;

        /// <summary>Creates a new TimerId with a random Guid.</summary>
        public static TimerId New() => new(Guid.NewGuid());

        /// <summary>Creates a TimerId from an existing Guid.</summary>
        public static TimerId FromGuid(Guid guid) => new(guid);

        private TimerId(Guid value) => _value = value;

        /// <summary>Gets the underlying Guid value.</summary>
        public Guid Value => _value;

        public override bool Equals(object? obj) => obj is TimerId id && Equals(id);
        public bool Equals(TimerId other) => _value.Equals(other._value);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => _value.ToString();

        public static bool operator ==(TimerId left, TimerId right) => left.Equals(right);
        public static bool operator !=(TimerId left, TimerId right) => !left.Equals(right);
    }
}
