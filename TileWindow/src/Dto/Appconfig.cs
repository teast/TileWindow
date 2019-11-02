using System.Collections.Generic;

namespace TileWindow.Dto
{
    public class AppConfig
    {
        public Dictionary<string, string> KeyBinds { get; set; }

        public BarConfig Bar { get; set; }

        /// <summary>
        /// Set to true if the program should disable normal windows-key shortcuts
        /// </summary>
        public bool DisableWinKey { get; set; }

        /// <summary>
        /// how many milliseconds for each key repeat
        /// </summary>
        public int KeyRepeatInMs { get; set; }

        /// <summary>
        /// True to show output from TWHandler/winhook
        /// </summary>
        public bool DebugShowHooks { get; set; }

        /// <summary>
        /// True to hide windows taskbar
        /// </summary>
        public bool HideTaskbar { get; set; }

        public AppConfig()
        {
            KeyBinds = new Dictionary<string, string>();
            DisableWinKey = false;
            KeyRepeatInMs = 125;
            DebugShowHooks = false;
            HideTaskbar = false;
            Bar = null;
        }
    }

    public enum BarPosition
    {
        Top,
        Left,
        Right,
        Bottom
    }

    public class BarConfig
    {
        public BarPosition Position { get; set; }
        public BarColorsConfig Colors { get; set; }

        public BarConfig()
        {
            Position = BarPosition.Bottom;
            Colors = new BarColorsConfig();
        }
    }

    public class BarColorsConfig
    {
        public string Background { get; set; }
        public string Statusline { get; set; }
        public BarColorClassConfig FocusedWorkspace { get; set; }
        public BarColorClassConfig InactiveWorkspace { get; set; }

        public BarColorsConfig()
        {
            Background = "#000000";
            Statusline = "#ffffff";
            FocusedWorkspace = new BarColorClassConfig
            {
                Border = "#4c7899",
                Background = "#285577",
                Text = "#ffffff"
            };
            InactiveWorkspace = new BarColorClassConfig
            {
                Border = "#333333",
                Background = "#222222",
                Text = "#888888"
            };
        }
    }

    public class BarColorClassConfig
    {
        public string Border { get; set; }
        public string Background { get; set; }
        public string Text { get; set; }
    }
}