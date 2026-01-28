# BrewedCode - Production-Grade Architecture for Unity

A modular, testable, event-driven framework for building scalable Unity games. Built with pure C# services, dependency injection, and clean architecture principles.

## 🎯 Overview

BrewedCode separates concerns into three layers:
- **Shared** - Universal types (IEventBus, IDs) with zero dependencies
- **Foundations** - Framework core (Logging, TimerManager, Events, Singleton)
- **Systems** - Game logic (Crafting, ItemHub, ResourceBay, Theme)
- **Utils** - Utilities (VitalGauge, Signals, Extensions)

**Key Features:**
✅ 100% testable (pure C#) | ✅ Fully decoupled (event-driven) | ✅ Type-safe IDs
✅ Thread-safe (ResourceBay) | ✅ Deterministic time | ✅ Fail-safe APIs

## 💾 Installation

### Via Package Manager (Git URL) - Recommended
1. Open Unity Package Manager: `Window → Package Manager`
2. Click the `+` button → `Add package from git URL`
3. Enter: `https://github.com/yourusername/BrewedCode-Core.git#1.0.0`
4. Click `Add`

### Via manifest.json
Add to `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.brewedcode.core": "https://github.com/yourusername/BrewedCode-Core.git#1.0.0"
  }
}
```

### Manual Clone
```bash
cd Packages/
git clone https://github.com/yourusername/BrewedCode-Core.git com.brewedcode.core
cd com.brewedcode.core
git checkout 1.0.0
```

### Requirements
- **Unity 2021.3** or later
- **TextMeshPro 3.0.6** or later (installed automatically)
- Optional: Kybernetik.Animancer (for `Utils.Animancer` integration)
- Optional: MoreMountains.TopDownEngine (for `Utils.MoreMountains` integration)
- Optional: Unity.GraphToolkit (for `Utils.GraphToolkit` integration)

See [CHANGELOG.md](CHANGELOG.md) for detailed release notes.

## 📁 Structure

```
BrewedCode/
├── Foundations/                 ← Core reusable library (single BrewedCode.Foundation.asmdef)
│   ├── Singleton/               - Generic singleton patterns for MonoBehaviours & C# classes
│   ├── Events/                  - Type-safe event bus system
│   ├── Logging/                 - Structured logging with sinks & debug support
│   └── TimerManager/            - Deterministic timer & time management system
│
├── Systems/                     ← Game-specific systems (can reuse on other projects)
│   ├── ItemHub/                 - Item definition, registry & management
│   ├── ResourceBay/             - Resource/currency management system
│   ├── Crafting/                - Recipe-based crafting system
│   └── Theme/                   - UI theming & style system
│
├── Utils/                       ← Lightweight utility features
│   ├── VitalGauge/              - Gauge UI component (health, stamina, etc)
│   └── Signals/                 - Signal/event trigger system
│
├── Shared/                      ← Common types & utilities (zero dependencies)
│   ├── IsExternalInit.cs        - C# 11 support for init properties
│   └── BrewedCode.Shared.asmdef
│
└── README.md
```

## 🎯 Layer Descriptions

### Foundations
**Purpose**: Universal, engine-agnostic foundations for any project
**Dependencies**: None (pure C# or Unity-only)
**Reusability**: ⭐⭐⭐⭐⭐ (can extract to UPM package)

Modules:
- **Singleton**: Abstract base classes for singleton patterns
  - `MonoSingleton<T>` - Scene-local singletons
  - `PersistentMonoSingleton<T>` - Cross-scene persistent singletons
  - `Singleton<T>` - Plain C# singletons
  - `ISingleton` - Common interface

- **Events**: Type-safe, scope-based event bus
  - `IEventBus` - Publish-subscribe interface
  - Scope support via `IEventScope`

- **Logging**: Structured, composable logging system
  - `ILog` - Core logging interface with safe methods
  - Multiple sinks (Console, File, UI)
  - LoggingRoot - Persistent singleton bootstrap

- **TimerManager**: Deterministic time & timer management
  - Pure C# core, event-driven
  - 4 timer types: Timer, CountdownTimer, CooldownTimer, TweenTimer
  - Time scaling, pause/resume, event publishing

### Systems
**Purpose**: Game-specific systems, but portable to other games
**Dependencies**: Foundations + optional cross-system deps
**Reusability**: ⭐⭐⭐ (tie to game design, but modular)

Modules:
- **ItemHub**: Central item definition & registry
- **ResourceBay**: Currency/resource management (coins, XP, etc)
- **Crafting**: Recipe-based production system
- **Theme**: UI theming & dynamic styling

### Utils
**Purpose**: Lightweight, standalone features
**Dependencies**: Minimal (usually just Logging)
**Reusability**: ⭐⭐⭐⭐ (pick individual features)

Modules:
- **VitalGauge**: Gauge UI component for values (health, mana, stamina)
- **Signals**: Signal/event trigger system for interactions

### Shared
**Purpose**: Common data types, interfaces, constants
**Dependencies**: None
**Reusability**: ⭐⭐⭐⭐⭐

Contains:
- `IsExternalInit.cs` - Support for `init` in older .NET versions
- Domain constants, enums, DTOs

## 📦 Assembly Organization

| Assembly | Purpose | References |
|----------|---------|-----------|
| `BrewedCode.Foundation` | All foundations in one | (none) |
| `BrewedCode.ItemHub` | Item system | Foundation, Shared |
| `BrewedCode.ResourceBay` | Resource system | Foundation, Shared |
| `BrewedCode.Crafting` | Crafting system | Foundation, TextMeshPro |
| `BrewedCode.Theme` | Theming system | Foundation |
| `BrewedCode.VitalGauge` | Gauge UI | Foundation |
| `BrewedCode.Signals` | Signal system | Foundation |
| `BrewedCode.Shared` | Common types | (none) |

## 🚀 Getting Started

1. **Use Foundations** in your project
   ```csharp
   using BrewedCode.Singleton;
   using BrewedCode.Events;
   using BrewedCode.Logging;
   using BrewedCode.TimerManager;
   ```

2. **Pick Systems** you need
   ```csharp
   using BrewedCode.ItemHub;
   using BrewedCode.ResourceBay;
   ```

3. **Add Utils** as-needed
   ```csharp
   using BrewedCode.VitalGauge;
   ```

## 🔗 Dependency Graph

```
                    BrewedCode.Foundation
                    (Singleton, Events, Logging, TimerManager)
                              ▲
                    ┌─────────┼─────────┬──────────┐
                    │         │         │          │
            ItemHub │  ResourceBay  Crafting    Theme
               │       │
               │       │
            Shared ◄───┘
```

## 📝 Notes

- No circular dependencies - clean architecture enforced via asmdefs
- Internal module boundaries (Singleton → Events → Logging) are implementation details
- All modules use safe null-coalescing patterns for optional features
- Editor/Test assemblies available for each module with Debug/Test variants

## 🎓 Best Practices

1. **Systems should communicate via Events**, not direct references
2. **Always null-coalesce** optional services (e.g., LoggingRoot)
3. **Bootstrap persistent singletons** in scenes as needed
4. **Use Shared.Shared** for cross-system DTOs only
5. **Keep Utils lightweight** - single responsibility

## 📚 Documentation

Each component has complete documentation:

**Architecture & Guides:**
- [Complete Architecture Overview](ARCHITECTURE.md) - System design, patterns, data flow

**Foundation Layer:**
- [Logging System](Foundations/Logging/Docs/README.md) - Channels, sinks, filtering
- [Timer Manager](Foundations/TimerManager/Docs/README.md) - Deterministic time, pause/resume
- [Events System](Foundations/Events/Docs/README.md) - Type-safe pub/sub
- [Singleton Pattern](Foundations/Singleton/Docs/README.md) - Safe singleton implementation

**Systems Layer:**
- [Crafting System](Systems/Crafting/Docs/README.md) - Stations, queues, atomic costs
- [ItemHub](Systems/ItemHub/Docs/README.md) - Inventory, commodities & instances
- [ResourceBay](Systems/ResourceBay/Docs/README.md) - Thread-safe resource allocation
- [Theme System](Systems/Theme/Docs/README.md) - UI theming with tokens

**Utilities:**
- [VitalGauge](Utils/VitalGauge/Docs/README.md) - Health/stamina/mana meters
- [Shared Types](Shared/README.md) - IEventBus, type-safe IDs, interfaces

---

*BrewedCode v1.0 - Pure C# architecture, event-driven, 100% testable*
