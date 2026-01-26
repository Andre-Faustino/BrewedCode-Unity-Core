namespace BrewedCode.TimerManager
{
    /// <summary>Published when a timer is paused.</summary>
    public sealed class TimerPausedEvent
    {
        public TimerId TimerId { get; set; }
    }
}
