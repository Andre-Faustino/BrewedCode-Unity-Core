using System;

namespace BrewedCode.VitalGauge
{
    /// <summary>
    /// Represents a source of time updates (ticks) for VitalGauge.
    /// Implementations should emit the OnTick event at regular intervals with delta time values.
    /// </summary>
    public interface ITickSource
    {
        /// <summary>
        /// Event that fires whenever a tick occurs.
        /// The float parameter is the delta time (Time.deltaTime or equivalent).
        /// </summary>
        event Action<float> OnTick;
    }
}
