using System;
using System.Collections.Generic;
using System.Diagnostics;
using BrewedCode.Events;

namespace BrewedCode.Logging
{
    /// <summary>
    /// Pure C# implementation of ILoggingService.
    ///
    /// Responsibilities:
    /// - Create and cache ILog instances per source
    /// - Manage log channels (enable/disable, min level)
    /// - Dispatch logs to registered sinks
    /// - Publish logging events for reactive workflows
    /// - Apply global and per-channel filters
    ///
    /// Design:
    /// - 100% testable (no Unity dependencies)
    /// - Immutable LogEntry DTOs
    /// - Stack trace capture only for Error/Fatal
    /// - Thread-safe (no locking needed, immutable after init)
    ///
    /// Channel Management:
    /// - Each log message belongs to a LogChannel
    /// - Channels can be independently enabled/disabled
    /// - Per-channel minimum log level filtering
    /// - Automatic channel resolution via LoggerFactory
    /// </summary>
    public sealed class LoggingService : ILoggingService
    {
        private readonly IEventBus _eventBus;
        private readonly LoggerFactory _factory;
        private readonly ChannelRegistry _channelRegistry;
        private readonly List<ILogSink> _sinks = new();
        private LogLevel _globalMinLevel = LogLevel.Trace;

        public LoggingService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _factory = new LoggerFactory(this);
            _channelRegistry = new ChannelRegistry();
            InitializeDefaultChannels();
        }

        // ===== Logger Factory =====

        public ILog GetLogger<T>() => _factory.GetLogger<T>();
        public ILog GetLogger(string sourceName) => _factory.GetLogger(sourceName);

        // ===== Channel Management =====

        public void EnableChannel(LogChannel channel)
        {
            var def = _channelRegistry.GetOrCreateDefinition(channel);
            def.IsEnabled = true;
            _eventBus.Publish(new ChannelStateChangedEvent { Channel = channel, IsEnabled = true });
        }

        public void DisableChannel(LogChannel channel)
        {
            var def = _channelRegistry.GetOrCreateDefinition(channel);
            def.IsEnabled = false;
            _eventBus.Publish(new ChannelStateChangedEvent { Channel = channel, IsEnabled = false });
        }

        public bool IsChannelEnabled(LogChannel channel) =>
            _channelRegistry.GetOrCreateDefinition(channel).IsEnabled;

        public LogChannelDefinition? GetChannelDefinition(LogChannel channel) =>
            _channelRegistry.GetDefinition(channel);

        public IReadOnlyList<LogChannelDefinition> GetAllChannels() =>
            _channelRegistry.GetAllDefinitions();

        // ===== Global Configuration =====

        public void SetGlobalMinLevel(LogLevel level) => _globalMinLevel = level;
        public LogLevel GetGlobalMinLevel() => _globalMinLevel;

        // ===== Sink Management =====

        public void AddSink(ILogSink sink)
        {
            if (sink != null && !_sinks.Contains(sink))
                _sinks.Add(sink);
        }

        public void RemoveSink(ILogSink sink)
        {
            _sinks.Remove(sink);
        }

        // ===== Internal Logging API (called by Logger) =====

        /// <summary>
        /// Fast-path check: Should this log message be emitted?
        /// Checked before creating LogEntry to save allocations.
        ///
        /// Filtering hierarchy:
        /// 1. Global minimum level (affects all logs)
        /// 2. Channel enabled/disabled
        /// 3. Channel minimum level
        /// All must pass for log to proceed.
        /// </summary>
        internal bool IsLogEnabled(LogChannel channel, LogLevel level)
        {
            if (level < _globalMinLevel) return false;
            var def = _channelRegistry.GetOrCreateDefinition(channel);
            return def.IsEnabled && level >= def.MinLevel;
        }

        internal void EmitLog(
            string source,
            LogChannel channel,
            LogLevel level,
            string message,
            Exception? exception,
            IReadOnlyDictionary<string, object>? metadata)
        {
            // Fast path: check if logging is enabled
            if (!IsLogEnabled(channel, level)) return;

            // Capture stack trace for Error and Fatal
            string? stackTrace = null;
            if (level >= LogLevel.Error)
            {
                stackTrace = new StackTrace(2, true).ToString(); // Skip 2 frames (EmitLog + Logger method)
            }

            // Create immutable log entry
            var entry = new LogEntry(
                timestamp: DateTime.UtcNow,
                source: source,
                channel: channel,
                level: level,
                message: message,
                stackTrace: stackTrace,
                exception: exception,
                metadata: metadata
            );

            // Publish event (for debug overlay, telemetry, etc)
            _eventBus.Publish(new LogEmittedEvent { Entry = entry });

            // Dispatch to all sinks
            foreach (var sink in _sinks)
            {
                try
                {
                    sink.Write(entry);
                }
                catch
                {
                    // Never let sink exceptions crash the application
                }
            }
        }

        // ===== Initialization =====

        private void InitializeDefaultChannels()
        {
            _channelRegistry.Register(LogChannel.System, "System", "#FFFFFF");
            _channelRegistry.Register(LogChannel.Crafting, "Crafting", "#FFA500");
            _channelRegistry.Register(LogChannel.Inventory, "Inventory", "#FFD700");
            _channelRegistry.Register(LogChannel.Timer, "Timer", "#00CED1");
            _channelRegistry.Register(LogChannel.Save, "Save", "#8A2BE2");
            _channelRegistry.Register(LogChannel.AI, "AI", "#FF6347");
            _channelRegistry.Register(LogChannel.UI, "UI", "#7FFF00");
            _channelRegistry.Register(LogChannel.Audio, "Audio", "#FF1493");
            _channelRegistry.Register(LogChannel.Network, "Network", "#1E90FF");
            _channelRegistry.Register(LogChannel.Default, "Default", "#A9A9A9");
        }
    }
}
