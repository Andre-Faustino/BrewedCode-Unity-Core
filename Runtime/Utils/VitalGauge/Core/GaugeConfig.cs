namespace BrewedCode.VitalGauge
{
    /// <summary>
    /// Configuration data for initializing a VitalGauge instance.
    /// </summary>
    public class GaugeConfig
    {
        /// <summary>Unique identifier for the gauge (e.g., "oxygen", "stamina").</summary>
        public string Id { get; set; }

        /// <summary>Maximum value the gauge can reach.</summary>
        public float Max { get; set; }

        /// <summary>Starting value when the gauge is initialized.</summary>
        public float Start { get; set; }

        /// <summary>
        /// Rate of change per second.
        /// Positive values drain the gauge, negative values regenerate it.
        /// </summary>
        public float RatePerSecond { get; set; }

        /// <summary>
        /// [DEPRECATED] Use Thresholds array instead.
        /// Threshold (0-1) for determining the Low state.
        /// When Normalized (Current/Max) falls below this, the gauge enters Low state.
        /// If Thresholds array is empty, this value is used to auto-create a default threshold.
        /// </summary>
        /// [System.Obsolete("Use Thresholds array instead. This field is kept for backward compatibility.")]
        public float LowThreshold01 { get; set; } = 0.25f;

        /// <summary>
        /// Custom named thresholds for this gauge.
        /// Each threshold triggers GaugeThresholdCrossed events when crossed.
        /// If null or empty, a default threshold is created from LowThreshold01 for backward compatibility.
        /// </summary>
        public GaugeThreshold[] Thresholds { get; set; }
    }
}
