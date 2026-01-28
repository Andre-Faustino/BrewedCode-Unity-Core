namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Cooldown timer with ready state.
    ///
    /// Used for rate-limiting actions. Once ready (completed),
    /// must be consumed to restart.
    /// </summary>
    public sealed class CooldownTimer : TimerBase
    {
        /// <summary>Whether the cooldown is ready (completed).</summary>
        public bool IsReady => IsCompleted;

        public CooldownTimer(TimerId id, float duration)
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

        /// <summary>Consumes the cooldown and restarts it.</summary>
        public void Consume()
        {
            Reset();
            Start();
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
