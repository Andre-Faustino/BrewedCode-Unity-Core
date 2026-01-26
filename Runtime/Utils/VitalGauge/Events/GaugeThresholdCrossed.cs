namespace BrewedCode.VitalGauge
{
    /// <summary>
    /// Event published when a gauge crosses a custom threshold.
    /// This is an edge-trigger event: fires once when crossing, not continuously while crossed.
    /// </summary>
    public readonly struct GaugeThresholdCrossed
    {
        /// <summary>Unique identifier of the gauge.</summary>
        public readonly string Id;

        /// <summary>Name of the threshold that was crossed.</summary>
        public readonly string ThresholdName;

        /// <summary>Direction of crossing (Entering or Exiting).</summary>
        public readonly CrossingDirection Direction;

        /// <summary>The threshold value (0-1) that was crossed.</summary>
        public readonly float ThresholdValue;

        /// <summary>Current normalized gauge value (0-1) at time of crossing.</summary>
        public readonly float CurrentNormalized;

        /// <summary>
        /// Creates a new threshold crossing event.
        /// </summary>
        public GaugeThresholdCrossed(
            string id,
            string thresholdName,
            CrossingDirection direction,
            float thresholdValue,
            float currentNormalized)
        {
            Id = id;
            ThresholdName = thresholdName;
            Direction = direction;
            ThresholdValue = thresholdValue;
            CurrentNormalized = currentNormalized;
        }
    }
}
