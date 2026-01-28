# Getting Started with the Logging System

## Quick Start

### 1. Initialize the Logging Service

In your bootstrap/startup code:

```csharp
using BrewedCode.Logging;

// Create event bus (Unity implementation)
var eventBus = new UnityEventChannelBus();

// Create and configure logging service
var loggingService = new LoggingService(eventBus);

// Add sinks
loggingService.AddSink(new UnityConsoleLogSink());
loggingService.AddSink(new FileLogSink("./Logs/app.log"));

// Optional: Set global minimum level
loggingService.SetGlobalMinLevel(LogLevel.Trace);
```

### 2. Get a Logger

```csharp
public class CraftingSystem
{
    private readonly ILog _logger;

    public CraftingSystem(ILoggingService loggingService)
    {
        // By type
        _logger = loggingService.GetLogger<CraftingSystem>();

        // Or by name
        _logger = loggingService.GetLogger("CraftingSystem");
    }
}
```

### 3. Log Messages

```csharp
_logger.Trace("Detailed trace information");
_logger.Info("Crafting started");
_logger.Warning("Low on resources");
_logger.Error("Crafting failed");
_logger.Fatal("System critical error");
```

### 4. Log with Metadata

```csharp
var metadata = new Dictionary<string, object>
{
    { "item_id", 42 },
    { "quantity", 5 },
    { "duration_ms", 1234 }
};

_logger.Info("Crafting completed", metadata);
```

### 5. Log Exceptions

```csharp
try
{
    // Some operation
}
catch (Exception ex)
{
    _logger.Error("Operation failed", ex);  // Auto-captures stack trace
}
```

## Channel Management

### Using Predefined Channels

Channels are automatically created when logs are emitted. Use predefined channels for core systems:

```csharp
// The Logger constructor automatically binds to a channel
_logger = loggingService.GetLogger<SaveSystem>();  // → LogChannel.Save
_logger = loggingService.GetLogger<UIManager>();   // → LogChannel.UI
_logger = loggingService.GetLogger<AIController>(); // → LogChannel.AI
```

### Creating Custom Channels

```csharp
var myChannel = LogChannel.Custom("MyFeature");
_logger = new Logger("MyClass", myChannel, loggingService);
```

### Controlling Channels

```csharp
// Disable a channel
loggingService.DisableChannel(LogChannel.AI);

// Enable a channel
loggingService.EnableChannel(LogChannel.UI);

// Check if enabled
if (loggingService.IsChannelEnabled(LogChannel.Network))
{
    // Channel is active
}

// Get all channels
var allChannels = loggingService.GetAllChannels();
foreach (var channelDef in allChannels)
{
    Debug.Log($"{channelDef.Name}: {(channelDef.IsEnabled ? "Enabled" : "Disabled")}");
}
```

## Log Levels

Use appropriate log levels:

| Level | Use Case | Example |
|-------|----------|---------|
| **Trace** | Very detailed debug info | Variable values, loop iterations |
| **Info** | General information | "System initialized", "User logged in" |
| **Warning** | Potentially problematic | "Resource running low", "Deprecated API" |
| **Error** | Recoverable errors | "Failed to load asset", "Invalid input" |
| **Fatal** | Critical failures | "Corrupted save file", "Out of memory" |

## Filtering

### Global Minimum Level

Only logs at or above this level are processed:

```csharp
loggingService.SetGlobalMinLevel(LogLevel.Warning);
// Trace, Info are discarded
// Warning, Error, Fatal are kept
```

### Channel-Specific Filtering

```csharp
var channelDef = loggingService.GetChannelDefinition(LogChannel.AI);
if (channelDef != null)
{
    channelDef.MinLevel = LogLevel.Warning;  // Only warn+ for AI channel
}
```

### Performance Check

Avoid expensive log formatting if logging is disabled:

```csharp
if (_logger.IsEnabled(LogLevel.Trace))
{
    var expensiveData = ComputeDebugInfo();
    _logger.Trace($"Debug: {expensiveData}");
}
```

## Event Bus Integration

### Consuming Log Events

Listen to logs in real-time (for UI overlays, telemetry, etc):

```csharp
eventBus.Subscribe<LogEmittedEvent>(evt =>
{
    var entry = evt.Entry;
    Debug.Log($"[{entry.Channel}] {entry.Level}: {entry.Message}");
});
```

### Channel State Changes

Listen for enable/disable changes:

```csharp
eventBus.Subscribe<ChannelStateChangedEvent>(evt =>
{
    Debug.Log($"Channel {evt.Channel} is now {(evt.IsEnabled ? "enabled" : "disabled")}");
});
```

## Adding Custom Sinks

### Example: Network Sink

```csharp
public class RemoteLogSink : ILogSink
{
    private readonly string _serverUrl;

    public RemoteLogSink(string serverUrl)
    {
        _serverUrl = serverUrl;
    }

    public void Write(LogEntry entry)
    {
        // Send to remote server
        var json = JsonUtility.ToJson(new
        {
            timestamp = entry.Timestamp,
            channel = entry.Channel.Name,
            level = entry.Level.ToString(),
            message = entry.Message
        });

        // Send via HTTP (implement actual networking)
    }
}

// Register
loggingService.AddSink(new RemoteLogSink("https://logs.myserver.com"));
```

### Removing Sinks

```csharp
var sink = new UnityConsoleLogSink();
loggingService.AddSink(sink);

// Later...
loggingService.RemoveSink(sink);
```

## Common Patterns

### Per-Class Logger

```csharp
public class MySystem
{
    private readonly ILog _logger;

    public MySystem(ILoggingService loggingService)
    {
        _logger = loggingService.GetLogger<MySystem>();
    }

    public void DoWork()
    {
        _logger.Info("Work started");
        try
        {
            // ... work
            _logger.Info("Work completed");
        }
        catch (Exception ex)
        {
            _logger.Error("Work failed", ex);
        }
    }
}
```

### Static Logger Access

For quick debugging (not recommended for production):

```csharp
public static class Log
{
    private static ILoggingService? _service;

    public static void Initialize(ILoggingService service)
    {
        _service = service;
    }

    public static ILog GetLogger(string name) => _service?.GetLogger(name) ?? new NullLogger();
}

// Usage
Log.Initialize(loggingService);
var logger = Log.GetLogger("MyClass");
```

## Best Practices

✅ **DO**:
- Pass `ILoggingService` via dependency injection
- Use specific channels for different subsystems
- Log at appropriate levels
- Include relevant metadata
- Catch and log exceptions
- Check `IsEnabled()` before expensive operations

❌ **DON'T**:
- Use `FindObjectOfType<>()` to get the logging service
- Log passwords, tokens, or sensitive data
- Log in tight loops without level checks
- Create loggers in tight loops (create once, reuse)
- Ignore sink exceptions (they're already handled)

## Testing

### Unit Test Example

```csharp
[Test]
public void ShouldLogCraftingStart()
{
    // Arrange
    var eventBus = new MockEventBus();
    var service = new LoggingService(eventBus);
    var logger = service.GetLogger<CraftingSystem>();

    // Act
    logger.Info("Crafting started");

    // Assert
    var emittedEvents = eventBus.GetPublishedEvents<LogEmittedEvent>();
    Assert.AreEqual(1, emittedEvents.Count);
    Assert.AreEqual("Crafting started", emittedEvents[0].Entry.Message);
}
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Logs not appearing | Check channel is enabled, level is sufficient, sink is registered |
| Performance issues | Use `IsEnabled()` checks, increase `GlobalMinLevel`, disable verbose channels |
| Missing stack traces | Only captured for Error/Fatal levels, ensure exception is non-null |
| Sink crashes game | Won't happen - exceptions are caught and ignored |
