using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Serilog;
using TileWindow.Extra.GraphViz;
using TileWindow.Handlers.I3wm.Nodes;

namespace TileWindow.Handlers.I3wm
{
    public interface ICommandHelper
    {
        Func<bool> GetCommand(string command);
    }

    public class CommandHelper: ICommandHelper
    {
        private readonly IVirtualDesktopCollection desktops;
        private readonly IPInvokeHandler pinvokeHandler;
        public static readonly string cmd_move_left = "[MoveLeft]";
        public static readonly string cmd_move_up = "[MoveUp]";
        public static readonly string cmd_move_right = "[MoveRight]";
        public static readonly string cmd_move_down = "[MoveDown]";
        public static readonly string cmd_focus_left = "[FocusLeft]";
        public static readonly string cmd_focus_up = "[FocusUp]";
        public static readonly string cmd_focus_right = "[FocusRight]";
        public static readonly string cmd_focus_down = "[FocusDown]";
        public static readonly string cmd_run = "[WinRun]";
        public static readonly string cmd_horizontal = "[Horizontal]";
        public static readonly string cmd_vertical = "[Vertical]";
        public static readonly string cmd_debug = "[Debug]";
        public static readonly string cmd_quit = "[Quit]";
        public static readonly string cmd_fullscreen = "[Fullscreen]";
        public static readonly string cmd_resize_left = "[ResizeLeft]";
        public static readonly string cmd_resize_up = "[ResizeUp]";
        public static readonly string cmd_resize_right = "[ResizeRight]";
        public static readonly string cmd_resize_down = "[ResizeDown]";
        public static readonly string cmd_show_desktop1 = "[ShowDesktop1]";
        public static readonly string cmd_show_desktop2 = "[ShowDesktop2]";
        public static readonly string cmd_show_desktop3 = "[ShowDesktop3]";
        public static readonly string cmd_show_desktop4 = "[ShowDesktop4]";
        public static readonly string cmd_show_desktop5 = "[ShowDesktop5]";
        public static readonly string cmd_show_desktop6 = "[ShowDesktop6]";
        public static readonly string cmd_show_desktop7 = "[ShowDesktop7]";
        public static readonly string cmd_show_desktop8 = "[ShowDesktop8]";
        public static readonly string cmd_show_desktop9 = "[ShowDesktop9]";
        public static readonly string cmd_show_desktop10 = "[ShowDesktop10]";
        public static readonly string cmd_move_to_desktop1 = "[MoveToDesktop1]";
        public static readonly string cmd_move_to_desktop2 = "[MoveToDesktop2]";
        public static readonly string cmd_move_to_desktop3 = "[MoveToDesktop3]";
        public static readonly string cmd_move_to_desktop4 = "[MoveToDesktop4]";
        public static readonly string cmd_move_to_desktop5 = "[MoveToDesktop5]";
        public static readonly string cmd_move_to_desktop6 = "[MoveToDesktop6]";
        public static readonly string cmd_move_to_desktop7 = "[MoveToDesktop7]";
        public static readonly string cmd_move_to_desktop8 = "[MoveToDesktop8]";
        public static readonly string cmd_move_to_desktop9 = "[MoveToDesktop9]";
        public static readonly string cmd_move_to_desktop10 = "[MoveToDesktop10]";
        public static readonly string cmd_switch_floating = "[SwitchFloating]";
        public static readonly string cmd_showwinmenu = "[ShowWinMenu]";
        public static readonly string cmd_restartTW = "[RestartTW]";
        public static readonly string cmd_toggleTaskbar = "[ToggleTaskbar]";

        public CommandHelper(IVirtualDesktopCollection desktops, IPInvokeHandler pinvokeHandler)
        {
            this.desktops = desktops;
            this.pinvokeHandler = pinvokeHandler;
        }
        
        public Func<bool> GetCommand(string command)
        {
            if (cmd_move_left.Equals(command))
                return () => MoveLeft();
            if (cmd_move_up.Equals(command))
                return () => MoveUp();
            if (cmd_move_right.Equals(command))
                return () => MoveRight();
            if (cmd_move_down.Equals(command))
                return () => MoveDown();
            if (cmd_focus_left.Equals(command))
                return () => FocusLeft();
            if (cmd_focus_up.Equals(command))
                return () => FocusUp();
            if (cmd_focus_right.Equals(command))
                return () => FocusRight();
            if (cmd_focus_down.Equals(command))
                return () => FocusDown();
            if (cmd_run.Equals(command))
                return () => CmdRun();
            if (cmd_debug.Equals(command))
                return () => CmdDebug();
            if (cmd_quit.Equals(command))
                return () => CmdQuit();
            if (cmd_vertical.Equals(command))
                return () => CmdVertical();
            if (cmd_horizontal.Equals(command))
                return () => CmdHorizontal();
            if (cmd_fullscreen.Equals(command))
                return () => CmdFullscreen();
            if (cmd_resize_left.Equals(command))
                return () => CmdResizeLeft();
            if (cmd_resize_up.Equals(command))
                return () => CmdResizeUp();
            if (cmd_resize_right.Equals(command))
                return () => CmdResizeRight();
            if (cmd_resize_down.Equals(command))
                return () => CmdResizeDown();
            if (cmd_show_desktop1.Equals(command))
                return () => CmdShowDesktop(0);
            if (cmd_show_desktop2.Equals(command))
                return () => CmdShowDesktop(1);
            if (cmd_show_desktop3.Equals(command))
                return () => CmdShowDesktop(2);
            if (cmd_show_desktop4.Equals(command))
                return () => CmdShowDesktop(3);
            if (cmd_show_desktop5.Equals(command))
                return () => CmdShowDesktop(4);
            if (cmd_show_desktop6.Equals(command))
                return () => CmdShowDesktop(5);
            if (cmd_show_desktop7.Equals(command))
                return () => CmdShowDesktop(6);
            if (cmd_show_desktop8.Equals(command))
                return () => CmdShowDesktop(7);
            if (cmd_show_desktop9.Equals(command))
                return () => CmdShowDesktop(8);
            if (cmd_show_desktop10.Equals(command))
                return () => CmdShowDesktop(9);
            if (cmd_move_to_desktop1.Equals(command))
                return () => CmdMoveToDesktop(0);
            if (cmd_move_to_desktop2.Equals(command))
                return () => CmdMoveToDesktop(1);
            if (cmd_move_to_desktop3.Equals(command))
                return () => CmdMoveToDesktop(2);
            if (cmd_move_to_desktop4.Equals(command))
                return () => CmdMoveToDesktop(3);
            if (cmd_move_to_desktop5.Equals(command))
                return () => CmdMoveToDesktop(4);
            if (cmd_move_to_desktop6.Equals(command))
                return () => CmdMoveToDesktop(5);
            if (cmd_move_to_desktop7.Equals(command))
                return () => CmdMoveToDesktop(6);
            if (cmd_move_to_desktop8.Equals(command))
                return () => CmdMoveToDesktop(7);
            if (cmd_move_to_desktop9.Equals(command))
                return () => CmdMoveToDesktop(8);
            if (cmd_move_to_desktop10.Equals(command))
                return () => CmdMoveToDesktop(9);
            if (cmd_switch_floating.Equals(command))
                return () => CmdSwitchFloating();
            if (cmd_showwinmenu.Equals(command))
                return () => CmdWinMenu();
            if (cmd_restartTW.Equals(command))
                return () => CmdRestartTW();
            if (cmd_toggleTaskbar.Equals(command))
                return () => CmdToggleTaskbar();

            return () => ExternalCommand(command);
        }

        private bool ExternalCommand(string command)
        {
            Log.Warning($"ExternalCommand: \"{command}\" not implemented");
            return false;
        }

        private bool CmdToggleTaskbar()
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

        private bool CmdWinMenu()
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

        private bool CmdRestartTW()
        {
            Startup.ParserSignal.SignalRestartThreads();
            return false;
        }

        private bool CmdRun()
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
        private bool CmdDebug()
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

        private bool CmdSwitchFloating()
        {
            desktops.ActiveDesktop.HandleSwitchFloating();
            return false;
        }

        private bool CmdMoveToDesktop(int index)
        {
            var src = desktops.ActiveDesktop;
            var dst = desktops[index];
            if (dst == null)
                return false;
            src.TransferFocusNodeToDesktop(dst);
            return false;
        }

        private bool CmdShowDesktop(int index)
        {
            desktops.Index = index;
            return false;
        }

        private bool CmdQuit()
        {
            desktops.ActiveDesktop.QuitFocusNode();
            return false;
        }

        private bool CmdVertical()
        {
            desktops.ActiveDesktop.HandleVerticalDirection();
            return false;
        }
        private bool CmdHorizontal()
        {
            desktops.ActiveDesktop.HandleHorizontalDirection();
            return false;
        }

        private bool CmdFullscreen()
        {
            desktops.ActiveDesktop.HandleFullscreen();
            return false;
        }

        private bool CmdResizeLeft()
        {
            desktops.ActiveDesktop.HandleResize(-20, TransferDirection.Right);
            return true;
        }

        private bool CmdResizeUp()
        {
            desktops.ActiveDesktop.HandleResize(-20, TransferDirection.Down);
            return true;
        }

        private bool CmdResizeRight()
        {
            desktops.ActiveDesktop.HandleResize(20, TransferDirection.Right);
            return true;
        }

        private bool CmdResizeDown()
        {
            desktops.ActiveDesktop.HandleResize(20, TransferDirection.Down);
            return true;
        }

        private bool MoveLeft()
        {
            return desktops.ActiveDesktop.HandleMoveNodeLeft();
        }

        private bool MoveUp()
        {
            return desktops.ActiveDesktop.HandleMoveNodeUp();
        }

        private bool MoveRight()
        {
            return desktops.ActiveDesktop.HandleMoveNodeRight();
        }

        private bool MoveDown()
        {
            return desktops.ActiveDesktop.HandleMoveNodeDown();
        }

        private bool FocusLeft()
        {
            desktops.ActiveDesktop.HandleMoveFocus(TransferDirection.Left);
            return false;
        }

        private bool FocusUp()
        {
            desktops.ActiveDesktop.HandleMoveFocus(TransferDirection.Up);
            return false;
        }

        private bool FocusRight()
        {
            desktops.ActiveDesktop.HandleMoveFocus(TransferDirection.Right);
            return false;
        }

        private bool FocusDown()
        {
            desktops.ActiveDesktop.HandleMoveFocus(TransferDirection.Down);
            return false;
        }
    }
}