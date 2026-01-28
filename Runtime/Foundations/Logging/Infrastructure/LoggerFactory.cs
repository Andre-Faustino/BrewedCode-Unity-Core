using System;
using System.Collections.Concurrent;

namespace BrewedCode.Logging
{
    /// <summary>
    /// Factory for creating and caching ILog instances per type.
    /// Ensures single logger per source with automatic channel resolution.
    /// </summary>
    internal sealed class LoggerFactory
    {
        private readonly LoggingService _service;
        private readonly ConcurrentDictionary<string, ILog> _loggerCache = new();

        public LoggerFactory(LoggingService service)
        {
            _service = service;
        }

        public ILog GetLogger<T>()
        {
            var typeName = typeof(T).Name;
            return GetLogger(typeName, ResolveChannelForType(typeof(T)));
        }

        public ILog GetLogger(string sourceName)
        {
            return GetLogger(sourceName, LogChannel.Default);
        }

        private ILog GetLogger(string sourceName, LogChannel channel)
        {
            return _loggerCache.GetOrAdd(sourceName,
                _ => new Logger(sourceName, channel, _service));
        }

        private LogChannel ResolveChannelForType(Type type)
        {
            // Automatic channel resolution based on namespace
            var ns = type.Namespace ?? "";

            if (ns.Contains("Crafting")) return LogChannel.Crafting;
            if (ns.Contains("ItemHub") || ns.Contains("Inventory")) return LogChannel.Inventory;
            if (ns.Contains("TimerManager") || ns.Contains("Timer")) return LogChannel.Timer;
            if (ns.Contains("Save")) return LogChannel.Save;
            if (ns.Contains("AI")) return LogChannel.AI;
            if (ns.Contains("UI")) return LogChannel.UI;
            if (ns.Contains("Audio") || ns.Contains("FMOD")) return LogChannel.Audio;

            return LogChannel.Default;
        }
    }
}
