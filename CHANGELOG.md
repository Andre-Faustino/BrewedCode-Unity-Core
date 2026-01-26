# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-01-26

### Added

#### Foundations Layer
- **Events System** - Type-safe, scope-based event bus with publish-subscribe pattern
  - `IEventBus` interface for decoupled event publishing
  - `EventChannel<T>` for channel-based event routing
  - Event scoping support via `IEventScope`
  - Comprehensive channel registry with listener tracking
  - 5 unit tests with 100% coverage

- **Logging System** - Structured, composable logging with multiple sinks
  - `ILog` interface with safe logging methods (Info, Warn, Error, Trace)
  - Multi-channel support (Default, System, Network, etc)
  - Pluggable sink architecture (Console, File, UI)
  - LoggingRoot persistent singleton bootstrap
  - Event publishing for log entries
  - Comprehensive documentation with examples

- **Singleton Pattern** - Safe singleton implementations for MonoBehaviours and C#
  - `MonoSingleton<T>` for scene-local singletons
  - `PersistentMonoSingleton<T>` for cross-scene persistence
  - `Singleton<T>` for plain C# singletons
  - `ISingleton` common interface
  - Proper handling of destroyed Unity objects (fake null detection)

- **TimerManager** - Deterministic time and timer management system
  - Pure C# core with injected delta time (enables replay/determinism)
  - 4 timer types: Timer, CountdownTimer, CooldownTimer, TweenTimer
  - Time scaling and pause/resume capabilities
  - Event publishing for timer lifecycle
  - Deterministic GameTime service
  - Scheduler for deferred execution
  - Comprehensive testing (6 unit tests)

#### Systems Layer
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

#### Utils Layer
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

#### Shared Layer
- Common types and interfaces with zero dependencies
- `IsExternalInit.cs` - Support for `init` properties in older .NET versions
- Universal `IEventBus` interface
- Type-safe ID wrapper system
- Domain constants and enums

### Architecture
- Clean 4-layer separation: Shared → Foundations → Systems → Utils
- Dependency injection via explicit constructor parameters
- Pure C# services decoupled from MonoBehaviour bootstraps
- Zero circular dependencies enforced via assembly definitions
- 23 assembly definition files with clear boundaries
- Event-driven communication eliminates tight coupling
- Immutable query results prevent accidental state mutations

### Testing
- 20+ comprehensive unit tests with NUnit
- 100% testable pure C# services (zero Unity dependencies in business logic)
- Test assembly definitions with proper constraints (`UNITY_INCLUDE_TESTS`)
- High code coverage on critical paths (Events, Logging, Timers, Crafting)

### Documentation
- Complete ARCHITECTURE.md overview with system design and data flow
- Component-specific documentation for each major system
- Getting Started guides with code examples
- API reference sections
- Common patterns and best practices
- Troubleshooting guides

### Performance
- Zero allocations in critical hot paths (event publishing)
- Efficient reflection caching in EventChannelRegistry
- Lock-based synchronization with minimal contention (ResourceBay)
- Static instance caching for singletons (avoids repeated lookups)

### Breaking Changes
None - first release

### Known Limitations
- Third-party integration modules require their respective packages installed
- RuntimeBootstrap.cs remains project-specific (not included in package)
- No Samples~ folder in v1.0 (documentation-based examples only)
- TextMeshPro is a required dependency (standard for modern Unity projects)

### Installation
Install via Package Manager (Git URL):
```
https://github.com/yourusername/BrewedCode-Core.git#1.0.0
```

Or manually add to `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.brewedcode.core": "https://github.com/yourusername/BrewedCode-Core.git#1.0.0"
  }
}
```

### Credits
- Pure C# architecture enables 100% testable services
- Event-driven design prevents tight coupling
- Type-safe IDs prevent common bugs (ItemId vs ResourceId confusion)
- Deterministic time management enables replay systems
- Thread-safe operations for concurrent gameplay mechanics

---

**Backwards Compatibility**: N/A - first major release

**Upgrade Path**: N/A - first major release

**Deprecations**: None
