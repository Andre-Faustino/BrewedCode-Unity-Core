namespace BrewedCode.VitalGauge
{
    /// <summary>
    /// Event published whenever a gauge's state transitions from one state to another.
    /// This captures both the previous state (From) and the new state (To).
    /// </summary>
    public readonly struct GaugeStateChanged
    {
        /// <summary>Unique identifier of the gauge.</summary>
        public readonly string Id;

        /// <summary>The state the gauge was in before the transition.</summary>
        public readonly GaugeState From;

        /// <summary>The state the gauge transitioned to.</summary>
        public readonly GaugeState To;

        public GaugeStateChanged(string id, GaugeState from, GaugeState to)
        {
            Id = id;
            From = from;
            To = to;
        }
    }
}
