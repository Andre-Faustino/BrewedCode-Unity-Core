namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Manages game time state: tick counter, time scale, and pause state.
    ///
    /// Deterministic and independent of Unity's Time class.
    /// Each call to Advance() returns a new GameTime snapshot and increments the tick.
    /// </summary>
    public sealed class TimeContext
    {
        private long _currentTick;
        private float _timeScale = 1f;
        private bool _isPaused;

        public TimeContext() { }

        /// <summary>
        /// Advances time by the given delta, returning a new GameTime snapshot.
        ///
        /// If paused, delta is zeroed (no time advancement).
        /// Delta is scaled by TimeScale.
        /// Tick is always incremented.
        /// </summary>
        public GameTime Advance(float deltaTime)
        {
            var unscaledDelta = deltaTime;
            if (_isPaused) deltaTime = 0f;

            var scaledDelta = deltaTime * _timeScale;
            var gameTime = new GameTime(
                tick: _currentTick++,
                delta: scaledDelta,
                unscaledDelta: unscaledDelta,
                timeScale: _timeScale,
                isPaused: _isPaused
            );
            return gameTime;
        }

        /// <summary>Sets the time scale multiplier (1.0 = normal, 0.5 = half speed).</summary>
        public void SetTimeScale(float scale) => _timeScale = scale;

        /// <summary>Gets the current time scale.</summary>
        public float GetTimeScale() => _timeScale;

        /// <summary>Pauses time (Delta becomes 0).</summary>
        public void Pause() => _isPaused = true;

        /// <summary>Resumes time.</summary>
        public void Resume() => _isPaused = false;

        /// <summary>Gets the current pause state.</summary>
        public bool GetIsPaused() => _isPaused;

        /// <summary>Gets the current tick counter.</summary>
        public long GetCurrentTick() => _currentTick;
    }
}
