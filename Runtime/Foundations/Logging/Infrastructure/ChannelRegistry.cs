using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BrewedCode.Logging
{
    /// <summary>
    /// Thread-safe registry for channel definitions.
    /// Manages channel metadata and configuration.
    /// </summary>
    internal sealed class ChannelRegistry
    {
        private readonly ConcurrentDictionary<LogChannel, LogChannelDefinition> _channels = new();

        public void Register(LogChannel channel, string displayName, string colorHex)
        {
            _channels[channel] = new LogChannelDefinition(channel, displayName, colorHex);
        }

        public LogChannelDefinition GetOrCreateDefinition(LogChannel channel)
        {
            return _channels.GetOrAdd(channel, ch =>
                new LogChannelDefinition(ch, ch.Name, "#FFFFFF"));
        }

        public LogChannelDefinition? GetDefinition(LogChannel channel)
        {
            _channels.TryGetValue(channel, out var def);
            return def;
        }

        public IReadOnlyList<LogChannelDefinition> GetAllDefinitions()
        {
            return _channels.Values.ToList();
        }
    }
}
