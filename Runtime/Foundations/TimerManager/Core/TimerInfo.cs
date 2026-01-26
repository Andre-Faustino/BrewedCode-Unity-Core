namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Read-only DTO (Data Transfer Object) for querying timer state.
    ///
    /// Immutable snapshot of a timer's current state.
    /// Returned by ITimerService.GetTimerInfo().
    /// </summary>
    public sealed class TimerInfo
    {
        public TimerId Id { get; }
        public string Type { get; }
        public float Duration { get; }
        public float Elapsed { get; }
        public float Remaining { get; }
        public float Progress { get; }
        public bool IsRunning { get; }
        public bool IsPaused { get; }
        public bool IsCompleted { get; }
        public bool IsLooping { get; }

        public TimerInfo(TimerId id, string type, float duration, float elapsed,
                         float remaining, float progress, bool isRunning,
                         bool isPaused, bool isCompleted, bool isLooping)
        {
            Id = id;
            Type = type;
            Duration = duration;
            Elapsed = elapsed;
            Remaining = remaining;
            Progress = progress;
            IsRunning = isRunning;
            IsPaused = isPaused;
            IsCompleted = isCompleted;
            IsLooping = isLooping;
        }

        public override string ToString() =>
            $"TimerInfo({Type} {Id}, {Elapsed:F2}/{Duration:F2}s, {Progress * 100:F0}%)";
    }
}
