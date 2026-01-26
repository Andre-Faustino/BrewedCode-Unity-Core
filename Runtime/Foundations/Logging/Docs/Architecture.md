# Logging System Architecture

## Overview

The Logging System is a modular, testable C# logging framework designed for the Unity engine. It provides centralized log management with support for multiple output channels, configurable filtering, and extensible sinks.

**Key Principle**: 100% testable without the Unity runtime. Core business logic is pure C#.

## Core Components

### 1. **ILoggingService** (Public API)
The main entry point for all logging operations. Manages logger creation, channel control, and global configuration.

```csharp
ILog GetLogger<T>();           // Get logger by type
ILog GetLogger(string name);    // Get logger by name
void EnableChannel(LogChannel);
void DisableChannel(LogChannel);
void SetGlobalMinLevel(LogLevel);
void AddSink(ILogSink);
```

### 2. **Logger (ILog Implementation)**
Individual logger instances that emit logs through the service. Each logger is bound to a specific channel and source.

- Lazy evaluation: checks if logging is enabled before formatting
- Supports structured logging with metadata
- Auto-captures stack traces for Error/Fatal levels

### 3. **LogChannel** (Value Object)
Strong-typed channel identifier representing functional domains (Crafting, UI, Save, etc).

**Predefined channels**:
- `System`, `Crafting`, `Inventory`, `Timer`, `Save`, `AI`, `UI`, `Audio`, `Network`, `Default`

**Custom channels**: `LogChannel.Custom("MyChannel")`

### 4. **LogLevel** (Filtering)
Hierarchical severity levels for filtering:
- `Trace` (0) → `Info` → `Warning` → `Error` → `Fatal` (4)

### 5. **LogEntry** (Immutable Data)
Represents a single log event. Contains:
- Timestamp, Source, Channel, Level
- Message, StackTrace, Exception, Metadata

### 6. **ILogSink** (Output Interface)
Extensible interface for directing logs to different destinations:

```csharp
public interface ILogSink
{
    void Write(LogEntry entry);
}
```

**Built-in implementations**:
- `UnityConsoleLogSink` - Outputs to Unity Debug console
- `FileLogSink` - Writes to disk files
- `NullLogSink` - Discards all logs (for testing)

### 7. **IEventBus** (Pub/Sub)**
Decouples log emission from consumption. Publishes two event types:
- `LogEmittedEvent` - Fired when a log is created (for UI overlays, telemetry)
- `ChannelStateChangedEvent` - Fired when channel enable/disable changes

## Architecture Diagram

```
┌─────────────────────────────────────────┐
│         Application Code                │
│  logger.Info("Crafting started")        │
└─────────────────┬───────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│    Logger (ILog Implementation)          │
│  - Checks IsEnabled(channel, level)    │
│  - Delegates to LoggingService         │
└─────────────────┬───────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│    LoggingService (ILoggingService)     │
│  - Validates filters                    │
│  - Creates immutable LogEntry           │
│  - Captures stack traces (Error+)      │
│  - Publishes LogEmittedEvent           │
└─────┬──────────────────────────────────┬┘
      │                                  │
      ▼                                  ▼
┌──────────────────────┐      ┌──────────────────────┐
│    ILogSink Array     │      │    IEventBus         │
│  - UnityConsole      │      │  - Debug Overlay     │
│  - FileLogSink       │      │  - Telemetry         │
│  - Custom Sinks      │      │  - Analytics         │
└──────────────────────┘      └──────────────────────┘
```

## Flow of Execution

1. **Logger Creation**: App calls `loggingService.GetLogger<MyClass>()`
2. **Log Emission**: App calls `logger.Info("message")`
3. **Level Check**: Logger checks `IsLogEnabled(channel, level)` - returns early if disabled
4. **Stack Trace**: For Error/Fatal, captures stack trace
5. **Entry Creation**: Immutable `LogEntry` created with all metadata
6. **Event Publishing**: `LogEmittedEvent` published to event bus
7. **Sink Dispatch**: `LogEntry` written to all registered sinks
8. **Exception Safety**: Sink exceptions are caught and ignored (never crash logging)

## Channel Management

Channels are independently controllable:

```csharp
service.EnableChannel(LogChannel.Crafting);
service.DisableChannel(LogChannel.UI);

// Check state
bool isEnabled = service.IsChannelEnabled(LogChannel.Save);
```

Each channel has:
- `IsEnabled` flag (default: true)
- `MinLevel` (default: Trace)
- Display name and color (for UI)

## Global Configuration

```csharp
service.SetGlobalMinLevel(LogLevel.Warning);  // Only Warning and above
```

**Filter chain**:
1. Message passes global min level check
2. Message passes channel-specific min level check
3. If both pass, logger emits

## Extensibility

### Custom Sinks

Implement `ILogSink`:

```csharp
public class MyCustomSink : ILogSink
{
    public void Write(LogEntry entry)
    {
        // Custom destination (database, network, etc)
    }
}

service.AddSink(new MyCustomSink());
```

### Event Bus Integration

Consume log events:

```csharp
eventBus.Subscribe<LogEmittedEvent>(evt =>
{
    Debug.Log($"[{evt.Entry.Channel}] {evt.Entry.Message}");
});
```

## Testing

The entire system is testable with a mock event bus:

```csharp
var eventBus = new MockEventBus();
var service = new LoggingService(eventBus);
var logger = service.GetLogger("Test");

logger.Info("test");
// Assert: eventBus captured event
```

No Unity runtime required. Pure C# validation possible.
