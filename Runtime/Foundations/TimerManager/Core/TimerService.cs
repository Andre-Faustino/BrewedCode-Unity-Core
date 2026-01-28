using System;
using System.Collections.Generic;
using System.Linq;
using BrewedCode.Events;
using UnityEngine;
using BrewedCode.Logging;

namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Pure C# implementation of ITimerService.
    ///
    /// Manages all timers, schedules them, and publishes events.
    /// 100% deterministic and testable without Unity runtime.
    ///
    /// Key Design:
    /// - Delta time is INJECTED (not accessed from Time.deltaTime)
    /// - All timers share the same TimeContext (global time state)
    /// - Time.deltaTime only used in TimerManagerRoot.Update() -> Tick()
    /// - Supports time scaling, pause/resume, and replay systems
    ///
    /// Timer Types:
    /// - Basic: Count from 0 to duration
    /// - Countdown: Count from duration to 0
    /// - Cooldown: Tracks ready state
    /// - Tween: Curve-based interpolation
    ///
    /// State Transitions:
    /// - Start → Running
    /// - Running → Paused (via TryPauseTimer)
    /// - Paused → Running (via TryResumeTimer)
    /// - Running → Completed (naturally or via Tick)
    /// - Any → Cancelled (via TryStopTimer)
    ///
    /// Performance: O(n) per Tick where n = active timers
    /// No garbage generation - value types and struct iteration
    /// </summary>
    public sealed class TimerService : ITimerService
    {
        private readonly IEventBus _eventBus;
        private readonly TimerScheduler _scheduler;
        private readonly TimeContext _timeContext;
        private readonly ILog? _logger;

        public TimerService(IEventBus eventBus, ILoggingService loggingService = null)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _scheduler = new TimerScheduler();
            _timeContext = new TimeContext();
            _logger = loggingService?.GetLogger<TimerService>();
        }

        // ===== Timer Lifecycle =====

        public bool TryStartTimer(TimerId id, float duration, bool isLooping, out string error)
        {
            error = "";

            if (duration <= 0f)
            {
                error = "Duration must be positive.";
                _logger.WarningSafe($"Timer start failed: {error}");
                return false;
            }

            if (_scheduler.GetTimer(id) != null)
            {
                error = $"Timer {id} already exists.";
                _logger.WarningSafe($"Timer start failed: {error}");
                return false;
            }

            var timer = new Timer(id, duration, isLooping);
            timer.Start();
            _scheduler.AddTimer(timer);

            _logger.InfoSafe($"Timer started: {id} ({duration:F2}s, looping={isLooping})");

            _eventBus.Publish(new TimerStartedEvent
            {
                TimerId = id,
                Duration = duration
            });

            return true;
        }

        public bool TryStartCountdown(TimerId id, float duration, out string error)
        {
            error = "";

            if (duration <= 0f)
            {
                error = "Duration must be positive.";
                _logger.WarningSafe($"Countdown start failed: {error}");
                return false;
            }

            if (_scheduler.GetTimer(id) != null)
            {
                error = $"Timer {id} already exists.";
                _logger.WarningSafe($"Countdown start failed: {error}");
                return false;
            }

            var timer = new CountdownTimer(id, duration);
            timer.Start();
            _scheduler.AddTimer(timer);

            _logger.InfoSafe($"Countdown started: {id} ({duration:F2}s)");

            _eventBus.Publish(new TimerStartedEvent
            {
                TimerId = id,
                Duration = duration
            });

            return true;
        }

        public bool TryStartCooldown(TimerId id, float duration, out string error)
        {
            error = "";

            if (duration <= 0f)
            {
                error = "Duration must be positive.";
                _logger.WarningSafe($"Cooldown start failed: {error}");
                return false;
            }

            if (_scheduler.GetTimer(id) != null)
            {
                error = $"Timer {id} already exists.";
                _logger.WarningSafe($"Cooldown start failed: {error}");
                return false;
            }

            var timer = new CooldownTimer(id, duration);
            timer.Start();
            _scheduler.AddTimer(timer);

            _logger.InfoSafe($"Cooldown started: {id} ({duration:F2}s)");

            _eventBus.Publish(new TimerStartedEvent
            {
                TimerId = id,
                Duration = duration
            });

            return true;
        }

        public bool TryStartTween(TimerId id, float duration, AnimationCurve curve, out string error)
        {
            error = "";

            if (duration <= 0f)
            {
                error = "Duration must be positive.";
                _logger.WarningSafe($"Tween start failed: {error}");
                return false;
            }

            if (curve == null)
            {
                error = "Curve cannot be null.";
                _logger.WarningSafe($"Tween start failed: {error}");
                return false;
            }

            if (_scheduler.GetTimer(id) != null)
            {
                error = $"Timer {id} already exists.";
                _logger.WarningSafe($"Tween start failed: {error}");
                return false;
            }

            var timer = new TweenTimer(id, duration, curve);
            timer.Start();
            _scheduler.AddTimer(timer);

            _logger.InfoSafe($"Tween started: {id} ({duration:F2}s)");

            _eventBus.Publish(new TimerStartedEvent
            {
                TimerId = id,
                Duration = duration
            });

            return true;
        }

        public bool TryPauseTimer(TimerId id, out string error)
        {
            error = "";
            var timer = _scheduler.GetTimer(id);

            if (timer == null)
            {
                error = $"Timer {id} not found.";
                return false;
            }

            if (!timer.IsRunning)
            {
                error = "Timer is not running.";
                return false;
            }

            timer.Pause();
            _logger.InfoSafe($"Timer paused: {id} (elapsed: {timer.Elapsed:F2}s)");

            _eventBus.Publish(new TimerPausedEvent { TimerId = id });

            return true;
        }

        public bool TryResumeTimer(TimerId id, out string error)
        {
            error = "";
            var timer = _scheduler.GetTimer(id);

            if (timer == null)
            {
                error = $"Timer {id} not found.";
                return false;
            }

            if (!timer.IsPaused)
            {
                error = "Timer is not paused.";
                return false;
            }

            timer.Resume();
            _logger.InfoSafe($"Timer resumed: {id}");

            _eventBus.Publish(new TimerResumedEvent { TimerId = id });

            return true;
        }

        public bool TryStopTimer(TimerId id, out string error)
        {
            error = "";
            var timer = _scheduler.GetTimer(id);

            if (timer == null)
            {
                error = $"Timer {id} not found.";
                return false;
            }

            timer.Stop();
            _logger.InfoSafe($"Timer stopped: {id}");
            _scheduler.RemoveTimer(id);

            _eventBus.Publish(new TimerCancelledEvent { TimerId = id });

            return true;
        }

        // ===== Queries =====

        public TimerInfo? GetTimerInfo(TimerId id)
        {
            var timer = _scheduler.GetTimer(id);
            if (timer == null) return null;

            return new TimerInfo(
                id: timer.Id,
                type: timer.GetType().Name,
                duration: timer.Duration,
                elapsed: timer.Elapsed,
                remaining: timer.Remaining,
                progress: timer.Progress,
                isRunning: timer.IsRunning,
                isPaused: timer.IsPaused,
                isCompleted: timer.IsCompleted,
                isLooping: timer.IsLooping
            );
        }

        public IReadOnlyList<TimerInfo> GetAllTimers()
        {
            var infos = new List<TimerInfo>();
            foreach (var timer in _scheduler.GetAllTimers())
            {
                var info = GetTimerInfo(timer.Id);
                if (info != null)
                {
                    infos.Add(info);
                }
            }
            return infos;
        }

        // ===== Global Time Control =====

        public void SetTimeScale(float scale)
        {
            var oldScale = _timeContext.GetTimeScale();
            _timeContext.SetTimeScale(scale);
            _logger.InfoSafe($"TimeScale changed: {oldScale:F2}x → {scale:F2}x (active timers: {_scheduler.GetAllTimers().Count()})");
        }

        public void Pause()
        {
            _timeContext.Pause();
            _logger.InfoSafe($"Timer system paused (active timers: {_scheduler.GetAllTimers().Count()})");
        }

        public void Resume()
        {
            _timeContext.Resume();
            _logger.InfoSafe($"Timer system resumed (active timers: {_scheduler.GetAllTimers().Count()})");
        }

        // ===== Update Loop =====

        public void Tick(float deltaTime)
        {
            // Advance time context and get current game time
            var gameTime = _timeContext.Advance(deltaTime);

            // If paused, don't advance timers
            if (gameTime.IsPaused) return;

            // Advance all timers and get completed ones
            var completedTimers = _scheduler.AdvanceAll(gameTime.Delta);

            // Publish tick events for all running timers
            foreach (var timer in _scheduler.GetAllTimers())
            {
                if (timer.IsRunning && !timer.IsPaused)
                {
                    _eventBus.Publish(new TimerTickEvent
                    {
                        TimerId = timer.Id,
                        Elapsed = timer.Elapsed,
                        Remaining = timer.Remaining,
                        Progress = timer.Progress
                    });
                }
            }

            // Publish completion events
            foreach (var timerId in completedTimers)
            {
                _logger.InfoSafe($"Timer completed: {timerId}");
                _eventBus.Publish(new TimerCompletedEvent { TimerId = timerId });
            }
        }
    }
}
