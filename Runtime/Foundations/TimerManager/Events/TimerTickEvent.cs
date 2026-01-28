namespace BrewedCode.TimerManager
{
    /// <summary>Published every frame for each running timer.</summary>
    public sealed class TimerTickEvent
    {
        public TimerId TimerId { get; set; }
        public float Elapsed { get; set; }
        public float Remaining { get; set; }
        public float Progress { get; set; }
    }
}
