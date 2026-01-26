# TimerManager - Getting Started Guide

## Quick Start (5 minutes)

### 1. Setup the Bootstrap

In your main game scene, add a new empty GameObject:
- Name: `TimerManager`
- Add Component: `TimerManagerRoot` (from `BrewedCode.TimerManager`)
- Leave `Event Bus Provider` empty (uses default `UnityEventChannelBus`)

```csharp
// It will initialize automatically on scene load
// The service is available via:
var service = TimerManagerRoot.Instance.Service;
```

### 2. Start Your First Timer

```csharp
using BrewedCode.TimerManager;

public class MyGameSystem : MonoBehaviour
{
    private void Start()
    {
        var timerId = TimerId.New();
        var service = TimerManagerRoot.Instance.Service;

        // Start a 5-second timer
        if (service.TryStartTimer(timerId, 5f, isLooping: false, out string error))
        {
            Debug.Log($"Timer started: {timerId}");
        }
        else
        {
            Debug.LogError($"Failed to start timer: {error}");
        }
    }
}
```

### 3. Listen to Timer Events

```csharp
public class MyGameUI : MonoBehaviour
{
    private void OnEnable()
    {
        // Subscribe to timer events via your event bus
        EventChannel<TimerCompletedEvent>.OnEvent += HandleTimerCompleted;
        EventChannel<TimerTickEvent>.OnEvent += HandleTimerTick;
    }

    private void OnDisable()
    {
        EventChannel<TimerCompletedEvent>.OnEvent -= HandleTimerCompleted;
        EventChannel<TimerTickEvent>.OnEvent -= HandleTimerTick;
    }

    private void HandleTimerCompleted(TimerCompletedEvent evt)
    {
        Debug.Log($"Timer {evt.TimerId} completed!");
    }

    private void HandleTimerTick(TimerTickEvent evt)
    {
        // Update progress bar
        Debug.Log($"Progress: {evt.Progress:P0}");
    }
}
```

---

## Timer Types

### Type 1: Basic Timer

Counts up from 0 to duration. Useful for tracking elapsed time.

```csharp
var timerId = TimerId.New();
var service = TimerManagerRoot.Instance.Service;

// Create a 10-second timer (non-looping)
service.TryStartTimer(timerId, 10f, isLooping: false, out _);

// Later: check progress
var info = service.GetTimerInfo(timerId);
if (info != null)
{
    Debug.Log($"Elapsed: {info.Elapsed}s");      // 0 to 10
    Debug.Log($"Progress: {info.Progress}");      // 0.0 to 1.0
    Debug.Log($"Remaining: {info.Remaining}s");   // 10 to 0
}
```

**Use Cases**:
- Animation duration
- Event scheduling
- Game round timers
- Attack duration

**With Looping**:
```csharp
// Create a looping timer
service.TryStartTimer(loopTimerId, 2f, isLooping: true, out _);

// Loops forever until manually stopped
// At 2.0s, wraps back to 0.0s and continues
```

---

### Type 2: Countdown Timer

Counts down from duration to 0. Useful for "time remaining" displays.

```csharp
var countdownId = TimerId.New();

// Start a 30-second countdown
service.TryStartCountdown(countdownId, 30f, out _);

// Listen to ticks
EventChannel<TimerTickEvent>.OnEvent += (evt) =>
{
    if (evt.TimerId == countdownId)
    {
        // Update HUD: show Remaining
        uiText.text = evt.Remaining.ToString("F1");
    }
};

// Completion
EventChannel<TimerCompletedEvent>.OnEvent += (evt) =>
{
    if (evt.TimerId == countdownId)
    {
        Debug.Log("Time's up!");
    }
};
```

**Use Cases**:
- Turn timers
- Wave countdowns
- Ability cooldown displays (seconds remaining)
- Match duration
- Respawn timers

**Difference from Timer**:
```csharp
// Timer.Remaining: 10.0, 9.5, 9.0, ...
// CountdownTimer: Always reports remaining correctly
```

---

### Type 3: Cooldown Timer

Tracks readiness state. Useful for ability cooldowns and resource regeneration.

```csharp
public class AbilitySystem : MonoBehaviour
{
    private TimerId _attackCooldown;
    private ITimerService _service;

    private void Start()
    {
        _service = TimerManagerRoot.Instance.Service;
        _attackCooldown = TimerId.New();
    }

    public bool TryAttack()
    {
        var info = _service.GetTimerInfo(_attackCooldown);

        // Check if cooldown is ready (no active cooldown)
        if (info == null || info.IsCompleted)
        {
            PerformAttack();

            // Start cooldown
            _service.TryStartCooldown(_attackCooldown, cooldownDuration: 3f, out _);
            return true;
        }

        Debug.Log($"Cooldown: {info.Remaining}s remaining");
        return false;
    }

    private void PerformAttack()
    {
        Debug.Log("Attack!");
    }
}
```

**Key Difference**:
```csharp
// Basic Timer: No concept of "ready"
// Cooldown Timer: Has IsReady property (IsCompleted)

var info = service.GetTimerInfo(cooldownId);
if (info.IsReady) { /* Can use ability */ }
```

**Use Cases**:
- Ability cooldowns
- Attack cooldowns
- Resource regeneration (health, mana, energy)
- Dash cooldowns
- Interaction cooldowns

---

### Type 4: Tween Timer

Interpolates along a curve for smooth easing.

```csharp
public class SmoothTransition : MonoBehaviour
{
    private TimerId _tweenId;
    private AnimationCurve _easeInOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void Start()
    {
        var service = TimerManagerRoot.Instance.Service;
        _tweenId = TimerId.New();

        // Create a 2-second tween
        service.TryStartTween(_tweenId, 2f, _easeInOutCurve, out _);

        // Listen to tick events
        EventChannel<TimerTickEvent>.OnEvent += OnTweenTick;
    }

    private void OnTweenTick(TimerTickEvent evt)
    {
        if (evt.TimerId == _tweenId)
        {
            // Get tween progress
            var info = TimerManagerRoot.Instance.Service.GetTimerInfo(_tweenId);
            if (info is TweenTimer tween)
            {
                // EvaluatedValue is curve output (0.0 to 1.0)
                float easeValue = tween.EvaluatedValue;

                // Apply to transform
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 2f, easeValue);
            }
        }
    }
}
```

**Common Curves**:
```csharp
// Linear interpolation
var linear = AnimationCurve.Linear(0, 0, 1, 1);

// Unity's built-in easing
var easeInOut = AnimationCurve.EaseInOut(0, 0, 1, 1);

// Custom curve in editor
[SerializeField] private AnimationCurve customCurve;
service.TryStartTween(tweenId, 3f, customCurve, out _);
```

**Use Cases**:
- Smooth transitions (scale, fade, move)
- Animated progress bars
- Eased camera movements
- Particle fade-out
- UI animations

**Note**: For complex animations, consider using Sequences or Tweens from Feel/DOTween instead. This is for simple interpolations.

---

## Common Patterns

### Pattern 1: Delayed Execution

```csharp
public void ExecuteAfterDelay(float delay, System.Action callback)
{
    var timerId = TimerId.New();
    var service = TimerManagerRoot.Instance.Service;

    service.TryStartTimer(timerId, delay, false, out _);

    EventChannel<TimerCompletedEvent>.OnEvent += (evt) =>
    {
        if (evt.TimerId == timerId)
        {
            callback.Invoke();
        }
    };
}

// Usage
ExecuteAfterDelay(2f, () => {
    Debug.Log("Executed after 2 seconds");
});
```

### Pattern 2: Progress Tracking

```csharp
public class ProgressBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    private TimerId _currentTimer;
    private ITimerService _service;

    private void Start()
    {
        _service = TimerManagerRoot.Instance.Service;
        EventChannel<TimerTickEvent>.OnEvent += UpdateProgress;
    }

    public void StartAction(float duration)
    {
        _currentTimer = TimerId.New();
        _service.TryStartTimer(_currentTimer, duration, false, out _);
    }

    private void UpdateProgress(TimerTickEvent evt)
    {
        if (evt.TimerId == _currentTimer)
        {
            fillImage.fillAmount = evt.Progress;
        }
    }
}
```

### Pattern 3: Multi-Timer Management

```csharp
public class GameRound : MonoBehaviour
{
    private Dictionary<string, TimerId> _timers = new();
    private ITimerService _service;

    private void Start()
    {
        _service = TimerManagerRoot.Instance.Service;
    }

    public void StartRound(float duration)
    {
        var roundTimerId = TimerId.New();
        _timers["round"] = roundTimerId;

        _service.TryStartTimer(roundTimerId, duration, false, out _);
    }

    public void PauseRound()
    {
        var timerId = _timers["round"];
        if (_service.TryPauseTimer(timerId, out _))
        {
            Debug.Log("Round paused");
        }
    }

    public void ResumeRound()
    {
        var timerId = _timers["round"];
        if (_service.TryResumeTimer(timerId, out _))
        {
            Debug.Log("Round resumed");
        }
    }
}
```

---

## Global Time Control

### Time Scale

```csharp
var service = TimerManagerRoot.Instance.Service;

// All timers now run at half speed
service.SetTimeScale(0.5f);

// Return to normal
service.SetTimeScale(1.0f);

// Fast forward
service.SetTimeScale(2.0f);

// Complete freeze (but not the same as Pause)
service.SetTimeScale(0.0f);
```

**Use Cases**:
- Slow-motion effects
- Fast-forward for testing
- Difficulty scaling (time passes slower/faster)

**Note**: `TimeScale` affects `Delta` but not `UnscaledDelta`.

### Pause/Resume All

```csharp
var service = TimerManagerRoot.Instance.Service;

// Pause all timers at once
service.Pause();

// All timers freeze, no ticks, no completions
// This is different from TimeScale(0) because:
// - TimeScale is multiplicative
// - Pause is explicit state
// - Pause prevents events from publishing

// Resume all timers
service.Resume();
```

**Use Cases**:
- Pause menu
- Cutscenes
- Tutorial pauses

---

## Error Handling

All mutation operations follow the `TryX(out string error)` pattern:

```csharp
var service = TimerManagerRoot.Instance.Service;

// Good: Check error
if (service.TryStartTimer(timerId, 5f, false, out string error))
{
    Debug.Log("Success");
}
else
{
    Debug.LogError($"Failed: {error}");
    // error might be:
    // - "Duration must be positive."
    // - "Timer {timerId} already exists."
}

// Bad: Ignore error (will work, but not recommended)
service.TryStartTimer(timerId, 5f, false, out _);
```

**Common Errors**:
```csharp
// Negative or zero duration
service.TryStartTimer(id, -1f, false, out error);  // "Duration must be positive."

// Timer already exists
var id = TimerId.New();
service.TryStartTimer(id, 5f, false, out _);
service.TryStartTimer(id, 5f, false, out error);  // "Timer {id} already exists."

// Timer not found
service.TryPauseTimer(nonexistentId, out error);  // "Timer {id} not found."

// Invalid state
var id = TimerId.New();
service.TryResumeTimer(id, out error);  // "Timer not found."
```

---

## Querying Timer State

```csharp
var service = TimerManagerRoot.Instance.Service;

// Get single timer info
var info = service.GetTimerInfo(timerId);
if (info != null)
{
    Debug.Log($"Type: {info.Type}");           // "Timer", "CountdownTimer", etc
    Debug.Log($"Duration: {info.Duration}s");  // Original duration
    Debug.Log($"Elapsed: {info.Elapsed}s");    // Time passed
    Debug.Log($"Remaining: {info.Remaining}s");
    Debug.Log($"Progress: {info.Progress:P0}"); // 50%
    Debug.Log($"IsRunning: {info.IsRunning}");
    Debug.Log($"IsPaused: {info.IsPaused}");
    Debug.Log($"IsCompleted: {info.IsCompleted}");
}

// Get all timers
var allTimers = service.GetAllTimers();
foreach (var timer in allTimers)
{
    Debug.Log($"{timer.Type}: {timer.Progress:P0}");
}
```

---

## Testing Timers

Unit tests don't require the full Unity runtime. Test the pure C# service:

```csharp
using NUnit.Framework;
using BrewedCode.TimerManager;

[TestFixture]
public class MyTimerTests
{
    private TimerService _service;

    [SetUp]
    public void Setup()
    {
        var mockBus = new MockEventBus();
        _service = new TimerService(mockBus);
    }

    [Test]
    public void CanStartAndCompleteTimer()
    {
        var timerId = TimerId.New();

        _service.TryStartTimer(timerId, 5f, false, out _);
        _service.Tick(3f);  // Advance 3 seconds

        var info = _service.GetTimerInfo(timerId);
        Assert.AreEqual(3f, info.Elapsed, 0.01f);
        Assert.IsFalse(info.IsCompleted);

        _service.Tick(2.5f);  // Advance to completion

        info = _service.GetTimerInfo(timerId);
        Assert.IsTrue(info.IsCompleted);
    }

    [Test]
    public void PausePreventsTick()
    {
        var timerId = TimerId.New();
        _service.TryStartTimer(timerId, 10f, false, out _);

        _service.TryPauseTimer(timerId, out _);
        _service.Tick(5f);

        var info = _service.GetTimerInfo(timerId);
        Assert.AreEqual(0f, info.Elapsed);  // No advancement
    }

    // Mock implementation (simplified)
    private class MockEventBus : IEventBus
    {
        public void Publish<T>(T evt) { }
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) => null;
    }
}
```

---

## Troubleshooting

### "TimerManager not initialized"
- **Problem**: `TimerManagerRoot` not in scene
- **Solution**: Add it to your main game scene before accessing `TimerManagerRoot.Instance`

### Timer never completes
- **Problem**: Forgot to call `Tick()` every frame
- **Solution**: Ensure `TimerManagerRoot` is active in the scene (handles Update automatically)

### Events not firing
- **Problem**: Using wrong event type or forgot to subscribe
- **Solution**:
  ```csharp
  // Subscribe to correct event
  EventChannel<TimerCompletedEvent>.OnEvent += HandleCompletion;
  ```

### Timer values seem wrong
- **Problem**: Checking `Elapsed` on a `CountdownTimer`
- **Solution**: Check `Remaining` instead for countdown timers
  ```csharp
  // Timer: Elapsed increases (0 → 10)
  // Countdown: Remaining decreases (10 → 0)
  ```

### State doesn't change after TryX
- **Problem**: Using result of operation without checking error
- **Solution**: Always check the boolean return value
  ```csharp
  if (!service.TryStartTimer(id, 5f, false, out string error))
  {
      Debug.LogError(error);
  }
  ```

---

## Next Steps

1. **Check Architecture.md** for system design details
2. **Check MMTimeManagerIntegration.md** if using More Mountains tools
3. **Run the unit tests** in `Tests/` folder to verify installation
4. **Create a demo scene** with a simple countdown to learn the API

Enjoy! 🎮
