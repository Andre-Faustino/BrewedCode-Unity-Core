# BrewedCode.Utils

Modular utility library for Unity with three levels of dependencies:

## Structure

### `Core/` - Pure Core Utilities (**No External Dependencies**)
**Assembly:** `BrewedCode.Utils.asmdef` | **Namespace:** `BrewedCode.Utils`

Core utilities that can be used in any Unity project without additional external libraries.

#### Extensions
- **ArraysExtensions.cs** - Array manipulation (Add, AddRange, InsertAt, RemoveAt, IsNullOrEmpty)
- **EnumerableExtensions.cs** - IEnumerable filtering (WithNotNulls for classes and structs)
- **EnumExtensions.cs** - Enum name resolution
- **FuncionalExtensions.cs** - Kotlin-inspired functional methods with Unity fake-null awareness (let, run, also, apply, takeIf, takeUnless, OrElse, With)
- **IntExtensions.cs** - Integer iteration (ForEach)
- **StringExtensions.cs** - String utilities (SplitPascalCase)
- **UnityEventExtensions.cs** - UnityEvent helpers (Once for 0, 1, and 2 arguments)

#### Lifecycle
- **RetryUtil.cs** - Retry mechanism with cancellable Handle (Retry, RetryUntil, After)
- **EditorTickerBase.cs** - Editor-time ticking system (IEditorTick, EditorTickService)
- **DestroyUtils.cs** - Destroy utilities (PendingDestroyMarker, IsPendingToDestroy)

#### Scene
- **SceneRefHelper.cs** - Scene reference helper with gizmos, layer/sorting validation, snapping

#### Colliders
- **TriggerRelay2D.cs** - Simple 2D trigger event relay (OnEnter, OnExit, OnStay)
- **TriggerDebug.cs** - 2D trigger debugging with detailed logging via BrewedCode.Logging (optional)

---

### `Animancer/` - Animancer Integration
**Assembly:** `BrewedCode.Utils.Animancer.asmdef` | **Namespace:** `BrewedCode.Utils.Animancer`

**Dependencies:** Animancer (independent library)

Use this module only if Animancer is installed in your project.

- **AnimancerExtensions.cs** - Animancer time synchronization helpers
  - TimeSyncReverseAware: Store and sync animation phases with reverse-aware support
  - AnimancerUtilExtensions: Utility methods like StopAndClearSpriteRenderer

---

### `MoreMountains/` - MoreMountains.Tools Integration
**Assembly:** `BrewedCode.Utils.MoreMountains.asmdef` | **Namespace:** `BrewedCode.Utils.MoreMountains.*`

**Dependencies:** MoreMountains.Tools, BrewedCode.Foundation, BrewedCode.Signals

Use this module only if MoreMountains.Tools is installed in your project.

#### ParticleSystem (MoreMountains-based)
- **ParticleShapeAlign.cs** - Aligns ParticleSystem shapes with transform or LineRenderer direction
- **ParticleSystemEvents.cs** - ParticleSystem lifecycle events (OnPlayed, OnPaused, OnFinished, etc.)

#### Colliders
- **PolygonPathQuery.cs** - 2D polygon path detection (point-in-polygon queries for segmented PolygonCollider2D)

---

### `GraphToolkit/` - Unity GraphToolkit Integration (Editor Only)
**Assembly:** `BrewedCode.Utils.GraphToolkit.asmdef` [Editor] | **Namespace:** `BrewedCode.Utils.GraphToolkit`

**Dependencies:** Unity.GraphToolkit.Editor

Editor-only utilities for working with Unity's GraphToolkit system.

- **GraphNodeExtensions.cs** - Node and port query helpers with type-safe enum-based option access

---

## Usage

### Using Core Utils
```csharp
using BrewedCode.Utils;

// Extensions
myArray.Add(item);
enumValue.Name(); // Get enum name
myInt.ForEach(i => Debug.Log(i));

// Functional style
obj.Let(x => x.SomeMethod())
   .Also(x => Debug.Log("Side effect"))
   .OrElse(fallback);

// Retry mechanism
RetryUtil.Retry(monoBehaviour, () => {
    // Attempt risky operation
}, tries: 3, intervalSec: 0.5f);
```

### Using Animancer Integration
```csharp
using BrewedCode.Utils.Animancer;

// Animancer time sync with reverse awareness
animancer.StopAndClearSpriteRenderer();
timeSynchronizer.StorePhase(animancerState);
timeSynchronizer.SyncPhase(animancerState, group);
```

### Using MoreMountains Integration
```csharp
using BrewedCode.Utils.MoreMountains;

// ParticleSystem events
particleSystemEvents.OnFinishedAll.AddListener(() => Debug.Log("Done"));

// PolygonPathQuery for sectored areas
polygonPathQuery.OnHit.Subscribe(this, (pos, pathIndices) => {
    Debug.Log($"Hit paths: {string.Join(", ", pathIndices)}");
});
```

### Using GraphToolkit Extensions
```csharp
using BrewedCode.Utils.GraphToolkit;

// Type-safe node option access
if (node.TryGetNodeOptionValue<string, MyEnum>(MyEnum.SomeName, out var value))
{
    Debug.Log(value);
}
```

---

## Assembly Dependencies

```
BrewedCode.Utils (Core)
    └─ No external dependencies
       Can be used standalone in any project

BrewedCode.Utils.Animancer
    └─ Animancer (independent library)

BrewedCode.Utils.MoreMountains
    ├─ BrewedCode.Utils (Core)
    ├─ BrewedCode.Foundation
    ├─ BrewedCode.Signals
    └─ MoreMountains.Tools

BrewedCode.Utils.GraphToolkit [Editor]
    └─ Unity.GraphToolkit.Editor
```

---

## Key Features

✅ **Modular**: Each module has clear dependencies via assembly definitions
✅ **Zero-dependency Core**: `BrewedCode.Utils` can be extracted to any project
✅ **Functional Programming**: Kotlin-inspired extensions with Unity fake-null awareness
✅ **Editor Safe**: GraphToolkit utils isolated to Editor-only builds
✅ **Logging Optional**: TriggerDebug.cs gracefully handles missing LoggingRoot
✅ **Well-Documented**: Each utility has clear purpose and usage examples

---

## Migration Notes

All utilities were migrated from `Assets/_Project/Scripts/Utils` to this modular structure:
- Removed dependency on `VFolders.Libs` (replaced `.Destroy()` with `Object.Destroy()`)
- Organized by functionality and external dependencies
- Updated namespaces to `BrewedCode.Utils.*`
- Maintained full backward compatibility in API
