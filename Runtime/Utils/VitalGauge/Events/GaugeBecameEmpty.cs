namespace BrewedCode.VitalGauge
{
    /// <summary>
    /// Event published when a gauge transitions into the Empty state.
    /// This is an edge-trigger event: it only fires once when entering the Empty state,
    /// not continuously while the gauge remains empty.
    /// </summary>
    public readonly struct GaugeBecameEmpty
    {
        /// <summary>Unique identifier of the gauge.</summary>
        public readonly string Id;

        public GaugeBecameEmpty(string id) => Id = id;
    }
}
