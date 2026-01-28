namespace BrewedCode.VitalGauge
{
    /// <summary>
    /// Defines the state of a gauge based on its current value relative to its max and thresholds.
    /// </summary>
    public enum GaugeState
    {
        /// <summary>Normal state - gauge is between low threshold and max (excluding full).</summary>
        Normal,

        /// <summary>Low state - gauge is below or at the low threshold but not empty.</summary>
        Low,

        /// <summary>Empty state - gauge is at or near zero.</summary>
        Empty,

        /// <summary>Full state - gauge is at or near max.</summary>
        Full
    }
}
