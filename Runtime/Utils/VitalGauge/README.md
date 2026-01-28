# VitalGauge System

A generic, reusable gauge/meter system for managing values that change over time (health, oxygen, stamina, energy, etc.). Built on pure C# with zero Unity dependencies in the core, making it highly testable and portable.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [API Reference](#api-reference)
- [Events](#events)
- [Advanced Usage](#advanced-usage)
- [Examples](#examples)
- [Testing](#testing)

---

## Overview

VitalGauge provides a framework for tracking numeric values with state management. It's designed to be:

- **Reusable**: Not tied to any specific game system (works for health, oxygen, stamina, etc.)
- **Testable**: Pure C# core with no Unity dependencies
- **Event-Driven**: Publishes events for UI updates, logic triggers, etc.
- **Flexible**: Supports manual updates or automatic time-based draining/regeneration
- **Performant**: Minimal allocations, no GetComponent calls

## Features

✓ **State Management** - Tracks gauge state (Normal, Low, Empty, Full)
✓ **Edge-Trigger Events** - Low/Empty/Full events fire only once per state entry
✓ **Tick Injection** - Automatic updates via ITickSource or manual Tick(dt) calls
✓ **Floating-Point Safety** - Epsilon-based boundary detection (no floating-point edge cases)
✓ **Threshold Customization** - Define "Low" state threshold (0-1 normalized)
✓ **Event Publishing** - Full pub/sub via IEventBus (bridges to EventChannel<T>)
✓ **Dependency Injection** - All dependencies injected, no singletons or FindObjectOfType

---

## Architecture

```
Assets/Systems/VitalGauge/
├── Contracts/
│   ├── ITickSource.cs    → Interface for time/tick updates
│   └── IEventBus.cs      → Interface for event publishing
├── Core/
│   ├── VitalGauge.cs     → Main gauge logic (pure C#)
│   └── GaugeConfig.cs    → Configuration data class
└── Events/
    ├── GaugeState.cs     → State enum (Normal, Low, Empty, Full)
    ├── GaugeChanged.cs   → Published on value changes
    ├── GaugeBecameLow.cs → Edge-trigger event
    ├── GaugeBecameEmpty.cs → Edge-trigger event
    ├── GaugeBecameFull.cs → Edge-trigger event
    └── GaugeStateChanged.cs → State transition event
```

### Key Design Decisions

**Pure C# Core**: VitalGauge.cs has zero Unity dependencies - it's a pure domain model. This means:
- Fully unit testable without mocking Unity
- Can be ported to other game engines
- Fast iteration and clear dependencies

**ITickSource Abstraction**: Allows decoupled time updates:
- Multiple gauge instances share one tick source
- Easy to test with MockTickSource
- Can switch between scaled/unscaled time at runtime

**IEventBus Abstraction**: Enables flexible event routing:
- VitalGauge remains event-bus-agnostic
- Bridges to any event system (EventChannel<T>, UnityEvents, custom bus, etc.)
- Easy to add middleware or logging

**Edge-Trigger Events**: Low/Empty/Full events fire only on entry:
- Prevents event spam (no repeated events while in same state)
- Simplifies game logic (no need to track "was I already low?")
- GaugeStateChanged always fires on state transitions

---

## Installation

VitalGauge is already set up in this project:

```
Assets/Systems/VitalGauge/         ← Generic core (can be reused in other projects)
Assets/_Project/Scripts/Characters/Oxygen/  ← Project-specific Oxygen implementation
```

To add a new gauge type (e.g., Stamina), create a similar folder structure:

```
Assets/_Project/Scripts/Characters/Stamina/
├── Data/
│   └── StaminaBalance.cs
├── Time/
│   └── (reuse MMTimeManagerTickSource)
├── Runtime/
│   ├── StaminaFeature.cs
│   └── StaminaController.cs
└── Integration/
    └── (reuse UnityEventChannelBus)
```

---

## Quick Start

### 1. Create OxygenBalance Asset

In the Unity Editor:
1. Right-click in Project → Create → Verdana/Oxygen/Balance
2. Set Max: 100, Start: 100, DrainPerSecond: 5, LowThreshold01: 0.25
3. Save as "OxygenBalance"

### 2. Add to Character

Add these components to your character GameObject:

1. **MMTimeManagerTickSource** - Provides time ticks
2. **OxygenController** - Orchestrates the oxygen system
   - Assign the OxygenBalance asset in the Inspector

### 3. Listen to Events

```csharp
// Subscribe to oxygen events
EventChannel<GaugeChanged>.AddListener(this);

public class MyUIManager : MonoBehaviour, EventListener<GaugeChanged>
{
    public void OnEvent(GaugeChanged evt)
    {
        if (evt.Id == "oxygen")
        {
            // Update oxygen UI slider
            oxygenSlider.value = evt.Normalized;
        }
    }
}
```

### 4. Access Oxygen

```csharp
var oxygenController = GetComponent<OxygenController>();
var oxygenFeature = oxygenController.Feature;

// Check oxygen level
if (oxygenFeature.State == GaugeState.Empty)
{
    // Character is suffocating!
    TakeDamage(10);
}

// Consume oxygen manually
oxygenFeature.Consume(5f);

// Refill oxygen
oxygenFeature.Refill(25f);
```

---

## API Reference

### VitalGauge Class

**Initialization:**
```csharp
void Init(GaugeConfig config)
```
Initializes gauge with configuration. Must be called before using the gauge.

**Value Management:**
```csharp
void SetMax(float value)        // Set maximum value
void SetCurrent(float value)    // Set current value
void SetRate(float ratePerSecond) // Set drain/regen rate
```

**Tick Updates:**
```csharp
void Tick(float dt)             // Manual tick update
void BindTicker(ITickSource)    // Bind to automatic tick source
void UnbindTicker()             // Unbind from tick source
```

**Properties:**
```csharp
string Id                       // Unique identifier
float Current                   // Current value
float Max                       // Maximum value
float Normalized                // Current / Max (0-1)
GaugeState State               // Current state (Normal, Low, Empty, Full)
```

### OxygenFeature Class

**Initialization:**
```csharp
void Init(OxygenBalance balance)
```
Initialize from OxygenBalance asset.

**Value Management:**
```csharp
void SetMax(float value)        // Set oxygen capacity
void SetCurrent(float value)    // Set oxygen amount
void SetRate(float ratePerSecond) // Set drain/regen rate
void Consume(float amount)      // Consume oxygen
void Refill(float amount)       // Regenerate oxygen
```

**Tick Management:**
```csharp
void BindTicker(ITickSource)    // Bind to tick source
void UnbindTicker()             // Unbind from tick source
void Tick(float dt)             // Manual tick
```

**Properties:**
```csharp
float Current                   // Current oxygen
float Max                       // Maximum oxygen
float Normalized                // Normalized oxygen (0-1)
GaugeState State               // Current state
```

### GaugeState Enum

```csharp
public enum GaugeState
{
    Normal,  // Above low threshold
    Low,     // Below low threshold but not empty
    Empty,   // At or near zero
    Full     // At or near max
}
```

---

## Events

All events use `readonly struct` for optimal performance. Subscribe via EventChannel<T>.

### GaugeChanged

Published **every time** the gauge value or max changes.

```csharp
public readonly struct GaugeChanged
{
    public string Id;           // Gauge identifier ("oxygen")
    public float Current;       // Current value
    public float Max;           // Maximum value
    public float Normalized;    // Current / Max (0-1)
}
```

**Usage:**
```csharp
EventChannel<GaugeChanged>.AddListener(this);

public void OnEvent(GaugeChanged evt)
{
    if (evt.Id == "oxygen")
    {
        oxygenSlider.value = evt.Normalized;
        oxygenText.text = $"{evt.Current:F0} / {evt.Max:F0}";
    }
}
```

### GaugeBecameLow

Published **once** when entering the Low state (edge-trigger).

```csharp
public readonly struct GaugeBecameLow
{
    public string Id;  // Gauge identifier
}
```

**Usage:**
```csharp
EventChannel<GaugeBecameLow>.AddListener(this);

public void OnEvent(GaugeBecameLow evt)
{
    if (evt.Id == "oxygen")
    {
        PlayLowOxygenWarning();
    }
}
```

### GaugeBecameEmpty

Published **once** when entering the Empty state (edge-trigger).

```csharp
public readonly struct GaugeBecameEmpty
{
    public string Id;
}
```

### GaugeBecameFull

Published **once** when entering the Full state (edge-trigger).

```csharp
public readonly struct GaugeBecameFull
{
    public string Id;
}
```

### GaugeStateChanged

Published whenever state transitions (Normal ↔ Low ↔ Empty, etc.).

```csharp
public readonly struct GaugeStateChanged
{
    public string Id;           // Gauge identifier
    public GaugeState From;     // Previous state
    public GaugeState To;       // New state
}
```

**Usage:**
```csharp
EventChannel<GaugeStateChanged>.AddListener(this);

public void OnEvent(GaugeStateChanged evt)
{
    if (evt.Id == "oxygen")
    {
        Debug.Log($"Oxygen state: {evt.From} → {evt.To}");
    }
}
```

---

## Advanced Usage

### Custom Tick Source

Implement ITickSource for custom timing logic:

```csharp
public class MyCustomTickSource : MonoBehaviour, ITickSource
{
    public event Action<float> OnTick;

    private void Update()
    {
        // Custom logic (e.g., only tick during specific game states)
        if (!GameState.IsPaused)
        {
            OnTick?.Invoke(Time.deltaTime);
        }
    }
}
```

### Custom Event Bus

Implement IEventBus for custom event routing:

```csharp
public class MyCustomEventBus : IEventBus
{
    public void Publish<TEvent>(TEvent evt)
    {
        // Log events
        Debug.Log($"Event: {typeof(TEvent).Name} - {evt}");

        // Route to different handlers
        EventChannel<TEvent>.Trigger(evt);
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        return new UnityEventChannelBus().Subscribe(handler);
    }
}
```

### Multiple Gauges

Create multiple gauge instances for different vital stats:

```csharp
public class CharacterVitals : MonoBehaviour
{
    private OxygenFeature _oxygen;
    private StaminaFeature _stamina;
    private HealthFeature _health;

    private void Start()
    {
        var eventBus = new UnityEventChannelBus();
        var tickSource = GetComponent<MMTimeManagerTickSource>();

        _oxygen = new OxygenFeature(eventBus);
        _oxygen.Init(_oxygenBalance);
        _oxygen.BindTicker(tickSource);

        _stamina = new StaminaFeature(eventBus);
        _stamina.Init(_staminaBalance);
        _stamina.BindTicker(tickSource);

        // etc.
    }
}
```

### Difficulty Scaling

Adjust drain rates based on difficulty:

```csharp
public void SetDifficulty(DifficultyLevel difficulty)
{
    float drainMultiplier = difficulty switch
    {
        DifficultyLevel.Easy => 0.5f,
        DifficultyLevel.Normal => 1.0f,
        DifficultyLevel.Hard => 1.5f,
        _ => 1.0f
    };

    var config = new GaugeConfig
    {
        Id = "oxygen",
        Max = 100f,
        Start = 100f,
        RatePerSecond = 5f * drainMultiplier,  // Scale drain
        LowThreshold01 = 0.25f
    };

    _oxygenFeature.Init(config);
}
```

### Pause Support

Pause oxygen drain when game is paused:

```csharp
public class PauseManager : MonoBehaviour
{
    private MMTimeManagerTickSource _tickSource;

    public void Pause()
    {
        Time.timeScale = 0;
        // MMTimeManagerTickSource will emit dt=0 (no tick fires)
    }

    public void Resume()
    {
        Time.timeScale = 1;
    }
}
```

---

## Examples

### Example 1: Simple Oxygen System

```csharp
public class SimpleOxygenExample : MonoBehaviour
{
    [SerializeField] private OxygenBalance oxygenBalance;
    private OxygenController _oxygenController;

    private void Start()
    {
        _oxygenController = GetComponent<OxygenController>();
    }

    private void OnTriggerStay(Collider collision)
    {
        if (collision.CompareTag("WaterVolume"))
        {
            // Drain oxygen while underwater
            _oxygenController.Feature.Consume(Time.deltaTime * 5);
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.CompareTag("WaterVolume"))
        {
            // Regenerate oxygen out of water
            _oxygenController.Feature.Refill(Time.deltaTime * 10);
        }
    }
}
```

### Example 2: UI Integration

```csharp
public class OxygenUIPanel : MonoBehaviour, EventListener<GaugeChanged>
{
    [SerializeField] private Slider oxygenSlider;
    [SerializeField] private Text oxygenText;
    [SerializeField] private Image oxygenFill;

    private void OnEnable()
    {
        this.EventStartListening<GaugeChanged>();
    }

    private void OnDisable()
    {
        this.EventStopListening<GaugeChanged>();
    }

    public void OnEvent(GaugeChanged evt)
    {
        if (evt.Id != "oxygen") return;

        oxygenSlider.value = evt.Normalized;
        oxygenText.text = $"{evt.Current:F0}";

        // Color shift: green (full) → yellow (low) → red (empty)
        oxygenFill.color = evt.Normalized > 0.25f ? Color.green : Color.red;
    }
}
```

### Example 3: Audio Feedback

```csharp
public class OxygenAudioFeedback : MonoBehaviour, EventListener<GaugeBecameLow>
{
    [SerializeField] private AudioClip lowOxygenWarning;

    private void OnEnable()
    {
        this.EventStartListening<GaugeBecameLow>();
    }

    private void OnDisable()
    {
        this.EventStopListening<GaugeBecameLow>();
    }

    public void OnEvent(GaugeBecameLow evt)
    {
        if (evt.Id == "oxygen")
        {
            AudioSource.PlayClipAtPoint(lowOxygenWarning, transform.position);
        }
    }
}
```

### Example 4: Game Logic Response

```csharp
public class OxygenGameLogic : MonoBehaviour, EventListener<GaugeBecameEmpty>
{
    private CharacterHealth _health;

    private void OnEnable()
    {
        this.EventStartListening<GaugeBecameEmpty>();
    }

    private void OnDisable()
    {
        this.EventStopListening<GaugeBecameEmpty>();
    }

    public void OnEvent(GaugeBecameEmpty evt)
    {
        if (evt.Id == "oxygen")
        {
            // Character is suffocating!
            _health.TakeDamage(50);
            Debug.Log("Character is suffocating!");
        }
    }
}
```

---

## Testing

Unit tests are located in `Assets/Tests/VitalGauge/`.

### Running Tests

In Unity Editor:
1. Window → General → Test Runner
2. Click "Run All" under PlayMode Tests
3. All VitalGaugeTests should pass

### Test Coverage

- ✓ Initialization and configuration
- ✓ Value management (SetMax, SetCurrent)
- ✓ Tick updates (manual and bound)
- ✓ State transitions (Normal → Low → Empty)
- ✓ Edge-trigger behavior (events fire only once per transition)
- ✓ Epsilon handling (floating-point safety)
- ✓ Event publishing (correct events published at correct times)
- ✓ Tick source binding/unbinding

### Test Utilities

Mock classes are provided for testing:

```csharp
var eventBus = new MockEventBus();
var tickSource = new MockTickSource();
var gauge = new VitalGauge(eventBus);

// Configure gauge...

// Emit tick
tickSource.EmitTick(1f);

// Verify events
var changedEvents = eventBus.GetEventsOfType<GaugeChanged>();
Assert.AreEqual(1, changedEvents.Count);
```

---

## Performance Considerations

- **Zero allocations per frame** (events are readonly structs, no boxing)
- **Single Update per tick source** (share one tick source across multiple gauges)
- **No GetComponent calls** (dependency injection)
- **No singletons** (easy to test and profile)

**Memory**: ~200 bytes per gauge instance (depends on CLR)

---

## Frequently Asked Questions

**Q: Can I have multiple gauges sharing one tick source?**
A: Yes! Create one `MMTimeManagerTickSource` and bind multiple gauge features to it.

**Q: Can I change the drain rate at runtime?**
A: Yes! Call `SetRate(newRate)` on the gauge/feature at any time.

**Q: What happens if Max = 0?**
A: Normalized becomes 0, state becomes Empty, gauge stops draining.

**Q: Do events fire even if value doesn't change?**
A: GaugeChanged fires if you call SetCurrent/SetMax. State events only fire on transition.

**Q: Can I use VitalGauge without oxygen?**
A: Yes! VitalGauge is generic. Create your own feature class (like OxygenFeature) for any gauge type.

---

## License & Credits

VitalGauge is part of Project Verdana.

Built with:
- Pure C# (no external dependencies)
- EventChannel<T> event system
- MMTimeManager for time management

---

## See Also

- [OxygenFeature](../Characters/Oxygen/Runtime/OxygenFeature.cs) - Example oxygen implementation
- [VitalGaugeTests](../../Tests/VitalGauge/VitalGaugeTests.cs) - Unit tests
