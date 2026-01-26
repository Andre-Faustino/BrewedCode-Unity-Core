# TimerManager - MMTimeManager Integration Guide

## Overview

This guide explains how to integrate the BrewedCode `TimerManager` system with **More Mountains' MMTimeManager** (Feel framework).

Both systems manage time, but they solve different problems:

| Aspect | TimerManager | MMTimeManager |
|--------|--------------|---------------|
| **Scope** | Game timers, cooldowns, delays, tweens | Global time scale, pause management |
| **Purpose** | Temporal logic for gameplay | Slow-motion, freeze effects |
| **Integration** | Events via IEventBus | Hooks, delegates |
| **Coupling** | Loose (event-driven) | Tight (direct dependency) |

**Strategy**: Use MMTimeManager for global time control, TimerManager for game logic timers.

---

## Architecture Integration

```
┌─────────────────────────────────────────────────────────────┐
│                      MMTimeManager                          │
│              (Feel's global time control)                   │
│                                                             │
│  - SetTimeScale(scale)                                      │
│  - Freeze / Unfreeze                                        │
│  - Slow-mo effects                                          │
│  - Global pause hooks                                       │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ Talks to
                       ↓
┌─────────────────────────────────────────────────────────────┐
│              TimerManager.TimeContext                       │
│                                                             │
│  - Receives global time scale from MMTimeManager           │
│  - Respects MMTimeManager's pause state                    │
│  - Produces GameTime snapshots                             │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ Drives
                       ↓
┌─────────────────────────────────────────────────────────────┐
│           Individual Timer Advancement                      │
│                                                             │
│  - Timers advance based on scaled/paused time              │
│  - Events publish when timers tick/complete                │
└─────────────────────────────────────────────────────────────┘
```

---

## Two Integration Approaches

### Approach A: Minimal Coupling (Recommended)

Keep systems independent. MMTimeManager controls physics/animation, TimerManager controls game logic.

```csharp
// TimerManager.TimeContext remains its own time source
// MMTimeManager is used for visual effects only
// No synchronization needed
```

**Pros**:
- Clean separation of concerns
- Easy to test independently
- No cross-system dependencies

**Cons**:
- Two time systems to manage
- Potential desync in effects

---

### Approach B: Full Integration

Make TimerManager respect MMTimeManager's time scale.

```csharp
// TimerManager.TimeContext reads from MMTimeManager
// Single source of truth for time
// More complex to set up
```

**Pros**:
- Single time source
- Guaranteed synchronization
- Simpler global time control

**Cons**:
- Tight coupling
- Dependency on Feel framework
- More complex to debug

---

## Implementation

### Step 1: Check MMTimeManager Installation

```csharp
// In your project, verify:
// - More Mountains/Feel folder exists
// - MMTimeManager in scene or as singleton
// - Using Feel/CineMachine (if applicable)

using MoreMountains.Feedbacks;  // Or appropriate MM namespace
```

### Step 2: Approach A - Independent Systems

No code changes needed. Just use both systems independently:

```csharp
public class GameLogic : MonoBehaviour
{
    private void Start()
    {
        var timerService = TimerManagerRoot.Instance.Service;
        var timeManager = MMTimeManager.Instance;  // Feel's singleton

        // Create a game timer
        var timerId = TimerId.New();
        timerService.TryStartTimer(timerId, 10f, false, out _);
    }

    private void Update()
    {
        // TimerManager ticks automatically from TimerManagerRoot.Update()
        // MMTimeManager ticks from its own update
        // Both are independent
    }
}
```

### Step 3: Approach B - Synchronized Time

Create a bridge that synchronizes TimerManager with MMTimeManager:

**File**: `Assets/Systems/TimerManager/Integration/MMTimeManagerBridge.cs`

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Synchronizes TimerManager with MMTimeManager (Feel framework).
    ///
    /// This bridge ensures that TimerManager respects Feel's time scale,
    /// freeze, and slow-mo effects.
    ///
    /// Design: Polls MMTimeManager's current time scale each frame
    /// and applies it to TimerManager's TimeContext.
    /// </summary>
    public sealed class MMTimeManagerBridge : MonoBehaviour
    {
        private float _lastKnownTimeScale = 1f;
        private ITimerService _timerService;

        private void OnEnable()
        {
            // Get timer service
            _timerService = TimerManagerRoot.Instance?.Service;
            if (_timerService == null)
            {
                Debug.LogError("TimerManager not initialized. Add TimerManagerRoot to scene.");
                enabled = false;
                return;
            }

            // Register for Feel's time scale changes
            // Note: Feel uses its own event system, adjust as needed
            if (MMTimeManager.Instance != null)
            {
                MMTimeManager.Instance.OnTimeScaleChangedEvent.AddListener(HandleMMTimeScaleChanged);
            }
        }

        private void OnDisable()
        {
            if (MMTimeManager.Instance != null)
            {
                MMTimeManager.Instance.OnTimeScaleChangedEvent.RemoveListener(HandleMMTimeScaleChanged);
            }
        }

        private void Update()
        {
            if (_timerService == null || MMTimeManager.Instance == null)
                return;

            // Poll MMTimeManager's current time scale
            float mmTimeScale = MMTimeManager.Instance.CurrentTimeScale;

            // Update TimerManager if MMTimeManager's scale changed
            if (!Mathf.Approximately(mmTimeScale, _lastKnownTimeScale))
            {
                _timerService.SetTimeScale(mmTimeScale);
                _lastKnownTimeScale = mmTimeScale;
            }

            // Optional: Sync pause state
            // if (MMTimeManager.Instance.FramePausedByInputManager)
            // {
            //     _timerService.Pause();
            // }
            // else
            // {
            //     _timerService.Resume();
            // }
        }

        /// <summary>
        /// Called by MMTimeManager when its time scale changes.
        /// </summary>
        private void HandleMMTimeScaleChanged(float newTimeScale)
        {
            if (_timerService != null)
            {
                _timerService.SetTimeScale(newTimeScale);
                _lastKnownTimeScale = newTimeScale;
            }
        }
    }
}
```

**Integration Steps**:

1. Create empty GameObject: `TimerManager-MMBridge`
2. Add `MMTimeManagerBridge` component
3. Ensure both `TimerManagerRoot` and `MMTimeManager` exist in scene
4. That's it! Now TimerManager respects Feel's time controls

---

## Specific Integration Scenarios

### Scenario 1: Slow-Motion Ability

```csharp
public class SlowMotionAbility : MonoBehaviour
{
    [SerializeField] private float slowScale = 0.2f;  // 80% slower
    [SerializeField] private float slowDuration = 3f;

    public void ActivateSlowMo()
    {
        // Approach A: Only affect visuals/physics
        // MMTimeManager.Instance.SetTimeScale(slowScale);

        // Approach B: Both visuals and game timers slow down
        TimerManagerRoot.Instance.Service.SetTimeScale(slowScale);

        // Schedule returning to normal
        var slowTimerId = TimerId.New();
        TimerManagerRoot.Instance.Service.TryStartTimer(
            slowTimerId, slowDuration, false, out _
        );

        EventChannel<TimerCompletedEvent>.OnEvent += (evt) =>
        {
            if (evt.TimerId == slowTimerId)
            {
                TimerManagerRoot.Instance.Service.SetTimeScale(1f);
                // Or: MMTimeManager.Instance.SetTimeScale(1f);
            }
        };
    }
}
```

### Scenario 2: Freeze/Unfreeze

Feel's freeze is different from TimerManager's pause:

```csharp
// Feel's freeze (frame freezes completely)
MMTimeManager.Instance.Freeze(duration: 0.2f);

// TimerManager's pause (timers pause, but time advances normally)
TimerManagerRoot.Instance.Service.Pause();

// Use both for combined effect:
// - Freeze stops rendering
// - Pause prevents timer ticks
```

### Scenario 3: Fast-Forward Replays

```csharp
public class ReplaySystem : MonoBehaviour
{
    public void PlayReplayAtSpeed(float speed)
    {
        // Approach B only (if synchronized):
        TimerManagerRoot.Instance.Service.SetTimeScale(speed);

        // Then play animation or events at 2x speed
        // Cinematics, input replays, etc.
    }
}
```

### Scenario 4: Pauseable Countdown UI

```csharp
public class PauseableCountdown : MonoBehaviour
{
    [SerializeField] private Text countdownText;
    private TimerId _countdownId;
    private ITimerService _service;

    private void Start()
    {
        _service = TimerManagerRoot.Instance.Service;
        _countdownId = TimerId.New();
        _service.TryStartCountdown(_countdownId, 30f, out _);

        EventChannel<TimerTickEvent>.OnEvent += OnTick;
    }

    private void OnTick(TimerTickEvent evt)
    {
        if (evt.TimerId == _countdownId)
        {
            countdownText.text = evt.Remaining.ToString("F1");
        }
    }

    public void PauseCountdown()
    {
        // Both approaches:
        // Approach A: Only pause timer
        _service.TryPauseTimer(_countdownId, out _);

        // Approach B: Would also pause via time scale
        // (but Pause() is cleaner for individual timers)
    }
}
```

---

## Advanced: Custom Time Provider

If you want even more control, create a custom `ITimeSource` that reads from MMTimeManager:

**File**: `Assets/Systems/TimerManager/Core/Time/MMTimeSource.cs` (optional)

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Optional: Custom time source that gets time from MMTimeManager.
    /// Only use this if you want TimerManager's time model to depend on Feel.
    ///
    /// Not recommended - creates tight coupling.
    /// Use MMTimeManagerBridge instead.
    /// </summary>
    public sealed class MMTimeSource : ITimeSource
    {
        public GameTime GetCurrentTime()
        {
            if (MMTimeManager.Instance == null)
                return new GameTime(0, 0, 0, 1f, false);

            var mm = MMTimeManager.Instance;
            return new GameTime(
                tick: mm.FrameCount,
                delta: Time.deltaTime * mm.CurrentTimeScale,
                unscaledDelta: Time.deltaTime,
                timeScale: mm.CurrentTimeScale,
                isPaused: mm.FramePausedByInputManager || !mm.GameRunning
            );
        }
    }
}
```

**Note**: This approach is **not recommended** because:
- Tight coupling to Feel
- Makes TimerManager untestable without Feel
- Adds Feel as hard dependency

---

## Decision Matrix

Choose your integration approach:

| Need | Approach A | Approach B |
|------|-----------|-----------|
| Simple slow-motion effects | ✅ | ✅ |
| Separate game/visual time | ✅ | ❌ |
| Synchronized time scale | ❌ | ✅ |
| Minimal Feel dependency | ✅ | ❌ |
| Easy testing | ✅ | ❌ |
| Single time source | ❌ | ✅ |

---

## Troubleshooting

### Problem: Timers not slowing down with Feel effects

**Cause**: Using Approach A (independent systems)

**Solution**: Either:
1. Accept that game logic doesn't slow (timers are independent)
2. Switch to Approach B with MMTimeManagerBridge

### Problem: MMTimeManagerBridge doesn't work

**Cause**: MMTimeManager event naming may vary by Feel version

**Solution**: Check Feel's source code for correct event names:
```csharp
// Different Feel versions use different event names:
// - OnTimeScaleChangedEvent
// - OnTimescaleChangeEvent
// - etc.

// Or use polling in Update() instead (more reliable):
float currentScale = MMTimeManager.Instance.CurrentTimeScale;
```

### Problem: Freeze doesn't work as expected

**Cause**: Mixing Feel's freeze with TimerManager's pause

**Solution**: Use the right tool:
- Freeze: Use `MMTimeManager.Freeze()` for visual lock
- Pause: Use `_service.Pause()` for logical pause

### Problem: Circular update dependency

**Cause**: Both systems trying to drive each other

**Solution**: Make one authoritative:
- Option 1: TimerManager drives all (using MMTimeManagerBridge)
- Option 2: MMTimeManager drives all (using custom ITimeSource)
- Option 3: Keep separate (no bridge)

---

## Performance Considerations

### Approach A (Independent)
- **Minimal overhead** - Two independent systems
- **No synchronization** - No polling needed
- **Best performance** for large timer counts

### Approach B (Synchronized)
- **Slight overhead** - Bridge polls MM each frame
- **Synchronization cost** - Negligible (one poll per frame)
- **Still fast** - O(1) operation

**Recommendation**: Approach B has negligible performance impact. Use it if synchronization is needed.

---

## Version Compatibility

This guide targets:
- **Feel** (More Mountains): 2.0+ (adjust event names for older versions)
- **TimerManager**: 1.0+
- **Unity**: 2021.3+

For other Feel versions, check the MMTimeManager source code and adjust event/method names accordingly.

---

## Migration Guide

### From Pure MMTimeManager to TimerManager

If you're currently using only Feel's time system and want to migrate:

**Before**:
```csharp
MMTimeManager.Instance.SetTimeScale(0.5f);  // For everything
```

**After - Approach A**:
```csharp
// Separate control:
// - Visual: MMTimeManager.SetTimeScale(0.5f)
// - Game logic: TimerManagerRoot.Instance.Service.SetTimeScale(0.5f)
```

**After - Approach B**:
```csharp
// Single control:
TimerManagerRoot.Instance.Service.SetTimeScale(0.5f);
// MMTimeManagerBridge syncs automatically
```

---

## Summary

| Aspect | Approach A | Approach B |
|--------|-----------|-----------|
| **Complexity** | Simple | Moderate |
| **Coupling** | Low | Medium |
| **Synchronization** | Manual | Automatic |
| **Recommended For** | Most projects | Projects needing tight sync |
| **Setup Time** | 5 min | 10 min |

**Default Recommendation**: **Approach A** - Keep systems independent unless you have a specific need for synchronization.

Enjoy the flexibility! 🎮
