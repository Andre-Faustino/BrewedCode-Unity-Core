namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Immutable value object representing a snapshot of game time.
    ///
    /// GameTime is deterministic and independent of Unity's Time class.
    /// It tracks frame ticks, scaled/unscaled delta time, time scale factor,
    /// and pause state.
    /// </summary>
    public readonly struct GameTime
    {
        /// <summary>Monotonically increasing frame counter.</summary>
        public readonly long Tick;

        /// <summary>Delta time scaled by TimeScale (affected by pause).</summary>
        public readonly float Delta;

        /// <summary>Raw delta time not affected by TimeScale or pause state.</summary>
        public readonly float UnscaledDelta;

        /// <summary>Current time scale multiplier (1.0 = normal speed).</summary>
        public readonly float TimeScale;

        /// <summary>Whether the game is currently paused.</summary>
        public readonly bool IsPaused;

        public GameTime(long tick, float delta, float unscaledDelta,
                        float timeScale, bool isPaused)
        {
            Tick = tick;
            Delta = delta;
            UnscaledDelta = unscaledDelta;
            TimeScale = timeScale;
            IsPaused = isPaused;
        }

        /// <summary>
        /// Creates a new GameTime with an advanced tick and delta.
        /// </summary>
        public GameTime WithDelta(float newDelta) =>
            new(Tick + 1, newDelta, newDelta, TimeScale, IsPaused);

        public override string ToString() =>
            $"GameTime(tick={Tick}, delta={Delta:F3}, scale={TimeScale:F2}, paused={IsPaused})";
    }
}
