using System;
using BrewedCode.VitalGauge;

namespace BrewedCode.VitalGauge.Tests
{
    /// <summary>
    /// Mock tick source for testing that allows manual control of tick emissions.
    /// </summary>
    public class MockTickSource : ITickSource
    {
        public event Action<float> OnTick;

        /// <summary>Manually emit a tick with the given delta time.</summary>
        public void EmitTick(float dt)
        {
            OnTick?.Invoke(dt);
        }
    }
}
