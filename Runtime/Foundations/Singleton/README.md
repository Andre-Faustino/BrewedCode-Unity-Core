# Singleton - BrewedCode Implementation

## Overview

This folder contains the **BrewedCode.Singleton** library - a local implementation of generic singleton patterns for MonoBehaviours and plain C# classes.

## What Changed

- **Before**: The project depended on the external NuGet package `UnityCommunity.UnitySingleton`
- **Now**: The singleton implementation is in this folder with the `BrewedCode.Singleton` namespace

## Files

- **MonoSingleton.cs** - Base singleton class for MonoBehaviours (instance destroyed on scene unload)
- **PersistentMonoSingleton.cs** - Persistent singleton class (survives scene loads via DontDestroyOnLoad, with optional unparenting)
- **Singleton.cs** - Generic singleton for plain C# classes (thread-safe, non-MonoBehaviour)
- **ISingleton.cs** - Interface defining singleton contract (IsInitialized, InitializeSingleton, ClearSingleton)
- **BrewedCode.Singleton.asmdef** - Assembly definition

## Namespace

Uses `BrewedCode.Singleton` namespace. All Systems assemblies have been updated to reference this namespace.

## Implementation Details

### MonoSingleton<T>

Base singleton pattern:
- Auto-creates if instance doesn't exist
- Destroys duplicate instances
- Thread-safe initialization
- Virtual `OnInitializing()` hook for setup

### PersistentMonoSingleton<T> : MonoSingleton<T>

Persistent variant:
- Inherits from MonoSingleton
- Uses `DontDestroyOnLoad()` to persist across scene loads
- Ideal for systems that should remain alive across all scenes

## Usage

### MonoBehaviour Singleton

```csharp
using BrewedCode.Singleton;

// Non-persistent (destroyed on scene unload)
public class MyController : MonoSingleton<MyController>
{
    protected override void OnInitializing()
    {
        // Called during initialization
    }

    protected override void OnInitialized()
    {
        // Called after initialization completes
    }
}

// Persistent (survives scene loads)
public class MySystem : PersistentMonoSingleton<MySystem>
{
    [SerializeField] private bool UnparentOnAwake = true; // Auto-detach from parent

    protected override void OnInitializing()
    {
        base.OnInitializing(); // Important: call base
        // Your initialization logic
    }
}
```

### Plain C# Singleton

```csharp
using BrewedCode.Singleton;

public class MyService : Singleton<MyService>
{
    protected override void OnInitializing()
    {
        // Called during initialization
    }

    protected override void OnInitialized()
    {
        // Called after initialization completes
    }
}

// Usage
var service = MyService.Instance;
```

## Assembly References

All Systems assemblies reference `BrewedCode.Singleton`:

- BrewedCode.Crafting
- BrewedCode.Events
- BrewedCode.ItemHub
- BrewedCode.Logging
- BrewedCode.ResourceBay
- BrewedCode.TimerManager
- BrewedCode.VitalGauge
- Plus their Editor and Test variants

## Migration Status

✅ **Complete** - All external dependencies removed, local BrewedCode.Singleton implementation active
