namespace BrewedCode.TimerManager
{
    /// <summary>Published when a timer is resumed from pause.</summary>
    public sealed class TimerResumedEvent
    {
        public TimerId TimerId { get; set; }
    }
}
