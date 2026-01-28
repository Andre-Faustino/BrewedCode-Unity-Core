using System;

namespace BrewedCode.Logging
{
    /// <summary>
    /// Strong-typed channel identifier.
    /// Represents a functional domain or subsystem (Crafting, UI, Save, etc).
    /// Immutable value object with equality comparison.
    /// </summary>
    public readonly struct LogChannel : IEquatable<LogChannel>
    {
        private readonly string _name;

        // Predefined channels for core systems
        public static readonly LogChannel System = new("System");
        public static readonly LogChannel Crafting = new("Crafting");
        public static readonly LogChannel Inventory = new("Inventory");
        public static readonly LogChannel Timer = new("Timer");
        public static readonly LogChannel Save = new("Save");
        public static readonly LogChannel AI = new("AI");
        public static readonly LogChannel UI = new("UI");
        public static readonly LogChannel Audio = new("Audio");
        public static readonly LogChannel Network = new("Network");
        public static readonly LogChannel Default = new("Default");

        /// <summary>Creates a custom channel with the given name.</summary>
        public static LogChannel Custom(string name) => new(name);

        private LogChannel(string name) => _name = name ?? "Unknown";

        public string Name => _name;

        public override bool Equals(object? obj) => obj is LogChannel ch && Equals(ch);

        public bool Equals(LogChannel other) =>
            string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() =>
            _name?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;

        public override string ToString() => _name;

        public static bool operator ==(LogChannel left, LogChannel right) => left.Equals(right);
        public static bool operator !=(LogChannel left, LogChannel right) => !left.Equals(right);
    }
}
