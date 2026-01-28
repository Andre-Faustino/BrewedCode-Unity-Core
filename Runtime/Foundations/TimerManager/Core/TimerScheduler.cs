using System;
using System.Collections.Generic;

namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Internal scheduler for managing timers.
    ///
    /// Not exposed in the public API. Handles:
    /// - Adding/removing timers
    /// - Advancing all timers each frame
    /// - Detecting completions
    /// - Maintaining deterministic ordering
    /// </summary>
    internal sealed class TimerScheduler
    {
        private readonly Dictionary<TimerId, TimerBase> _timers = new();
        private readonly List<TimerId> _completedTimers = new();

        /// <summary>Adds a timer to the scheduler.</summary>
        public void AddTimer(TimerBase timer)
        {
            _timers[timer.Id] = timer;
        }

        /// <summary>Removes a timer from the scheduler.</summary>
        public bool RemoveTimer(TimerId id)
        {
            return _timers.Remove(id);
        }

        /// <summary>Gets a timer by ID, or null if not found.</summary>
        public TimerBase? GetTimer(TimerId id)
        {
            return _timers.TryGetValue(id, out var timer) ? timer : null;
        }

        /// <summary>
        /// Advances all running timers by delta and returns IDs of completed timers.
        ///
        /// Deterministic: iterates in stable Dictionary order.
        /// Paused timers are skipped.
        /// </summary>
        public IEnumerable<TimerId> AdvanceAll(float delta)
        {
            _completedTimers.Clear();

            foreach (var timer in _timers.Values)
            {
                if (!timer.IsRunning || timer.IsPaused) continue;

                timer.Advance(delta);

                if (timer.IsCompleted)
                {
                    _completedTimers.Add(timer.Id);
                }
            }

            return _completedTimers;
        }

        /// <summary>Gets all timers (for iteration).</summary>
        public IEnumerable<TimerBase> GetAllTimers() => _timers.Values;

        /// <summary>Gets count of active timers.</summary>
        public int GetTimerCount() => _timers.Count;
    }
}
