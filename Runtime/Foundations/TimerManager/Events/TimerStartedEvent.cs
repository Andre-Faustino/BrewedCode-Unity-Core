namespace BrewedCode.TimerManager
{
    /// <summary>Published when a timer starts.</summary>
    public sealed class TimerStartedEvent
    {
        public TimerId TimerId { get; set; }
        public float Duration { get; set; }
    }
}
