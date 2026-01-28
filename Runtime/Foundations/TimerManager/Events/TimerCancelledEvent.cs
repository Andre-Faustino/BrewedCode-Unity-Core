namespace BrewedCode.TimerManager
{
    /// <summary>Published when a timer is stopped by user.</summary>
    public sealed class TimerCancelledEvent
    {
        public TimerId TimerId { get; set; }
    }
}
