using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Serilog;
using TileWindow.Configuration.Parser.Commands;
using TileWindow.Extra.GraphViz;
using TileWindow.Nodes;

namespace TileWindow.Handlers
{
    public interface ICommandHelper
    {
        bool GetCommand(string command);
    }

    public class CommandHelper: ICommandHelper, ICommandHandler
    {
        public readonly IVirtualDesktopCollection desktops;
        public readonly IPInvokeHandler pinvokeHandler;
        public readonly ICommandExecutor commandExecutor;

        public CommandHelper(IVirtualDesktopCollection desktops, IPInvokeHandler pinvokeHandler, ICommandExecutor commandExecutor)
        {
            this.desktops = desktops;
            this.pinvokeHandler = pinvokeHandler;
            this.commandExecutor = commandExecutor;
        }
        
        public bool GetCommand(string command)
        {
            var result = commandExecutor.Execute(command, this);
            if (result == null)
            {
                Log.Error($"Could not handle command \"{command}\"");
            }

            return result ?? false;
        }

        public bool CmdExecute(string command)
        {
            Log.Warning($"ExternalCommand: \"{command}\" not implemented");
            return false;
        }
        
        public bool CmdToggleTaskbar(string cmd)
        {
            var trayHwnd = pinvokeHandler.FindWindow("Shell_TrayWnd", null);
            if (trayHwnd == IntPtr.Zero)
                return false;

            if (pinvokeHandler.IsWindowVisible(trayHwnd))
                pinvokeHandler.SetWindowPos(trayHwnd, IntPtr.Zero, 0, 0, 0, 0, SetWindowPosFlags.SWP_HIDEWINDOW | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);
            else
                pinvokeHandler.SetWindowPos(trayHwnd, IntPtr.Zero, 0, 0, 0, 0, SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);

            return false;
        }

        public bool CmdWinMenu(string cmd)
        {
            Log.Information("################# GOING TO SHOW WIN MENU ###################");
            //var startMenu = pinvokeHandler.FindWindow("DV2ControlHost", "Start menu");
            //pinvokeHandler.ShowWindow(startMenu, ShowWindowCmd.SW_SHOW);
            //pinvokeHandler.SendMessage(startMenu, PInvoker.WM_LBUTTONDOWN, new IntPtr(PInvoker.MK_LBUTTON), new IntPtr(5 + (5<<8)));

            var hTaskBarWnd = pinvokeHandler.FindWindow("Shell_TrayWnd", null);
            var hStartButtonWnd = pinvokeHandler.GetWindow(hTaskBarWnd, GetWindowType.GW_CHILD);
            pinvokeHandler.SendMessage(hStartButtonWnd, PInvoker.WM_LBUTTONDOWN, new IntPtr(PInvoker.MK_LBUTTON), new IntPtr(5 + (5<<8)));
            pinvokeHandler.SendMessage(hStartButtonWnd, PInvoker.WM_LBUTTONUP, new IntPtr(PInvoker.MK_LBUTTON), new IntPtr(5 + (5<<8)));
            return false;
        }

        public bool CmdRestart(string cmd)
        {
            Startup.ParserSignal.SignalRestartThreads();
            return false;
        }

        public bool CmdRun(string cmd)
        {
            Process.Start(new ProcessStartInfo
            {
                Arguments = "Shell:::{2559a1f3-21d7-11d4-bdaf-00c04f60b9f0}",
                FileName = "explorer.exe",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            });
            return false;
        }
        
        public bool CmdDebugGraph(string cmd)
        {
            var desktop = desktops.ActiveDesktop;
            desktop.HandleMoveFocus(TransferDirection.Down);
            var travler = new NodeTraveler();
            travler.Travel(desktop);
            var rootDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            rootDir = Path.Combine(rootDir, "output");
            if (!Directory.Exists(rootDir))
                Directory.CreateDirectory(rootDir);

            var index = Directory.GetFiles(rootDir, "image*.dot")
                .Select(fullPath => Path.GetFileNameWithoutExtension(fullPath))
                .Select(fileName => Convert.ToInt64(fileName.Substring(5)))
                .OrderByDescending(i => i)
                .FirstOrDefault() + 1;
            var dotFile = Path.Combine(rootDir, $"image{index}.dot");
            var pngFile = Path.Combine(rootDir, $"image{index}.png");
            File.WriteAllText(dotFile, travler.GetOutput());

            var dotExe = "c:\\Program Files (x86)\\Graphviz2.38\\bin\\dot.exe";
            if (File.Exists(dotExe) == false)
                return false;

            Process.Start(dotExe, $"-Tpng -o\"{pngFile}\" \"{dotFile}\"");
            return false;
        }

        public bool CmdLayoutStacking(string cmd)
        {
            desktops.ActiveDesktop.HandleToggleStackLayout();
            return false;
        }

        public bool CmdFloatingToggle(string cmd)
        {
            desktops.ActiveDesktop.HandleSwitchFloating();
            return false;
        }

        public bool CmdMoveToWorkspace(string cmd)
        {
            var index = Convert.ToInt32(cmd);
            var src = desktops.ActiveDesktop;
            var dst = desktops[index];
            if (dst == null)
                return false;
            src.TransferFocusNodeToDesktop(dst);
            return false;
        }

        public bool CmdShowWorkspace(string cmd)
        {
            desktops.Index = Convert.ToInt32(cmd);
            return false;
        }

        public bool CmdKill(string cmd)
        {
            desktops.ActiveDesktop.QuitFocusNode();
            return false;
        }

        public bool CmdSplitVertical(string cmd)
        {
            desktops.ActiveDesktop.HandleVerticalDirection();
            return false;
        }
        public bool CmdSplitHorizontal(string cmd)
        {
            desktops.ActiveDesktop.HandleHorizontalDirection();
            return false;
        }

        public bool CmdSplitToggle(string cmd)
        {
            desktops.ActiveDesktop.HandleHorizontalDirection();
            return false;
        }

        public bool CmdFullscreen(string cmd)
        {
            desktops.ActiveDesktop.HandleFullscreen();
            return false;
        }

        public bool CmdResizeLeft(string cmd)
        {
            desktops.ActiveDesktop.HandleResize(Convert.ToInt32(cmd), TransferDirection.Right);
            return true;
        }

        public bool CmdResizeUp(string cmd)
        {
            desktops.ActiveDesktop.HandleResize(Convert.ToInt32(cmd), TransferDirection.Down);
            return true;
        }

        public bool CmdResizeRight(string cmd)
        {
            desktops.ActiveDesktop.HandleResize(Convert.ToInt32(cmd), TransferDirection.Right);
            return true;
        }

        public bool CmdResizeDown(string cmd)
        {
            desktops.ActiveDesktop.HandleResize(Convert.ToInt32(cmd), TransferDirection.Down);
            return true;
        }

        public bool CmdMoveLeft(string cmd)
        {
            return desktops.ActiveDesktop.HandleMoveNodeLeft();
        }

        public bool CmdMoveUp(string cmd)
        {
            return desktops.ActiveDesktop.HandleMoveNodeUp();
        }

        public bool CmdMoveRight(string cmd)
        {
            return desktops.ActiveDesktop.HandleMoveNodeRight();
        }

        public bool CmdMoveDown(string cmd)
        {
            return desktops.ActiveDesktop.HandleMoveNodeDown();
        }

        public bool CmdFocusLeft(string cmd)
        {
            desktops.ActiveDesktop.HandleMoveFocus(TransferDirection.Left);
            return false;
        }

        public bool CmdFocusUp(string cmd)
        {
            desktops.ActiveDesktop.HandleMoveFocus(TransferDirection.Up);
            return false;
        }

        public bool CmdFocusRight(string cmd)
        {
            desktops.ActiveDesktop.HandleMoveFocus(TransferDirection.Right);
            return false;
        }

        public bool CmdFocusDown(string cmd)
        {
            desktops.ActiveDesktop.HandleMoveFocus(TransferDirection.Down);
            return false;
        }
    }
}