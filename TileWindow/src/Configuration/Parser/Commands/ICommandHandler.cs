namespace TileWindow.Configuration.Parser.Commands
{
    public interface ICommandHandler
    {
        bool CmdExecute(string cmd);
        bool CmdFocusLeft(string cmd);
        bool CmdFocusUp(string cmd);
        bool CmdFocusRight(string cmd);
        bool CmdFocusDown(string cmd);
        bool CmdMoveLeft(string cmd);
        bool CmdMoveUp(string cmd);
        bool CmdRestart(string data);
        bool CmdMoveRight(string cmd);
        bool CmdMoveDown(string cmd);
        bool CmdDebugGraph(string data);
        bool CmdShowWorkspace(string cmd);
        bool CmdMoveToWorkspace(string cmd);
        bool CmdSplitVertical(string cmd);
        bool CmdSplitHorizontal(string cmd);
        bool CmdSplitToggle(string cmd);
        bool CmdKill(string cmd);
        bool CmdRun(string cmd);
        bool CmdFullscreen(string cmd);
        bool CmdResizeLeft(string cmd);
        bool CmdResizeUp(string cmd);
        bool CmdResizeRight(string cmd);
        bool CmdResizeDown(string cmd);
        bool CmdFloatingToggle(string cmd);
        bool CmdLayoutStacking(string cmd);
    }
}