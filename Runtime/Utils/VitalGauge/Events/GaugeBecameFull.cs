namespace BrewedCode.VitalGauge
{
    /// <summary>
    /// Event published when a gauge transitions into the Full state.
    /// This is an edge-trigger event: it only fires once when entering the Full state,
    /// not continuously while the gauge remains full.
    /// </summary>
    public readonly struct GaugeBecameFull
    {
        /// <summary>Unique identifier of the gauge.</summary>
        public readonly string Id;

        public GaugeBecameFull(string id) => Id = id;
    }
}
