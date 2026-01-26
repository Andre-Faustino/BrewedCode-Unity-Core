namespace BrewedCode.VitalGauge
{
    /// <summary>
    /// Event published when a gauge transitions into the Low state.
    /// This is an edge-trigger event: it only fires once when entering the Low state,
    /// not continuously while the gauge remains low.
    /// </summary>
    public readonly struct GaugeBecameLow
    {
        /// <summary>Unique identifier of the gauge.</summary>
        public readonly string Id;

        public GaugeBecameLow(string id) => Id = id;
    }
}
