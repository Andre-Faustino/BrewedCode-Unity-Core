namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Counts down from Duration to 0.
    ///
    /// Behaves like a standard countdown timer that completes when
    /// Elapsed reaches Duration.
    /// </summary>
    public sealed class CountdownTimer : TimerBase
    {
        public CountdownTimer(TimerId id, float duration)
            : base(id, duration) { }

        public override void Advance(float delta)
        {
            if (!IsRunning || IsPaused) return;

            Elapsed += delta;

            if (Elapsed >= Duration)
            {
                Elapsed = Duration;
                IsCompleted = true;
                IsRunning = false;
            }
        }

        public override void Reset()
        {
            Elapsed = 0f;
            IsCompleted = false;
            IsRunning = false;
            IsPaused = false;
        }
    }
}
