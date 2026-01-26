# TimerManager System Architecture

## Overview

The TimerManager is a deterministic, event-driven time management system designed for complete independence from Unity's runtime. It serves as the temporal foundation for the game, managing all timer-based gameplay mechanics.

**Core Principle**: Pure C# business logic with a thin Unity bootstrap layer.

---

## Design Philosophy

### 1. Determinism First
- Time advances deterministically without dependency on `Time.deltaTime`
- Frame counter (`Tick`) is monotonically increasing
- Time scaling and pause state are explicit and traceable
- No hidden state or random behavior

### 2. Pure C# Core
- Zero Unity dependencies in `Core/` namespace (except `AnimationCurve` in TweenTimer)
- All business logic is 100% unit testable without Unity runtime
- Time model is a first-class citizen, not a side effect

### 3. Event-Driven Communication
- No direct callbacks or delegates in the service layer
- All state changes publish events via `IEventBus`
- Systems react to events, not to direct method calls
- Complete separation of concerns

### 4. Strong Typing
- Value objects (`TimerId`, `GameTime`) for domain entities
- Read-only DTOs (`TimerInfo`) for queries
- No primitive obsession or magic numbers
- Type safety enforced at compile time

---

## Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                    Unity Integration                     │
│              (TimerManagerRoot, Callbacks)               │
├─────────────────────────────────────────────────────────┤
│                    Public API Layer                      │
│              (ITimerService - TryX pattern)              │
├─────────────────────────────────────────────────────────┤
│                  Business Logic Layer                    │
│        (TimerService, TimerScheduler, Timers)           │
├─────────────────────────────────────────────────────────┤
│                    Domain Model Layer                    │
│    (Timer types, GameTime, TimeContext, TimerId)        │
├─────────────────────────────────────────────────────────┤
│                 Event Publishing Layer                   │
│              (IEventBus, Event Classes)                  │
└─────────────────────────────────────────────────────────┘
```

---

## Core Components

### Time Model (`Core/Time/`)

#### GameTime (Value Object)
```csharp
public readonly struct GameTime
{
    public long Tick;           // Frame counter, monotonically increasing
    public float Delta;         // Scaled delta time (respects TimeScale)
    public float UnscaledDelta; // Raw delta time (ignores TimeScale)
    public float TimeScale;     // Current time scale multiplier (1.0 = normal)
    public bool IsPaused;       // Whether time is paused
}
```

**Purpose**: Immutable snapshot of time state passed to each frame tick.
**Why**: Enables deterministic replay and testing. Time doesn't change mid-frame.

#### TimeContext (State Manager)
- Maintains current tick counter
- Applies time scaling
- Handles pause/resume state
- Produces `GameTime` snapshots

**Key Methods**:
- `Advance(deltaTime)` - Produces next GameTime snapshot
- `SetTimeScale(scale)` - Multiplies all subsequent deltas
- `Pause()` / `Resume()` - Freeze time globally

### Timer Domain (`Core/Timers/`)

#### TimerBase (Abstract Base)
Defines the timer contract:
- **State Properties**: `IsRunning`, `IsPaused`, `IsCompleted`, `IsLooping`
- **Progress Properties**: `Elapsed`, `Remaining`, `Progress` (0.0 - 1.0)
- **Lifecycle Methods**: `Start()`, `Pause()`, `Resume()`, `Stop()`
- **Abstract Methods**: `Advance(delta)`, `Reset()`

#### Four Timer Types

**1. Timer (Basic Counter)**
```
Time: 0 ──────[Advance]────── Duration
      └─ Elapsed increases ─┘
```
- Counts up from 0 to duration
- Supports looping (wraps around)
- Use for: countup timers, animation timers, gameplay duration

**2. CountdownTimer**
```
Time: Duration ────[Advance]──── 0
      └─ Remaining decreases ─┘
```
- Countdown from duration to 0
- Reports remaining time visually (HUD display)
- Use for: countdowns, ability cooldowns, UI timers

**3. CooldownTimer**
```
Time: 0 ──[Advance]── Duration ──[Ready State]──
      └─────────── Consume() ──────────┘
```
- Countdown with explicit ready state
- `IsReady` property indicates availability
- `Consume()` resets and restarts
- Use for: ability cooldowns, attack cooldowns, resource generation

**4. TweenTimer (Interpolation)**
```
Time: 0 ──────[Advance]────── Duration
      └─[Curve Evaluation]─┘
      EvaluatedValue ∈ [0, 1]
```
- Interpolates along an `AnimationCurve`
- Provides smooth easing (ease-in, ease-out, custom)
- `EvaluatedValue` property is curve output
- Use for: smooth transitions, animated values, eased movements

### Scheduling Engine (`Core/`)

#### TimerScheduler (Internal)
- Stores timers in `Dictionary<TimerId, TimerBase>`
- Maintains insertion order for deterministic iteration
- `AdvanceAll(delta)` updates all running timers
- Returns list of completed timer IDs for event publishing

**Key Invariant**: Dictionary iteration order is stable (C# 7.0+).

#### TimerService (Implementation)
- Implements `ITimerService` public interface
- Manages lifecycle (create, pause, resume, stop)
- Validates operations (duration > 0, timer exists, etc.)
- Publishes events for all state changes
- **Three-part tick**:
  1. Advance `TimeContext` to get current `GameTime`
  2. Advance all timers via scheduler
  3. Publish events for tick and completion

**Design Pattern**: Service layer is pure domain logic, event bus is dependency injected.

### Event System

#### IEventBus (Local Interface)
```csharp
public interface IEventBus
{
    void Publish<T>(T evt);
    IDisposable Subscribe<TEvent>(Action<TEvent> handler);
}
```

**Why Local**: Each system defines its own IEventBus. This prevents cross-system coupling.

#### Event Lifecycle

```
TryStartTimer() → [Publish TimerStartedEvent]
     ↓
Tick() → [Publish TimerTickEvent per running timer]
     ↓
Timer Completes → [Publish TimerCompletedEvent]
     ↓
TryStopTimer() → [Publish TimerCancelledEvent]

TryPauseTimer() → [Publish TimerPausedEvent]
TryResumeTimer() → [Publish TimerResumedEvent]
```

### Bootstrap (`Bootstrap/`)

#### TimerManagerRoot
```csharp
[DefaultExecutionOrder(-100)]  // Runs before normal Update
public sealed class TimerManagerRoot
    : PersistentMonoSingleton<TimerManagerRoot>
```

**Responsibilities**:
1. Initializes `TimerService` with event bus
2. Calls `Tick(Time.deltaTime)` in `Update()`
3. Provides singleton access via `Instance.Service`

**Execution Order**: `-100` ensures timers tick before other systems.

**Why Singleton**: Games need global time access. This is acceptable at the bootstrap layer.

---

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        TimerManagerRoot                         │
│                   (Singleton Bootstrap)                         │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             │ Update() each frame
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│                      TimerService.Tick()                        │
│  1. TimeContext.Advance(Time.deltaTime)  → GameTime             │
│  2. TimerScheduler.AdvanceAll(delta)     → Completed IDs        │
│  3. Publish Events (Tick + Completion)                          │
└────────────────────────────┬────────────────────────────────────┘
                             │
                ┌────────────┼────────────┐
                ↓            ↓            ↓
        ┌────────────┐  ┌────────────┐  ┌──────────────┐
        │  Timers    │  │TimeContext │  │  EventBus    │
        │ Advanced   │  │  Manages   │  │   Publishes  │
        │            │  │  Scale,    │  │   Events     │
        │            │  │  Pause     │  │              │
        └────────────┘  └────────────┘  └──────────────┘
                             │
                ┌────────────┴────────────┐
                ↓                         ↓
        ┌────────────────────┐  ┌────────────────────────┐
        │ Systems listening  │  │ UI/FX listening        │
        │ to events          │  │ to events              │
        │ (Game logic)       │  │ (Visual feedback)      │
        └────────────────────┘  └────────────────────────┘
```

---

## Key Patterns

### 1. TryX(out string error)

All mutable operations follow this pattern:

```csharp
bool TryStartTimer(TimerId id, float duration, bool isLooping, out string error)
{
    error = "";

    // Validation
    if (duration <= 0f) {
        error = "Duration must be positive.";
        return false;
    }

    if (_scheduler.GetTimer(id) != null) {
        error = $"Timer {id} already exists.";
        return false;
    }

    // Operation
    var timer = new Timer(id, duration, isLooping);
    timer.Start();
    _scheduler.AddTimer(timer);

    // Event
    _eventBus.Publish(new TimerStartedEvent { TimerId = id, Duration = duration });

    return true;
}
```

**Benefits**:
- No exceptions for expected failures
- Error messages are actionable
- Caller controls error handling
- Pure determinism (no try-catch overhead)

### 2. Value Objects (TimerId)

```csharp
public readonly struct TimerId : IEquatable<TimerId>
{
    private readonly Guid _value;

    public static TimerId New() => new(Guid.NewGuid());
    // ...
}
```

**Why**:
- Type safety (can't mix TimerId with other Guids)
- Equality semantics (two TimerIds with same Guid are equal)
- Value semantics (no reference issues, no null)

### 3. Read-Only DTOs (TimerInfo)

```csharp
public sealed class TimerInfo
{
    public TimerId Id { get; }
    public string Type { get; }
    public float Duration { get; }
    public float Elapsed { get; }
    // ...
}
```

**Why**:
- Queries don't expose mutable timers
- Snapshots are consistent (no mid-query changes)
- Clear API boundary

### 4. Immutable Snapshots (GameTime)

```csharp
public readonly struct GameTime
{
    public readonly long Tick;
    public readonly float Delta;
    // ...
}
```

**Why**:
- Time can't change mid-frame
- Enables deterministic replay
- No aliasing issues

---

## Determinism Guarantees

### What is Deterministic

✅ Timer advancement (given same input delta, same result)
✅ Completion detection (same frame, same result)
✅ Event publishing order (dictionary iteration order is stable)
✅ Time scaling (multiplicative, not additive)
✅ Pause/resume behavior (consistent state)

### What is NOT Deterministic

❌ Frame timing (depends on hardware, OS scheduling)
❌ Guid generation (uses cryptographic randomness)
❌ Input timing (depends on user, network latency)

### Testing Determinism

```csharp
[Test]
public void AdvanceAll_DeterministicOrdering()
{
    var ids = new[] { TimerId.New(), TimerId.New(), TimerId.New() };
    foreach (var id in ids) {
        var timer = new Timer(id, 10f);
        timer.Start();
        _scheduler.AddTimer(timer);
    }

    _scheduler.AdvanceAll(1f);
    var order1 = new List<TimerBase>(_scheduler.GetAllTimers());

    _scheduler.AdvanceAll(1f);
    var order2 = new List<TimerBase>(_scheduler.GetAllTimers());

    // Same order every time
    Assert.AreEqual(order1.Count, order2.Count);
    for (int i = 0; i < order1.Count; i++) {
        Assert.AreEqual(order1[i].Id, order2[i].Id);
    }
}
```

---

## Extension Points

### Future: Visual Effects Integration

```csharp
public interface ITimeFXAdapter
{
    void OnTimerStarted(TimerId id);
    void OnTimerTick(TimerId id, float progress);
    void OnTimerCompleted(TimerId id);
}
```

**When to Implement**: Once Feel/DOTween integration is needed.

### Future: Persistence

```csharp
public sealed class TimerStateDTO
{
    public TimerId Id { get; set; }
    public string Type { get; set; }
    public float Duration { get; set; }
    public float Elapsed { get; set; }
    // ...
}
```

**When to Implement**: Game save/load system.

### Future: Debugging Tools

```csharp
public sealed class TimerDebugWindow : EditorWindow
{
    // List all active timers
    // Show progress bars
    // Manual time control
    // Breakpoint on completion
}
```

---

## Assembly Organization

```
BrewedCode.TimerManager
├── References: BrewedCode.Events, BrewedCode.Singleton
├── Namespace: BrewedCode.TimerManager
├── Core logic is pure C#
└── Bootstrap depends on Unity

BrewedCode.TimerManager.Tests
├── References: TimerManager, NUnit, UnityEngine.TestRunner
├── Editor-only assembly
├── No Mock frameworks (manually implemented)
└── Tests are deterministic and fast
```

---

## Performance Characteristics

| Operation | Complexity | Notes |
|-----------|-----------|-------|
| StartTimer | O(1) | Dictionary insert |
| StopTimer | O(1) | Dictionary remove |
| GetTimer | O(1) | Dictionary lookup |
| AdvanceAll | O(n) | Iterate all timers |
| Tick | O(n) | Service tick time |

**Optimization Notes**:
- No allocations during `Advance()` (uses value types)
- Event publishing is O(n subscribers) per event
- Consider object pooling if creating many timers frequently
- No GC pressure in hot path

---

## Summary

The TimerManager architecture prioritizes:
1. **Determinism** - Predictable, testable behavior
2. **Purity** - Business logic independent of Unity
3. **Events** - Loose coupling via messaging
4. **Strong Typing** - Compile-time safety
5. **Simplicity** - No hidden complexity

This makes it suitable as the temporal foundation for the entire game.
