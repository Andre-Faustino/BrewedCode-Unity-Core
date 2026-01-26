# BrewedCode Events System

A lightweight, type-safe event system for Unity with support for scoped event channels.

## Features

- **Type-safe events**: Generic event channels ensure compile-time type safety
- **Scoped channels**: Events can be Global, Scene-scoped, or Instance-scoped
- **Backward compatible**: Existing global event code continues to work
- **Auto scene cleanup**: Scene-scoped channels are automatically cleared on scene unload
- **Editor debug window**: Inspect active channels, view event history, and filter by type

---

## Architecture

```
EventSystem/
├── Core/
│   ├── EventChannel<T>        # Generic event publisher
│   ├── EventChannelRegistry   # Channel factory and cache
│   ├── EventRegister          # Extension methods for listening
│   ├── EventScopeKey          # Immutable scope identifier
│   ├── ChannelKey             # Composite key (Type + Scope)
│   └── IEventScope            # Interface for event emitters
├── Scopes/
│   └── SceneScopeProvider     # Auto scene lifecycle management
├── Debug/
│   ├── EventDebugCapture      # Editor-only event recording
│   └── EventDispatchRecord    # Captured event data
├── Editor/
│   └── EventSystemWindow      # Debug window UI
└── Tests/
    └── *Tests.cs              # Unit tests
```

### Core Components

| Component | Purpose |
|-----------|---------|
| `EventChannel<T>` | Holds subscribers and raises events |
| `EventChannelRegistry` | Manages channel instances by type and scope |
| `EventListener<T>` | Interface for receiving events |
| `EventScopeKey` | Identifies a scope (Global, Scene, or Instance) |
| `IEventScope` | Interface for objects that emit scoped events |

---

## Quick Start

### 1. Define an Event

Events are simple structs:

```csharp
public struct PlayerDamagedEvent
{
    public int Damage;
    public GameObject Source;
}
```

### 2. Create a Listener

Implement `EventListener<T>` on your MonoBehaviour:

```csharp
using BrewedCode.Events;
using UnityEngine;

public class HealthUI : MonoBehaviour, EventListener<PlayerDamagedEvent>
{
    private void OnEnable()
    {
        this.EventStartListening<PlayerDamagedEvent>();
    }

    private void OnDisable()
    {
        this.EventStopListening<PlayerDamagedEvent>();
    }

    public void OnEvent(PlayerDamagedEvent e)
    {
        Debug.Log($"Player took {e.Damage} damage from {e.Source.name}");
    }
}
```

### 3. Trigger an Event

```csharp
// From anywhere in your code:
EventChannel<PlayerDamagedEvent>.Trigger(new PlayerDamagedEvent
{
    Damage = 10,
    Source = gameObject
});
```

---

## Scoped Events

### Why Scopes?

Global events broadcast to all listeners. Scoped events let you target specific contexts:

- **Scene scope**: Events only reach listeners in the same scene
- **Instance scope**: Events only reach listeners subscribed to a specific object

### Scene-Scoped Events

```csharp
// Get the current scene's scope
var sceneScope = SceneScopeProvider.GetCurrentSceneScopeKey();

// Listen to scene-scoped events
this.EventStartListening<EnemySpawnedEvent>(sceneScope);

// Trigger in scene scope
EventChannel<EnemySpawnedEvent>.Trigger(data, sceneScope);
```

Scene channels are **automatically cleared** when the scene unloads.

### Instance-Scoped Events

Create an event emitter by implementing `IEventScope`:

```csharp
public class Turret : MonoBehaviour, IEventScope
{
    private EventScopeKey _scopeKey;
    public EventScopeKey ScopeKey => _scopeKey;

    private void Awake()
    {
        _scopeKey = EventScopeKey.ForInstance(GetInstanceID());
    }

    public void Fire()
    {
        EventChannel<TurretFiredEvent>.Trigger(new TurretFiredEvent(), this);
    }
}
```

Subscribe to a specific instance:

```csharp
public class TurretUI : MonoBehaviour, EventListener<TurretFiredEvent>
{
    [SerializeField] private Turret _turret;

    private void OnEnable()
    {
        this.EventStartListening<TurretFiredEvent>(_turret);
    }

    private void OnDisable()
    {
        this.EventStopListening<TurretFiredEvent>(_turret);
    }

    public void OnEvent(TurretFiredEvent e)
    {
        // Only receives events from _turret, not other turrets
    }
}
```

---

## API Reference

### EventChannel<T>

```csharp
// Static trigger methods
EventChannel<T>.Trigger(T data);                    // Global scope
EventChannel<T>.Trigger(T data, EventScopeKey key); // Scoped
EventChannel<T>.Trigger(T data, IEventScope scope); // Scoped via interface

// Instance methods (advanced use)
channel.RaiseEvent(T data);
channel.OnEventRaised += handler;  // Direct subscription
```

### EventRegister Extensions

```csharp
// Global scope
this.EventStartListening<T>();
this.EventStopListening<T>();

// Scoped by key
this.EventStartListening<T>(EventScopeKey key);
this.EventStopListening<T>(EventScopeKey key);

// Scoped by emitter
this.EventStartListening<T>(IEventScope scope);
this.EventStopListening<T>(IEventScope scope);
```

### EventScopeKey

```csharp
EventScopeKey.Global                      // Broadcast scope
EventScopeKey.ForScene(int sceneHandle)   // Scene scope
EventScopeKey.ForInstance(int instanceId) // Instance scope

// Properties
key.Type      // EventScopeType enum
key.Id        // Internal identifier
key.IsGlobal  // True if Global scope
```

### SceneScopeProvider

```csharp
SceneScopeProvider.GetCurrentSceneScopeKey()       // Active scene
SceneScopeProvider.GetSceneScopeKey(Scene scene)   // Specific scene
SceneScopeProvider.GetSceneScopeKey(string name)   // By name
```

---

## Best Practices

### 1. Always Unsubscribe

```csharp
private void OnDisable()
{
    this.EventStopListening<MyEvent>();
}
```

Forgetting to unsubscribe causes memory leaks and ghost callbacks.

### 2. Use Structs for Events

```csharp
// Good - no allocation
public struct DamageEvent { public int Amount; }

// Avoid - allocates on every trigger
public class DamageEvent { public int Amount; }
```

### 3. Keep Events Small

Include only the data receivers need. Large events with unused fields waste memory.

### 4. Prefer Instance Scope for Component Communication

When an object needs to broadcast to its own UI/effects, use instance scope instead of global + filtering.

### 5. Use Scene Scope for Level-Specific Systems

Scene-scoped events prevent cross-scene pollution in additive loading scenarios.

---

## Debug Window

Open via menu: **BrewedCode > Events**

### Tabs

| Tab | Description |
|-----|-------------|
| **Channels** | Active channels with listener counts |
| **History** | Event dispatch log (requires capture enabled) |
| **Inspector** | Selected GameObject's event scope info |

### Toolbar

- **REC**: Toggle event capture (red when recording)
- **Filter**: Text search for event types
- **Types**: Multi-select filter by event type
- **Global/Scene/Instance**: Scope type filters
- **Clear**: Clear history
- **Refresh**: Refresh channel list

### Viewing Event Data

1. Enable capture (click REC)
2. Trigger events in play mode
3. Switch to History tab
4. Click a row to see event details and JSON body
5. Use "Copy JSON" to copy event data

---

## Examples

### Global Broadcast

```csharp
// Event
public struct GamePausedEvent { public bool IsPaused; }

// Listener
public class AudioManager : MonoBehaviour, EventListener<GamePausedEvent>
{
    void OnEnable() => this.EventStartListening<GamePausedEvent>();
    void OnDisable() => this.EventStopListening<GamePausedEvent>();

    public void OnEvent(GamePausedEvent e)
    {
        AudioListener.pause = e.IsPaused;
    }
}

// Trigger
EventChannel<GamePausedEvent>.Trigger(new GamePausedEvent { IsPaused = true });
```

### Enemy Death in Scene

```csharp
// Event
public struct EnemyDiedEvent
{
    public Vector3 Position;
    public int ScoreValue;
}

// Spawn VFX only for enemies in current scene
public class DeathVFXSpawner : MonoBehaviour, EventListener<EnemyDiedEvent>
{
    private EventScopeKey _sceneScope;

    void OnEnable()
    {
        _sceneScope = SceneScopeProvider.GetCurrentSceneScopeKey();
        this.EventStartListening<EnemyDiedEvent>(_sceneScope);
    }

    void OnDisable() => this.EventStopListening<EnemyDiedEvent>(_sceneScope);

    public void OnEvent(EnemyDiedEvent e)
    {
        Instantiate(vfxPrefab, e.Position, Quaternion.identity);
    }
}

// Enemy triggers with scene scope
EventChannel<EnemyDiedEvent>.Trigger(data, SceneScopeProvider.GetCurrentSceneScopeKey());
```

### Instance-Specific UI

```csharp
// Health bar that tracks a specific unit
public class UnitHealthBar : MonoBehaviour, EventListener<HealthChangedEvent>
{
    [SerializeField] private Unit _unit;

    void OnEnable() => this.EventStartListening<HealthChangedEvent>(_unit);
    void OnDisable() => this.EventStopListening<HealthChangedEvent>(_unit);

    public void OnEvent(HealthChangedEvent e)
    {
        UpdateBar(e.CurrentHealth, e.MaxHealth);
    }
}
```

---

## Troubleshooting

### Events not received

1. Check `OnEnable`/`OnDisable` pairing
2. Verify scope matches between trigger and listener
3. Enable debug capture and check History tab

### Multiple events received

1. Ensure `EventStopListening` is called in `OnDisable`
2. Check if subscribed multiple times (e.g., in `Start` instead of `OnEnable`)

### Scene events not clearing

- Scene scope cleanup is automatic via `SceneScopeProvider`
- Verify scene is actually unloaded (not just deactivated)

---

## License

Internal use - BrewedCode
