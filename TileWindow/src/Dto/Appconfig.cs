using System.Collections.Generic;

namespace TileWindow.Dto
{
    public class AppConfig
    {
        /// <summary>
        /// Set to true if the program should disable normal windows-key shortcuts
        /// </summary>
        public bool DisableWinKey { get; set; }

        /// <summary>
        /// Key combination for moving focus one step to the left
        /// </summary>
        public string FocusLeft { get; set; }

        /// <summary>
        /// Key combination for moving focus one step to the up
        /// </summary>
        public string FocusUp { get; set; }

        /// <summary>
        /// Key combination for moving focus one step to the rright
        /// </summary>
        public string FocusRight { get; set; }

        /// <summary>
        /// Key combination for moving focus one step to the down
        /// </summary>
        public string FocusDown { get; set; }

        /// <summary>
        /// Key combination for moving current window left
        /// </summary>
        public string MoveLeft { get; set; }

        /// <summary>
        /// Key combination for moving current window up
        /// </summary>
        public string MoveUp { get; set; }

        /// <summary>
        /// Key combination for moving current window right
        /// </summary>
        public string MoveRight { get; set; }

        /// <summary>
        /// Key combination for moving current window down
        /// </summary>
        public string MoveDown { get; set; }

        /// <summary>
        /// Key combination for showing windows built in "run dialog"
        /// </summary>
        public string WinRun { get; set; }

        /// <summary>
        /// Key combination for outputing graphviz dot and (if possible) png file
        /// </summary>
        public string Debug { get; set; }

        /// <summary>
        /// Key combination for switching to vertical tiling
        /// </summary>
        public string Vertical { get; set; }

        /// <summary>
        /// Key combination for switching to horizontal tiling
        /// </summary>
        /// <value></value>
        public string Horizontal { get; set; }
        
        /// <summary>
        /// Key combination for closing an program
        /// </summary>
        public string Quit { get; set; }

        /// <summary>
        /// Key combination for putting focus node in fullscreen on on escreen
        /// </summary>
        public string Fullscreen { get; set; }
        
        /// <summary>
        /// Key combination for resizing focus nodes left side
        /// </summary>
        public string ResizeLeft { get; set; }

        /// <summary>
        /// Key combination for resizing focus nodes up side
        /// </summary>
        public string ResizeUp { get; set; }

        /// <summary>
        /// Key combination for resizing focus nodes right side
        /// </summary>
        public string ResizeRight { get; set; }

        /// <summary>
        /// Key combination for resizing focus nodes down side
        /// </summary>
        public string ResizeDown { get; set; }

        /// <summary>
        /// Key combination for showing virtual desktop 1
        /// </summary>
        public string ShowDesktop1 { get; set; }

        /// <summary>
        /// Key combination for showing virtual desktop 2
        /// </summary>
        public string ShowDesktop2 { get; set; }

        /// <summary>
        /// Key combination for showing virtual desktop 3
        /// </summary>
        public string ShowDesktop3 { get; set; }

        /// <summary>
        /// Key combination for showing virtual desktop 4
        /// </summary>
        public string ShowDesktop4 { get; set; }

        /// <summary>
        /// Key combination for showing virtual desktop 5
        /// </summary>
        public string ShowDesktop5 { get; set; }

        /// <summary>
        /// Key combination for showing virtual desktop 6
        /// </summary>
        public string ShowDesktop6 { get; set; }

        /// <summary>
        /// Key combination for showing virtual desktop 7
        /// </summary>
        public string ShowDesktop7 { get; set; }

        /// <summary>
        /// Key combination for showing virtual desktop 8
        /// </summary>
        public string ShowDesktop8 { get; set; }

        /// <summary>
        /// Key combination for showing virtual desktop 9
        /// </summary>
        public string ShowDesktop9 { get; set; }

        /// <summary>
        /// Key combination for showing virtual desktop 10
        /// </summary>
        public string ShowDesktop10 { get; set; }

        /// <summary>
        /// Key combination for moving focus node to desktop 1
        /// </summary>
        public string MoveToDesktop1 { get; set; }

        /// <summary>
        /// Key combination for moving focus node to desktop 2
        /// </summary>
        public string MoveToDesktop2 { get; set; }

        /// <summary>
        /// Key combination for moving focus node to desktop 3
        /// </summary>
        public string MoveToDesktop3 { get; set; }

        /// <summary>
        /// Key combination for moving focus node to desktop 4
        /// </summary>
        public string MoveToDesktop4 { get; set; }

        /// <summary>
        /// Key combination for moving focus node to desktop 5
        /// </summary>
        public string MoveToDesktop5 { get; set; }

        /// <summary>
        /// Key combination for moving focus node to desktop 6
        /// </summary>
        public string MoveToDesktop6 { get; set; }

        /// <summary>
        /// Key combination for moving focus node to desktop 7
        /// </summary>
        public string MoveToDesktop7 { get; set; }

        /// <summary>
        /// Key combination for moving focus node to desktop 8
        /// </summary>
        public string MoveToDesktop8 { get; set; }

        /// <summary>
        /// Key combination for moving focus node to desktop 9
        /// </summary>
        public string MoveToDesktop9 { get; set; }

        /// <summary>
        /// Key combination for moving focus node to desktop 10
        /// </summary>
        public string MoveToDesktop10 { get; set; }

        /// <summary>
        /// Key combinatin for switching between floating and tile
        /// </summary>
        public string SwitchFloating { get; set; }

        /// <summary>
        /// Key combinatin for restarting threads in tile window
        /// </summary>
        public string RestartTW { get; set; }

        /// <summary>
        /// List of custom key combination and what to execute when they are pressed
        /// </summary>
        /// <remarks>
        /// Key should be the key combination and value should be the command to execute
        /// example:
        /// { "WIN+r", "cmd.exe" }
        /// </remarks>
        public IDictionary<string, string> Custom { get; set; }

        /// <summary>
        /// how many milliseconds for each key repeat
        /// </summary>
        public int KeyRepeatInMs { get; set; }

        /// <summary>
        /// Key combination for showing windows start menu
        /// </summary>
        public string ShowWinMenu { get; set; }

        /// <summary>
        /// True to show output from TWHandler/winhook
        /// </summary>
        public bool DebugShowHooks { get; set; }

        /// <summary>
        /// True to hide windows taskbar
        /// </summary>
        public bool HideTaskbar { get; set; }

        /// <summary>
        /// Key combination for showing/hiding windows taskbar
        /// </summary>
        public string ToggleTaskbar { get; set; }
    }
}