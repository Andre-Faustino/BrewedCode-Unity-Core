namespace BrewedCode.Logging
{
    /// <summary>
    /// Configuration and metadata for a log channel.
    /// Defines channel behavior, color, and enabled state.
    /// </summary>
    public sealed class LogChannelDefinition
    {
        public LogChannel Channel { get; }
        public string DisplayName { get; }
        public string ColorHex { get; set; } // e.g., "#00FF00" for green
        public bool IsEnabled { get; set; }
        public LogLevel MinLevel { get; set; }

        public LogChannelDefinition(
            LogChannel channel,
            string displayName,
            string colorHex = "#FFFFFF",
            bool isEnabled = true,
            LogLevel minLevel = LogLevel.Trace)
        {
            Channel = channel;
            DisplayName = displayName ?? channel.Name;
            ColorHex = colorHex;
            IsEnabled = isEnabled;
            MinLevel = minLevel;
        }
    }
}
