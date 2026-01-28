namespace BrewedCode.VitalGauge
{
    /// <summary>
    /// Event published whenever a gauge's current value or max value changes.
    /// This event is published on every value update, not just state transitions.
    /// </summary>
    public readonly struct GaugeChanged
    {
        /// <summary>Unique identifier of the gauge.</summary>
        public readonly string Id;

        /// <summary>Current value of the gauge.</summary>
        public readonly float Current;

        /// <summary>Maximum value of the gauge.</summary>
        public readonly float Max;

        /// <summary>Normalized value (Current / Max), clamped to [0, 1].</summary>
        public readonly float Normalized;

        public GaugeChanged(string id, float current, float max, float normalized)
        {
            Id = id;
            Current = current;
            Max = max;
            Normalized = normalized;
        }
    }
}
