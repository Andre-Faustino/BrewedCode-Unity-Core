namespace BrewedCode.VitalGauge
{
    /// <summary>
    /// Direction of threshold crossing.
    /// </summary>
    public enum CrossingDirection
    {
        /// <summary>Gauge crossed below the threshold (entering danger zone).</summary>
        Entering,

        /// <summary>Gauge crossed above the threshold (exiting danger zone).</summary>
        Exiting
    }
}
