using System.Text;

namespace BrewedCode.Logging
{
    /// <summary>
    /// Formats LogEntry instances into human-readable strings.
    /// Used by sinks to produce output.
    /// </summary>
    public static class LogFormatter
    {
        public static string Format(LogEntry entry, bool includeTimestamp = true, bool includeChannel = true)
        {
            var sb = new StringBuilder();

            if (includeTimestamp)
                sb.Append($"[{entry.Timestamp:HH:mm:ss.fff}] ");

            if (includeChannel)
                sb.Append($"[{entry.Channel.Name}] ");

            sb.Append($"[{entry.Level}] ");
            sb.Append($"{entry.Source}: ");
            sb.Append(entry.Message);

            if (entry.Exception != null)
            {
                sb.AppendLine();
                sb.Append($"Exception: {entry.Exception}");
            }

            if (entry.StackTrace != null)
            {
                sb.AppendLine();
                sb.Append(entry.StackTrace);
            }

            if (entry.Metadata != null && entry.Metadata.Count > 0)
            {
                sb.AppendLine();
                sb.Append("Metadata: ");
                foreach (var kvp in entry.Metadata)
                {
                    sb.Append($"{kvp.Key}={kvp.Value} ");
                }
            }

            return sb.ToString();
        }

        public static string FormatUnityColored(LogEntry entry, LogChannelDefinition? channelDef)
        {
            var color = channelDef?.ColorHex ?? "#FFFFFF";
            var channelName = entry.Channel.Name;

            return $"<color={color}>[{channelName}]</color> " +
                   $"<color=#888888>[{entry.Timestamp:HH:mm:ss}]</color> " +
                   $"{entry.Source}: {entry.Message}";
        }
    }
}
