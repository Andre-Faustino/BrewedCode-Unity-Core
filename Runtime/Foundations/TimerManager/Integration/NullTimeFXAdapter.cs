namespace BrewedCode.TimerManager
{
    /// <summary>
    /// No-op implementation of ITimeFXAdapter.
    ///
    /// Used as default when no FX adapter is needed.
    /// </summary>
    public sealed class NullTimeFXAdapter : ITimeFXAdapter
    {
        public void OnTimerStarted(TimerId id) { }
        public void OnTimerTick(TimerId id, float progress) { }
        public void OnTimerCompleted(TimerId id) { }
    }
}
