using System.Collections.Generic;

namespace BrewedCode.Logging
{
    /// <summary>
    /// Builder for log metadata dictionaries.
    /// Provides fluent API for adding contextual information to logs.
    /// </summary>
    public sealed class LogMetadata
    {
        private readonly Dictionary<string, object> _data = new();

        public LogMetadata Add(string key, object value)
        {
            _data[key] = value;
            return this;
        }

        public IReadOnlyDictionary<string, object> Build() => _data;

        public static LogMetadata Create() => new();
    }
}
