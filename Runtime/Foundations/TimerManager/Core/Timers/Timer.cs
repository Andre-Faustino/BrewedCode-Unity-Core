namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Basic upward-counting timer.
    ///
    /// Elapsed time increases from 0 to Duration.
    /// Can loop continuously if IsLooping is true.
    /// </summary>
    public sealed class Timer : TimerBase
    {
        public Timer(TimerId id, float duration, bool isLooping = false)
            : base(id, duration, isLooping) { }

        public override void Advance(float delta)
        {
            if (!IsRunning || IsPaused) return;

            Elapsed += delta;

            if (Elapsed >= Duration)
            {
                if (IsLooping)
                {
                    Elapsed = Elapsed % Duration;
                }
                else
                {
                    Elapsed = Duration;
                    IsCompleted = true;
                    IsRunning = false;
                }
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
