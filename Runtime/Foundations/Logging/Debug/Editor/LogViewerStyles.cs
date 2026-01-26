using UnityEngine;
using UnityEditor;

namespace BrewedCode.Logging.Debug
{
    /// <summary>
    /// GUIStyle definitions for the Log Viewer Editor Window.
    /// Provides color-coded styles for channels, levels, and UI elements.
    /// </summary>
    public static class LogViewerStyles
    {
        // Private cache for styles
        private static GUIStyle _logEntryStyleEven;
        private static GUIStyle _logEntryStyleOdd;
        private static GUIStyle _logEntryStyleSelected;
        private static GUIStyle _expandedContent;
        private static GUIStyle _toolbarButton;
        private static GUIStyle _toolbarToggle;

        // Color definitions
        private static readonly Color ColorTrace = new Color(0.53f, 0.53f, 0.53f); // #888888
        private static readonly Color ColorInfo = Color.white; // #FFFFFF
        private static readonly Color ColorWarning = Color.yellow; // #FFFF00
        private static readonly Color ColorError = new Color(1f, 0.26f, 0.26f); // #FF4444
        private static readonly Color ColorFatal = new Color(1f, 0f, 1f); // #FF00FF

        /// <summary>Gets the GUIStyle for even-numbered log entries.</summary>
        public static GUIStyle LogEntryEven => _logEntryStyleEven ??= CreateLogEntryStyle(new Color(0.15f, 0.15f, 0.15f));

        /// <summary>Gets the GUIStyle for odd-numbered log entries.</summary>
        public static GUIStyle LogEntryOdd => _logEntryStyleOdd ??= CreateLogEntryStyle(new Color(0.12f, 0.12f, 0.12f));

        /// <summary>Gets the GUIStyle for selected log entries.</summary>
        public static GUIStyle LogEntrySelected => _logEntryStyleSelected ??= CreateLogEntryStyle(new Color(0.2f, 0.35f, 0.5f));

        /// <summary>Gets the GUIStyle for expanded content details.</summary>
        public static GUIStyle ExpandedContent => _expandedContent ??= CreateExpandedContentStyle();

        /// <summary>Gets the GUIStyle for toolbar buttons.</summary>
        public static GUIStyle ToolbarButton => _toolbarButton ??= new GUIStyle(EditorStyles.toolbarButton)
        {
            padding = new RectOffset(5, 5, 2, 2),
            margin = new RectOffset(2, 2, 0, 0)
        };

        /// <summary>Gets the GUIStyle for toolbar toggle buttons.</summary>
        public static GUIStyle ToolbarToggle => _toolbarToggle ??= new GUIStyle(EditorStyles.toolbarButton)
        {
            padding = new RectOffset(5, 5, 2, 2),
            margin = new RectOffset(2, 2, 0, 0)
        };

        /// <summary>Get color for a specific log level.</summary>
        public static Color GetLevelColor(LogLevel level) => level switch
        {
            LogLevel.Trace => ColorTrace,
            LogLevel.Info => ColorInfo,
            LogLevel.Warning => ColorWarning,
            LogLevel.Error => ColorError,
            LogLevel.Fatal => ColorFatal,
            _ => ColorInfo
        };

        /// <summary>Get color for a specific channel from its hex definition.</summary>
        public static bool TryGetChannelColor(LogChannelDefinition channelDef, out Color color)
        {
            if (channelDef != null && ColorUtility.TryParseHtmlString(channelDef.ColorHex, out var parsed))
            {
                color = parsed;
                return true;
            }

            color = Color.white;
            return false;
        }

        /// <summary>Create a GUIStyle for log entry display with given background color.</summary>
        private static GUIStyle CreateLogEntryStyle(Color backgroundColor)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, backgroundColor);
            texture.Apply();

            return new GUIStyle(EditorStyles.label)
            {
                normal = { background = texture },
                padding = new RectOffset(5, 5, 2, 2),
                margin = new RectOffset(0, 0, 1, 1),
                wordWrap = false
            };
        }

        /// <summary>Create a GUIStyle for expanded content display.</summary>
        private static GUIStyle CreateExpandedContentStyle()
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.08f));
            texture.Apply();

            return new GUIStyle(EditorStyles.textArea)
            {
                normal = { background = texture },
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(15, 0, 0, 5),
                fontSize = 9,
                font = EditorStyles.miniLabel.font,
                wordWrap = true
            };
        }
    }
}
