using System.Collections.Generic;

namespace TileWindow.Dto
{
    public class AppConfig
    {
        public Dictionary<string, string> KeyBinds { get; set; }

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
    }
}