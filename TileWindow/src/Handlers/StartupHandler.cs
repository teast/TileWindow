using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Serilog;
using TileWindow.Dto;
using TileWindow.Extensions;
using TileWindow.Nodes;
using TileWindow.Nodes.Creaters;
using TileWindow.Trackers;

namespace TileWindow.Handlers
{
    public interface IStartupHandler: IHandler
    {
    }

    public class StartupHandler: IStartupHandler
    {
        private readonly IContainerNodeCreater containerNodeCreator;
        private readonly ISignalHandler signal;
        private readonly IKeyHandler keyHandler;
        private readonly ICommandHelper commandHelper;
        private readonly IWindowTracker windowTracker;
        private readonly IPInvokeHandler pinvokeHandler;
        private List<IntPtr> HandlesToIgnore;
        private readonly IScreens screensInfo;
        private IVirtualDesktopCollection desktops;

        private RECT[] _screens;

        public StartupHandler(IScreens screensInfo, IVirtualDesktopCollection virtualDesktops, IContainerNodeCreater containerNodeCreator, IVirtualDesktopCreater virtualDesktopCreator, IScreenNodeCreater screenNodeCreator, ISignalHandler signal, IKeyHandler keyHandler, ICommandHelper commandHelper, IWindowTracker windowTracker, IPInvokeHandler pinvokeHandler)
        {
            this.screensInfo = screensInfo;
            this.desktops = virtualDesktops;
            this.containerNodeCreator = containerNodeCreator;
            this.signal = signal;
            this.keyHandler = keyHandler;
            this.commandHelper = commandHelper;
            this.windowTracker = windowTracker;
            this.pinvokeHandler = pinvokeHandler;
            this.HandlesToIgnore = new List<IntPtr>();
            var screens = new List<ContainerNode>();

            var result = screensInfo.AllScreens.GetOrderRect();
            _screens = result.rect.ToArray();

            for(var i = 0; i < desktops.Count; i++)
            {
                var screensToAdd = _screens.Select((rect, i) => screenNodeCreator.Create("Screen" + i, rect, dir: result.direction)).ToArray();
                desktops[i] = virtualDesktopCreator.Create(i, rect: _screens.TotalRect(), dir: result.direction, childs: screensToAdd);
                desktops[i].Hide();
            }

            desktops.Index = 0;
            desktops.ActiveDesktop.Show();
        }

        public void ReadConfig(AppConfig config)
        {
            var shortcuts = new Dictionary<string, string>(config.Custom ?? new Dictionary<string, string>());
            if(string.IsNullOrEmpty(config.FocusLeft) == false) shortcuts.Add(config.FocusLeft, CommandHelper.cmd_focus_left);
            if(string.IsNullOrEmpty(config.FocusUp) == false) shortcuts.Add(config.FocusUp, CommandHelper.cmd_focus_up);
            if(string.IsNullOrEmpty(config.FocusRight) == false) shortcuts.Add(config.FocusRight, CommandHelper.cmd_focus_right);
            if(string.IsNullOrEmpty(config.FocusDown) == false) shortcuts.Add(config.FocusDown, CommandHelper.cmd_focus_down);
            if(string.IsNullOrEmpty(config.MoveLeft) == false) shortcuts.Add(config.MoveLeft, CommandHelper.cmd_move_left);
            if(string.IsNullOrEmpty(config.MoveUp) == false) shortcuts.Add(config.MoveUp, CommandHelper.cmd_move_up);
            if(string.IsNullOrEmpty(config.MoveRight) == false) shortcuts.Add(config.MoveRight, CommandHelper.cmd_move_right);
            if(string.IsNullOrEmpty(config.MoveDown) == false) shortcuts.Add(config.MoveDown, CommandHelper.cmd_move_down);
            if(string.IsNullOrEmpty(config.WinRun) == false) shortcuts.Add(config.WinRun, CommandHelper.cmd_run);
            if(string.IsNullOrEmpty(config.Debug) == false) shortcuts.Add(config.Debug, CommandHelper.cmd_debug);
            if(string.IsNullOrEmpty(config.Quit) == false) shortcuts.Add(config.Quit, CommandHelper.cmd_quit);
            if(string.IsNullOrEmpty(config.Horizontal) == false) shortcuts.Add(config.Horizontal, CommandHelper.cmd_horizontal);
            if(string.IsNullOrEmpty(config.Vertical) == false) shortcuts.Add(config.Vertical, CommandHelper.cmd_vertical);
            if(string.IsNullOrEmpty(config.Fullscreen) == false) shortcuts.Add(config.Fullscreen, CommandHelper.cmd_fullscreen);
            if(string.IsNullOrEmpty(config.ResizeLeft) == false) shortcuts.Add(config.ResizeLeft, CommandHelper.cmd_resize_left);
            if(string.IsNullOrEmpty(config.ResizeUp) == false) shortcuts.Add(config.ResizeUp, CommandHelper.cmd_resize_up);
            if(string.IsNullOrEmpty(config.ResizeRight) == false) shortcuts.Add(config.ResizeRight, CommandHelper.cmd_resize_right);
            if(string.IsNullOrEmpty(config.ResizeDown) == false) shortcuts.Add(config.ResizeDown, CommandHelper.cmd_resize_down);
            if(string.IsNullOrEmpty(config.ShowDesktop1) == false) shortcuts.Add(config.ShowDesktop1, CommandHelper.cmd_show_desktop1);
            if(string.IsNullOrEmpty(config.ShowDesktop2) == false) shortcuts.Add(config.ShowDesktop2, CommandHelper.cmd_show_desktop2);
            if(string.IsNullOrEmpty(config.ShowDesktop3) == false) shortcuts.Add(config.ShowDesktop3, CommandHelper.cmd_show_desktop3);
            if(string.IsNullOrEmpty(config.ShowDesktop4) == false) shortcuts.Add(config.ShowDesktop4, CommandHelper.cmd_show_desktop4);
            if(string.IsNullOrEmpty(config.ShowDesktop5) == false) shortcuts.Add(config.ShowDesktop5, CommandHelper.cmd_show_desktop5);
            if(string.IsNullOrEmpty(config.ShowDesktop6) == false) shortcuts.Add(config.ShowDesktop6, CommandHelper.cmd_show_desktop6);
            if(string.IsNullOrEmpty(config.ShowDesktop7) == false) shortcuts.Add(config.ShowDesktop7, CommandHelper.cmd_show_desktop7);
            if(string.IsNullOrEmpty(config.ShowDesktop8) == false) shortcuts.Add(config.ShowDesktop8, CommandHelper.cmd_show_desktop8);
            if(string.IsNullOrEmpty(config.ShowDesktop9) == false) shortcuts.Add(config.ShowDesktop9, CommandHelper.cmd_show_desktop9);
            if(string.IsNullOrEmpty(config.ShowDesktop10) == false) shortcuts.Add(config.ShowDesktop10, CommandHelper.cmd_show_desktop10);
            if(string.IsNullOrEmpty(config.MoveToDesktop1) == false) shortcuts.Add(config.MoveToDesktop1, CommandHelper.cmd_move_to_desktop1);
            if(string.IsNullOrEmpty(config.MoveToDesktop2) == false) shortcuts.Add(config.MoveToDesktop2, CommandHelper.cmd_move_to_desktop2);
            if(string.IsNullOrEmpty(config.MoveToDesktop3) == false) shortcuts.Add(config.MoveToDesktop3, CommandHelper.cmd_move_to_desktop3);
            if(string.IsNullOrEmpty(config.MoveToDesktop4) == false) shortcuts.Add(config.MoveToDesktop4, CommandHelper.cmd_move_to_desktop4);
            if(string.IsNullOrEmpty(config.MoveToDesktop5) == false) shortcuts.Add(config.MoveToDesktop5, CommandHelper.cmd_move_to_desktop5);
            if(string.IsNullOrEmpty(config.MoveToDesktop6) == false) shortcuts.Add(config.MoveToDesktop6, CommandHelper.cmd_move_to_desktop6);
            if(string.IsNullOrEmpty(config.MoveToDesktop7) == false) shortcuts.Add(config.MoveToDesktop7, CommandHelper.cmd_move_to_desktop7);
            if(string.IsNullOrEmpty(config.MoveToDesktop8) == false) shortcuts.Add(config.MoveToDesktop8, CommandHelper.cmd_move_to_desktop8);
            if(string.IsNullOrEmpty(config.MoveToDesktop9) == false) shortcuts.Add(config.MoveToDesktop9, CommandHelper.cmd_move_to_desktop9);
            if(string.IsNullOrEmpty(config.MoveToDesktop10) == false) shortcuts.Add(config.MoveToDesktop10, CommandHelper.cmd_move_to_desktop10);
            if(string.IsNullOrEmpty(config.SwitchFloating) == false) shortcuts.Add(config.SwitchFloating, CommandHelper.cmd_switch_floating);
            if(string.IsNullOrEmpty(config.ShowWinMenu) == false) shortcuts.Add(config.ShowWinMenu, CommandHelper.cmd_showwinmenu);
            if(string.IsNullOrEmpty(config.RestartTW) == false) shortcuts.Add(config.RestartTW, CommandHelper.cmd_restartTW);
            if(string.IsNullOrEmpty(config.ToggleTaskbar) == false) shortcuts.Add(config.ToggleTaskbar, CommandHelper.cmd_toggleTaskbar);
            if(string.IsNullOrEmpty(config.ToggleStackLayout) == false) shortcuts.Add(config.ToggleStackLayout, CommandHelper.cmd_toggleStackLayout);

            ValidateAndAddKeyShortcuts(shortcuts);

            IntPtr trayHwnd;
            if (config.HideTaskbar && (trayHwnd = pinvokeHandler.FindWindow("Shell_TrayWnd", null)) != IntPtr.Zero)
                pinvokeHandler.SetWindowPos(trayHwnd, IntPtr.Zero, 0, 0, 0, 0, SetWindowPosFlags.SWP_HIDEWINDOW | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);
        }

        private void ValidateAndAddKeyShortcuts(Dictionary<string, string> shortcuts)
        {
            foreach(var shortcut in shortcuts)
            {
                if (string.IsNullOrEmpty(shortcut.Value))
                {
                    Log.Warning($"Shortcut command is empty \"{shortcut.Key}\"");
                    continue;
                }
                if (keyHandler.GetKeyCombination(shortcut.Key, out ulong[] keys) == false)
                {
                    Log.Warning($"Problem with \"{shortcut.Key}\": \"{shortcut.Value}\" (see previous warning(s))");
                    continue;
                }

                var cmd = commandHelper.GetCommand(shortcut.Value);
                keyHandler.AddListener(keys, keyCombo => cmd());
            }
        }

        public void Init()
        {
            TakeoverExistingWindows();
        }

        public void Quit()
        {
            foreach(var desktop in desktops)
                desktop.Restore();
            
            var trayHwnd = pinvokeHandler.FindWindow("Shell_TrayWnd", null);
            if (trayHwnd != IntPtr.Zero)
            {
                if (pinvokeHandler.IsWindowVisible(trayHwnd) == false)
                    pinvokeHandler.SetWindowPos(trayHwnd, IntPtr.Zero, 0, 0, 0, 0, SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);
            }
        }

        private static bool EnumProc(IntPtr hWnd, ref EnumExtraData data)
        {
            data.Hwnd.Add(new IntPtr(hWnd.ToInt64()));
            return true;
        }

        private void TakeoverExistingWindows()
        {
            var maxProgPerRow = 4;

            var sb = new EnumExtraData();
            var result = pinvokeHandler.EnumWindows(new EnumWindowsProc(EnumProc), ref sb);
            if (result == false)
                Log.Warning($"Error from EnumWindows: {pinvokeHandler.GetLastError()}");

            var windowsToHandle = sb.Hwnd
                .Select(hwnd => windowTracker.CreateNode(hwnd))
                .Where(n => n != null);
            var programPerScreen = windowsToHandle.Select(node => {
                if (node.Style == NodeStyle.Floating)
                    return new {
                        node = node,
                        Screen = -1
                    };

                var screen = Tuple.Create((int)0, (long)0);
                for(int i = 0; i < _screens.Length; i++)
                {
                    long area = 0;
                    if ((area = _screens[i].Intersection(node.Rect).CalcArea()) > screen.Item2)
                    {
                        screen = Tuple.Create(i, area);
                    }
                }

                return new {
                    node = node,
                    Screen = screen.Item1
                };
            })
            .GroupBy(wind => wind.Screen).ToDictionary(s => s.Key, s => s.Select(s2 => s2.node).ToList());

            foreach(var progs in programPerScreen)
            {
                // -1 == floating nodes
                if (progs.Key == -1)
                {
                    progs.Value.ForEach(node => desktops.ActiveDesktop.AddFloatingNode(node));
                    continue;
                }

//Log.Information($"Screen[{progs.Key}] got {progs.Value.Count} windows to sort out");
                if (!desktops.ActiveDesktop.GetScreenRect(progs.Key, out RECT sr))
                {
                    Log.Error($"Could not get screen rect for screen index {progs.Key}");
                    continue;
                }

                var rows = (int)Math.Ceiling((decimal)progs.Value.Count / maxProgPerRow);
                desktops.ActiveDesktop.Screen(progs.Key).ChangeDirection(rows > 1 ? Direction.Vertical : Direction.Horizontal);

                ContainerNode wn = null;
                var row = 0;
                var col = 0;
                Node lastAdded = null;

                if (rows > 1)
                {
                    wn = containerNodeCreator.Create(sr);
                    desktops.ActiveDesktop.Screen(progs.Key).AddNodes(wn);
                }
                else
                    wn = desktops.ActiveDesktop.Screen(progs.Key);

                foreach(var node in progs.Value)
                {
                    if(wn.AddNodes(node))
                        lastAdded = node;

                    col++;
                    if (col == maxProgPerRow)
                    {
                        row++;
                        col=0;
                        if (rows > 1)
                        {
                            wn = containerNodeCreator.Create(sr);
                            desktops.ActiveDesktop.Screen(progs.Key).AddNodes(wn);
                        }
                    }
                }

                if (lastAdded != null && progs.Key == desktops.ActiveDesktop.ActiveScreenIndex)
                    lastAdded.SetFocus();
            }
        }

        private string GetWindowText(IntPtr Hwnd)
        {
            var tb = new StringBuilder(1024);
            if (pinvokeHandler.GetWindowText(Hwnd, tb, tb.Capacity) > 0)
                return tb.ToString();
            else
                return Hwnd.ToString();
        }

        private string GetClassName(IntPtr Hwnd)
        {
            var cb = new StringBuilder(1024);
            pinvokeHandler.GetClassName(Hwnd, cb, cb.Capacity);

            return cb.ToString();
        }

		public void HandleMessage(PipeMessage msg)
		{
			if(msg.msg == signal.WMC_CREATE)
			{
                //HandleMessageCreate(msg);
                //Log.Information($"WMC_CREATE wParam: {msg.wParam}, lparam: {msg.lParam} \"{GetWindowText(new IntPtr((long)msg.wParam))}\" [{GetClassName(new IntPtr((long)msg.wParam))}]");
			}
			else if (msg.msg == signal.WMC_SHOW)
			{
                HandleMessageShow(msg);
			}
            else if (msg.msg == signal.WMC_SHOWWINDOW)
            {
                HandleMessageShowWindow(msg);
            }
            else if (msg.msg == signal.WMC_DESTROY)
            {
                HandleMessageDestroy(msg);
            }
            else if (msg.msg == signal.WMC_SCCLOSE)
            {
                var hwnd = new IntPtr((long)msg.wParam);
                var node = windowTracker.GetNodes(hwnd);
                //Log.Information($"#################### NEW MESSAGE!!! SCCLOSE for {hwnd} ({node}) ###############");                
            }
            else if (msg.msg == signal.WMC_SCRESTORE)
            {
                var hwnd = new IntPtr((long)msg.wParam);
                var node = windowTracker.GetNodes(hwnd);
                //Log.Information($"#################### NEW MESSAGE!!! SCRESTORE for {hwnd} ({node}) ###############");                
            }
            else if (msg.msg == signal.WMC_ACTIVATEAPP)
            {
                // If the current hwnd is the one receiving focus
                if (msg.lParam != 0)
                {
                    var hwnd = new IntPtr((long)msg.wParam);
                    var node = windowTracker.GetNodes(hwnd);
                    if (node == null)
                    {
                        var old = windowTracker.IgnoreVisualFlag;
                        windowTracker.IgnoreVisualFlag = true;
                        if (windowTracker.CanHandleHwnd(hwnd))
                            desktops.ActiveDesktop.HandleNewWindow(hwnd);
                        windowTracker.IgnoreVisualFlag = old;
                    }
                }
            }
            else if (msg.msg == signal.WMC_DISPLAYCHANGE)
            {
                var result = screensInfo.AllScreens.GetOrderRect();
                var screens = result.rect.ToArray();
                var diff = true;
                if (_screens.Length == screens.Length)
                {
                    diff = false;
                    for(var i = 0; i < _screens.Length; i++)
                    {
                        if (!_screens[i].Equals(screens[i]))
                        {
                            diff = true;
                            break;
                        }
                    }

                }

                if (diff)
                {
                    _screens = screens;
                    foreach(var desktop in desktops)
                        desktop.ScreensChanged(_screens, result.direction);
                    
                    Startup.ParserSignal.SignalRestartThreads();
                }
            }
            else if (msg.msg == signal.WMC_SHOWNODE)
            {
                var node = desktops.ActiveDesktop.FindNodeWithId((long)msg.wParam);
                node?.SetFocus();
            }
		}

        public void Dispose()
        {
        }

		private void HandleMessageShowWindow(PipeMessage msg)
        {
            //Log.Information($"StartupHandler> WMC_ShowWindow received for {msg.wParam} value: {msg.lParam} \"{GetWindowText(new IntPtr((long)msg.wParam))}\" [{GetClassName(new IntPtr((long)msg.wParam))}]");
        }
		private void HandleMessageDestroy(PipeMessage msg)
        {
            //Log.Information($"StartupHandler> WMC_DESTROY received for {msg.wParam} value: {msg.lParam} \"{GetWindowText(new IntPtr((long)msg.wParam))}\" [{GetClassName(new IntPtr((long)msg.wParam))}]");
            desktops.ActiveDesktop.HandleMessageDestroy(msg);
        }

		private void HandleMessageShow(PipeMessage msg)
        {
            var hwnd = new IntPtr(Convert.ToInt64(msg.wParam));
            //Log.Information($"StartupHandler> WMC_SHOW received for {msg.wParam} value: {msg.lParam} \"{GetWindowText(new IntPtr((long)msg.wParam))}\" [{GetClassName(new IntPtr((long)msg.wParam))}]");
            
            // Show signal and not hide
            if(msg.lParam == 1)
            {
                var node = windowTracker.GetNodes(hwnd);
                if (node != null)
                {
                    if (windowTracker.RevalidateHwnd(node as WindowNode, hwnd) == false)
                    {
                        node.Parent?.RemoveChild(node);
                        return;
                    }

                    if (node.Desktop != null)
                    {
                        if (node.Desktop.Index == desktops.Index)
                        {
                            Log.Information($"WMC_SHOW lParam == 1 and node exists, going to show it! ({node}) ({node.Desktop.Index} != {desktops.Index})");
                            node.Show();
                        }
                        else
                        {
                            Log.Information($"WMC_SHOW lParam == 1 and node exists, going to hide it! ({node}) ({node.Desktop.Index} != {desktops.Index})");
                            node.Hide();
                        }
                    }
                    else
                    {
                        Log.Warning($"WindowNode.Desktop is null ({node})");
                    }
                }
                else
                {
                    if (windowTracker.CanHandleHwnd(hwnd))
                    {
                        node = desktops.ActiveDesktop.HandleNewWindow(hwnd);
                        node?.SetFocus();
                    }
                }

                //PInvokeHandler.SetWindowPos(hwnd, IntPtr.Zero, tile.X, tile.Y, tile.Width, tile.Height, 0);
            }
            else if (msg.lParam == 0)
            {
                var node = windowTracker.GetNodes(hwnd);
                if (node != null)
                {
                    //Log.Information($"StartupHandler> WMC_SHOW going to remove node with name \"{node.Name}\"");
                    node.Parent?.DisconnectChild(node);
                    node.Dispose();
                }
            }
        }
    }
}