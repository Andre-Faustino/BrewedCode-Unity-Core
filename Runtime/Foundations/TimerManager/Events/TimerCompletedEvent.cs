namespace BrewedCode.TimerManager
{
    /// <summary>Published when a timer completes its duration.</summary>
    public sealed class TimerCompletedEvent
    {
        public TimerId TimerId { get; set; }
    }
}
