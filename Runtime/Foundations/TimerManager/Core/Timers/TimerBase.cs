namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Abstract base class for all timer types.
    ///
    /// Defines the common interface and lifecycle for timers:
    /// Start, Pause, Resume, Stop, Advance, Reset.
    ///
    /// Subclasses must implement Advance() (time update logic)
    /// and Reset() (state reset logic).
    /// </summary>
    public abstract class TimerBase
    {
        /// <summary>Unique identifier for this timer.</summary>
        public TimerId Id { get; }

        /// <summary>Total duration of the timer in seconds.</summary>
        public float Duration { get; protected set; }

        /// <summary>Elapsed time since start in seconds.</summary>
        public float Elapsed { get; protected set; }

        /// <summary>Remaining time until completion in seconds (read-only).</summary>
        public float Remaining => System.Math.Max(0f, Duration - Elapsed);

        /// <summary>Progress from 0 to 1 (read-only).</summary>
        public float Progress => Duration > 0 ? Elapsed / Duration : 0f;

        /// <summary>Whether the timer is currently running.</summary>
        public bool IsRunning { get; protected set; }

        /// <summary>Whether the timer is paused.</summary>
        public bool IsPaused { get; protected set; }

        /// <summary>Whether the timer has completed.</summary>
        public bool IsCompleted { get; protected set; }

        /// <summary>Whether the timer loops after completion.</summary>
        public bool IsLooping { get; set; }

        protected TimerBase(TimerId id, float duration, bool isLooping = false)
        {
            Id = id;
            Duration = duration;
            IsLooping = isLooping;
        }

        /// <summary>Advances the timer by delta time. Subclasses must implement.</summary>
        public abstract void Advance(float delta);

        /// <summary>Resets the timer to initial state. Subclasses must implement.</summary>
        public abstract void Reset();

        /// <summary>Starts the timer.</summary>
        public void Start()
        {
            IsRunning = true;
            IsPaused = false;
        }

        /// <summary>Pauses the timer (Advance has no effect).</summary>
        public void Pause()
        {
            IsPaused = true;
        }

        /// <summary>Resumes a paused timer.</summary>
        public void Resume()
        {
            IsPaused = false;
        }

        /// <summary>Stops the timer and marks as completed.</summary>
        public void Stop()
        {
            IsRunning = false;
            IsCompleted = true;
        }
    }
}
