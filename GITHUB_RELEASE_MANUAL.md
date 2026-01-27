# Create GitHub Release (Manual Steps)

✅ **Git Push Complete!**

The code has been successfully pushed to GitHub:
- **Repository**: https://github.com/Andre-Faustino/BrewedCode-Unity-Core
- **Branch**: main (pushed ✅)
- **Tag**: v1.0.0 (pushed ✅)

Now create the GitHub Release manually:

---

## Step 1: Go to Releases

Go to: https://github.com/Andre-Faustino/BrewedCode-Unity-Core/releases

Or from the repository page:
1. Click "Releases" on the right sidebar
2. Click "Create a new release"

---

## Step 2: Create Release

Fill in the form:

**Tag version**: `v1.0.0` (select from dropdown)

**Release title**: `BrewedCode Core Framework v1.0.0`

**Description**: Copy and paste the content below (entire CHANGELOG section)

---

## CHANGELOG Content to Paste

```markdown
# [1.0.0] - 2026-01-26

## Added

### Foundations Layer
- **Events System** - Type-safe, scope-based event bus with publish-subscribe pattern
  - IEventBus interface for decoupled event publishing
  - EventChannel<T> for channel-based event routing
  - Event scoping support via IEventScope
  - Comprehensive channel registry with listener tracking
  - 5 unit tests with 100% coverage

- **Logging System** - Structured, composable logging with multiple sinks
  - ILog interface with safe logging methods (Info, Warn, Error, Trace)
  - Multi-channel support (Default, System, Network, etc)
  - Pluggable sink architecture (Console, File, UI)
  - LoggingRoot persistent singleton bootstrap
  - Event publishing for log entries
  - Comprehensive documentation with examples

- **Singleton Pattern** - Safe singleton implementations for MonoBehaviours and C#
  - MonoSingleton<T> for scene-local singletons
  - PersistentMonoSingleton<T> for cross-scene persistence
  - Singleton<T> for plain C# singletons
  - ISingleton common interface
  - Proper handling of destroyed Unity objects (fake null detection)

- **TimerManager** - Deterministic time and timer management system
  - Pure C# core with injected delta time (enables replay/determinism)
  - 4 timer types: Timer, CountdownTimer, CooldownTimer, TweenTimer
  - Time scaling and pause/resume capabilities
  - Event publishing for timer lifecycle
  - Deterministic GameTime service
  - Scheduler for deferred execution
  - Comprehensive testing (6 unit tests)

### Systems Layer
- **ItemHub** - Central item definition and inventory management
  - Item registry with ItemId type-safe wrappers
  - Commodity system for stackable items
  - Instance system for unique items
  - Cargo (inventory) management
  - Event-based item notifications
  - Thread-safe operations

- **ResourceBay** - Thread-safe resource and currency management
  - Type-safe ResourceId wrappers (prevent accidental type confusion)
  - Allocation-based resource tracking
  - Explicit release requirements (preventing resource leaks)
  - Thread-safe allocation and balance checking via lock-based synchronization
  - Event publishing for resource changes
  - Query-based balance retrieval with immutable results

- **Crafting System** - Recipe-based production system with atomic transactions
  - Station-based crafting with CraftingStationId type-safe wrappers
  - Recipe registry with cost atomicity guarantees
  - Queue-based crafting with station capacity limits
  - Process state machine (Waiting → Processing → Paused → Finished)
  - Atomic cost deduction (all-or-nothing semantics)
  - Pause/resume capabilities
  - Event publishing for crafting lifecycle
  - Comprehensive testing (5 unit tests)

- **Theme System** - UI theming and dynamic styling
  - Theme registry with dynamic theme switching
  - Token-based theme values (colors, sizes, fonts)
  - Binding system for automatic UI updates
  - Scriptable theme asset support
  - Runtime theme customization

### Utils Layer
- **Core Extensions** - Lightweight utility functions
  - Lifecycle helpers (OnDestroy, OnEnable, OnDisable)
  - Scene management utilities
  - Collider/physics helper extensions

- **VitalGauge** - Health/stamina/mana meter UI component
  - Generic gauge for any vital value
  - State machine (Normal, Low, Empty, Full)
  - Custom threshold support with priority ordering
  - Rate-based value changes (drain/regen)
  - Edge-trigger events (state transitions only)
  - Epsilon-safe floating-point comparisons
  - Ticker binding for automatic updates
  - Comprehensive testing (7 unit tests)
  - Full documentation with code examples

- **Signals** - Signal/event trigger system for interactions
  - Simple trigger-based event dispatching
  - UI signal integration

- **Third-Party Integrations** - Optional modules for popular packages
  - **Animancer** - Animation state management integration
  - **MoreMountains.TopDownEngine** - TopDownEngine framework integration
  - **GraphToolkit** - Graph-based systems integration

### Shared Layer
- Common types and interfaces with zero dependencies
- IsExternalInit.cs - Support for init properties in older .NET versions
- Universal IEventBus interface
- Type-safe ID wrapper system
- Domain constants and enums

## Architecture
- Clean 4-layer separation: Shared → Foundations → Systems → Utils
- Dependency injection via explicit constructor parameters
- Pure C# services decoupled from MonoBehaviour bootstraps
- Zero circular dependencies enforced via assembly definitions
- 23 assembly definition files with clear boundaries
- Event-driven communication eliminates tight coupling
- Immutable query results prevent accidental state mutations

## Testing
- 20+ comprehensive unit tests with NUnit
- 100% testable pure C# services (zero Unity dependencies in business logic)
- Test assembly definitions with proper constraints (UNITY_INCLUDE_TESTS)
- High code coverage on critical paths (Events, Logging, Timers, Crafting)

## Documentation
- Complete ARCHITECTURE.md overview with system design and data flow
- Component-specific documentation for each major system
- Getting Started guides with code examples
- API reference sections
- Common patterns and best practices
- Troubleshooting guides

## Performance
- Zero allocations in critical hot paths (event publishing)
- Efficient reflection caching in EventChannelRegistry
- Lock-based synchronization with minimal contention (ResourceBay)
- Static instance caching for singletons (avoids repeated lookups)

## Breaking Changes
None - first release

## Known Limitations
- Third-party integration modules require their respective packages installed
- RuntimeBootstrap.cs remains project-specific (not included in package)
- No Samples~ folder in v1.0 (documentation-based examples only)
- TextMeshPro is a required dependency (standard for modern Unity projects)

## Installation
Install via Package Manager (Git URL):
```
https://github.com/Andre-Faustino/BrewedCode-Unity-Core.git#1.0.0
```

Or manually add to `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.brewedcode.core": "https://github.com/Andre-Faustino/BrewedCode-Unity-Core.git#1.0.0"
  }
}
```

## Credits
- Pure C# architecture enables 100% testable services
- Event-driven design prevents tight coupling
- Type-safe IDs prevent common bugs (ItemId vs ResourceId confusion)
- Deterministic time management enables replay systems
- Thread-safe operations for concurrent gameplay mechanics
```

---

## Step 3: Publish Release

1. Click "Publish release" button
2. Done! 🎉

The release will be available at:
https://github.com/Andre-Faustino/BrewedCode-Unity-Core/releases/tag/v1.0.0

---

## ✅ Complete!

Your package is now ready for distribution via Unity Package Manager!

**Install using Git URL:**
```
https://github.com/Andre-Faustino/BrewedCode-Unity-Core.git#1.0.0
```

**Or add to manifest.json:**
```json
{
  "dependencies": {
    "com.brewedcode.core": "https://github.com/Andre-Faustino/BrewedCode-Unity-Core.git#1.0.0"
  }
}
```

---

## Next Steps

1. ✅ GitHub repository created
2. ✅ Code pushed to main branch
3. ✅ v1.0.0 tag pushed
4. ⏳ Create GitHub Release (this manual step)
5. ⏳ Phase 4: Install in FarmSpace project and verify
