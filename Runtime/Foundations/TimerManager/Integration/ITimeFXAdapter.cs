namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Interface for timer FX/UI integration (Future use).
    ///
    /// NOT IMPLEMENTED in this phase.
    /// Prepared for future integration with Feel, DOTween, or custom FX.
    ///
    /// Subscribers can listen to TimerTickEvent via IEventBus
    /// and implement their own FX logic.
    /// </summary>
    public interface ITimeFXAdapter
    {
        void OnTimerStarted(TimerId id);
        void OnTimerTick(TimerId id, float progress);
        void OnTimerCompleted(TimerId id);
    }
}
