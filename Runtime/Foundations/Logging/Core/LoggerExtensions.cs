namespace BrewedCode.Logging
{
    /// <summary>
    /// Null-safe extension methods for ILogger.
    /// Allows safe logging even when logger is null, falling back to Unity's Debug output.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Logs an info message safely, with null-check fallback.
        /// </summary>
        public static void InfoSafe(this ILog? logger, string message)
        {
            if (logger != null)
                logger.Info(message);
            else
                UnityEngine.Debug.Log(message);
        }

        /// <summary>
        /// Logs a warning message safely, with null-check fallback.
        /// </summary>
        public static void WarningSafe(this ILog? logger, string message)
        {
            if (logger != null)
                logger.Warning(message);
            else
                UnityEngine.Debug.LogWarning(message);
        }

        /// <summary>
        /// Logs an error message safely, with null-check fallback.
        /// </summary>
        public static void ErrorSafe(this ILog? logger, string message)
        {
            if (logger != null)
                logger.Error(message);
            else
                UnityEngine.Debug.LogError(message);
        }

        /// <summary>
        /// Logs a trace message safely, with null-check fallback.
        /// </summary>
        public static void TraceSafe(this ILog? logger, string message)
        {
            if (logger != null)
                logger.Trace(message);
            else
                UnityEngine.Debug.Log(message);
        }

        /// <summary>
        /// Logs a fatal message safely, with null-check fallback.
        /// </summary>
        public static void FatalSafe(this ILog? logger, string message)
        {
            if (logger != null)
                logger.Fatal(message);
            else
                UnityEngine.Debug.LogError($"[FATAL] {message}");
        }
    }
}
