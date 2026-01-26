using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Public API for the timer service.
    ///
    /// Pure interface with no implementation details.
    /// All operations use TryX(out string error) pattern.
    /// State queries return immutable TimerInfo DTOs.
    /// </summary>
    public interface ITimerService
    {
        // ===== Timer Lifecycle =====

        /// <summary>Starts a basic timer that counts up from 0 to duration.</summary>
        bool TryStartTimer(TimerId id, float duration, bool isLooping, out string error);

        /// <summary>Starts a countdown timer.</summary>
        bool TryStartCountdown(TimerId id, float duration, out string error);

        /// <summary>Starts a cooldown timer (ready state).</summary>
        bool TryStartCooldown(TimerId id, float duration, out string error);

        /// <summary>Starts a tween timer with curve-based interpolation.</summary>
        bool TryStartTween(TimerId id, float duration, AnimationCurve curve, out string error);

        /// <summary>Pauses a running timer.</summary>
        bool TryPauseTimer(TimerId id, out string error);

        /// <summary>Resumes a paused timer.</summary>
        bool TryResumeTimer(TimerId id, out string error);

        /// <summary>Stops a timer and removes it.</summary>
        bool TryStopTimer(TimerId id, out string error);

        // ===== Queries =====

        /// <summary>Gets current state of a timer, or null if not found.</summary>
        TimerInfo? GetTimerInfo(TimerId id);

        /// <summary>Gets all active timers.</summary>
        IReadOnlyList<TimerInfo> GetAllTimers();

        // ===== Global Time Control =====

        /// <summary>Sets the global time scale (1.0 = normal, 0.5 = half speed).</summary>
        void SetTimeScale(float scale);

        /// <summary>Pauses all timers.</summary>
        void Pause();

        /// <summary>Resumes all timers.</summary>
        void Resume();

        // ===== Update Loop =====

        /// <summary>
        /// Advances all timers by delta time.
        /// Must be called every frame by TimerManagerRoot.Update().
        /// </summary>
        void Tick(float deltaTime);
    }
}
