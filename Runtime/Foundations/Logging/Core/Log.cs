namespace BrewedCode.Logging
{
    /// <summary>
    /// Static facade for easy logger access.
    /// Usage: private static readonly ILog Log = Log.For&lt;MyClass&gt;();
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Creates a logger for the specified type.
        /// Automatically resolves source name and channel from type.
        /// </summary>
        public static ILog For<T>()
        {
            return LoggingRoot.Instance.Service.GetLogger<T>();
        }

        /// <summary>
        /// Creates a logger with a custom source name.
        /// Uses default channel.
        /// </summary>
        public static ILog For(string sourceName)
        {
            return LoggingRoot.Instance.Service.GetLogger(sourceName);
        }
    }
}
