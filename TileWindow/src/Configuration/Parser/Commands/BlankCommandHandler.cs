using System;

namespace TileWindow.Configuration.Parser.Commands
{
    public class BlankCommandHandler : ICommandHandler
    {
        public bool CmdExecute(string cmd) => true;
        public bool CmdFocusDown(string cmd) => true;
        public bool CmdFocusLeft(string cmd) => true;
        public bool CmdFocusRight(string cmd) => true;
        public bool CmdFocusUp(string cmd) => true;
        public bool CmdKill(string cmd) => true;
        public bool CmdMoveDown(string cmd) => true;
        public bool CmdMoveLeft(string cmd) => true;
        public bool CmdMoveRight(string cmd) => true;
        public bool CmdShowWorkspace(string cmd) => true;
        public bool CmdMoveToWorkspace(string cmd) => true;
        public bool CmdMoveUp(string cmd) => true;
        public bool CmdRun(string cmd) => true;
        public bool CmdFullscreen(string cmd) => true;
        public bool CmdResizeLeft(string cmd) => true;
        public bool CmdResizeUp(string cmd) => true;
        public bool CmdResizeRight(string cmd) => true;
        public bool CmdResizeDown(string cmd) => true;
        public bool CmdSplitHorizontal(string cmd) => true;
        public bool CmdSplitToggle(string cmd) => true;
        public bool CmdSplitVertical(string cmd) => true;
        public bool CmdFloatingToggle(string cmd) => true;
        public bool CmdLayoutStacking(string cmd) => true;
        public bool CmdDebugGraph(string cmd) => true;
        public bool CmdRestart(string cmd) => true;
    }
}