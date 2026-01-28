using System;

namespace BrewedCode.VitalGauge
{
    /// <summary>
    /// Defines a named threshold for a VitalGauge.
    /// Thresholds trigger GaugeThresholdCrossed events when the gauge crosses them.
    /// </summary>
    public readonly struct GaugeThreshold
    {
        /// <summary>Unique name for this threshold (e.g., "Warning", "Critical", "Danger").</summary>
        public readonly string Name;

        /// <summary>
        /// Threshold value (0-1, normalized).
        /// Threshold crossing occurs when Normalized <= Value.
        /// </summary>
        public readonly float Value;

        /// <summary>
        /// Priority for ordering (higher = more important).
        /// Used to determine which threshold controls the Low state when multiple thresholds are crossed.
        /// </summary>
        public readonly int Priority;

        /// <summary>
        /// Optional color hint for UI/debug visualization (hex format "#RRGGBB").
        /// Empty string if not specified.
        /// </summary>
        public readonly string ColorHex;

        /// <summary>
        /// Creates a new gauge threshold with the given parameters.
        /// Value is clamped to [0, 1].
        /// </summary>
        public GaugeThreshold(string name, float value, int priority, string colorHex = "")
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = Clamp01(value);
            Priority = priority;
            ColorHex = colorHex ?? "";
        }

        /// <summary>Clamps a value between 0 and 1.</summary>
        private static float Clamp01(float value)
        {
            if (value < 0) return 0;
            if (value > 1) return 1;
            return value;
        }

        /// <summary>Returns a string representation of this threshold.</summary>
        public override string ToString() => $"{Name} @ {Value:F2} (Priority {Priority})";
    }
}
