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
    public interface IStartupHandler : IHandler
    {
    }

    public class StartupHandler : IStartupHandler
    {
        private readonly IContainerNodeCreater containerNodeCreator;
        private readonly ISignalHandler signal;
        private readonly IKeyHandler keyHandler;
        private readonly ICommandHelper commandHelper;
        private readonly IWindowTracker windowTracker;
        private readonly IPInvokeHandler pinvokeHandler;
        private readonly List<IntPtr> HandlesToIgnore;
        private readonly IScreens screensInfo;
        private readonly IVirtualDesktopCollection desktops;

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

            for (var i = 0; i < desktops.Count; i++)
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
            var shortcuts = config.KeyBinds ?? new Dictionary<string, string>();
            ValidateAndAddKeyShortcuts(shortcuts);

            IntPtr trayHwnd;
            if (config.HideTaskbar && (trayHwnd = pinvokeHandler.FindWindow("Shell_TrayWnd", null)) != IntPtr.Zero)
            {
                pinvokeHandler.SetWindowPos(trayHwnd, IntPtr.Zero, 0, 0, 0, 0, SetWindowPosFlags.SWP_HIDEWINDOW | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);
            }
        }

        private void ValidateAndAddKeyShortcuts(Dictionary<string, string> shortcuts)
        {
            foreach (var shortcut in shortcuts)
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

                keyHandler.AddListener(keys, keyCombo => commandHelper.GetCommand(shortcut.Value));
            }
        }

        public void Init()
        {
            TakeoverExistingWindows();
        }

        public void Quit()
        {
            foreach (var desktop in desktops)
            {
                desktop.Restore();
            }
            
            var trayHwnd = pinvokeHandler.FindWindow("Shell_TrayWnd", null);
            if (trayHwnd != IntPtr.Zero)
            {
                if (pinvokeHandler.IsWindowVisible(trayHwnd) == false)
                {
                    pinvokeHandler.SetWindowPos(trayHwnd, IntPtr.Zero, 0, 0, 0, 0, SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);
                }
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
            {
                Log.Warning($"Error from EnumWindows: {pinvokeHandler.GetLastError()}");
            }

            var windowsToHandle = sb.Hwnd
                .Select(hwnd => windowTracker.CreateNode(hwnd))
                .Where(n => n != null);
            var programPerScreen = windowsToHandle.Select(node =>
            {
                if (node.Style == NodeStyle.Floating)
                {
                    return new
                    {
                        node,
                        Screen = -1
                    };
                }

                var screen = Tuple.Create((int)0, (long)0);
                for (int i = 0; i < _screens.Length; i++)
                {
                    long area = 0;
                    if ((area = _screens[i].Intersection(node.Rect).CalcArea()) > screen.Item2)
                    {
                        screen = Tuple.Create(i, area);
                    }
                }

                return new
                {
                    node = node,
                    Screen = screen.Item1
                };
            })
            .GroupBy(wind => wind.Screen).ToDictionary(s => s.Key, s => s.Select(s2 => s2.node).ToList());

            foreach (var progs in programPerScreen.ToList())
            {
                // -1 == floating nodes
                if (progs.Key == -1)
                {
                    progs.Value.ToList().ForEach(node => desktops.ActiveDesktop.AddFloatingNode(node));
                    continue;
                }

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
                {
                    wn = desktops.ActiveDesktop.Screen(progs.Key);
                }

                foreach (var node in progs.Value)
                {
                    if (wn.AddNodes(node))
                    {
                        lastAdded = node;
                    }

                    col++;
                    if (col == maxProgPerRow)
                    {
                        row++;
                        col = 0;
                        if (rows > 1)
                        {
                            wn = containerNodeCreator.Create(sr);
                            desktops.ActiveDesktop.Screen(progs.Key).AddNodes(wn);
                        }
                    }
                }

                if (lastAdded != null && progs.Key == desktops.ActiveDesktop.ActiveScreenIndex)
                {
                    lastAdded.SetFocus();
                }
            }
        }

        private string GetWindowText(IntPtr Hwnd)
        {
            var tb = new StringBuilder(1024);
            if (pinvokeHandler.GetWindowText(Hwnd, tb, tb.Capacity) > 0)
            {
                return tb.ToString();
            }
            else
            {
                return Hwnd.ToString();
            }
        }

        private string GetClassName(IntPtr Hwnd)
        {
            var cb = new StringBuilder(1024);
            pinvokeHandler.GetClassName(Hwnd, cb, cb.Capacity);

            return cb.ToString();
        }

        public void HandleMessage(PipeMessageEx msg)
        {

            if (msg.msg == signal.WMC_SHOW)
            {
                HandleMessageShow(msg);
            }
            else if (msg.msg == signal.WMC_SHOWWINDOW)
            {
                HandleMessageShowWindow(msg);
            }
            else if (msg.msg == signal.WMC_DESTROY)
            {
                HandleQuitWindow(new IntPtr((long)msg.wParam));
            }
            else if (msg.msg == signal.WMC_SCCLOSE)
            {
                HandleQuitWindow(new IntPtr((long)msg.wParam));
            }
            else if (msg.msg == signal.WMC_SCRESTORE)
            {
                var hwnd = new IntPtr((long)msg.wParam);
                var node = windowTracker.GetNodes(hwnd);
            }
            else if (msg.msg == signal.WMC_ACTIVATEAPP)
            {
                // If the current hwnd is the one receiving focus
                if (msg.lParam != 0)
                {
                    var hwnd = new IntPtr((long)msg.wParam);
                    var node = windowTracker.GetNodes(hwnd);
                    if (node == null && windowTracker.CanHandleHwnd(hwnd, new ValidateHwndParams(validateApplicationFrame: false)))
                    {
                        desktops.ActiveDesktop.HandleNewWindow(hwnd, new ValidateHwndParams(doValidate: false));
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
                    for (var i = 0; i < _screens.Length; i++)
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
                    foreach (var desktop in desktops)
                    {
                        desktop.ScreensChanged(_screens, result.direction);
                    }

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
            // Nothing to do here
        }

        private void HandleMessageShowWindow(PipeMessageEx msg)
        {
            // Nothing to do here yet...
        }
        private void HandleQuitWindow(IntPtr hwnd)
        {
            var n = windowTracker.GetNodes(hwnd);
            n?.QuitNode();
        }

        private void HandleMessageShow(PipeMessageEx msg)
        {
            var hwnd = new IntPtr(Convert.ToInt64(msg.wParam));

            // Show signal and not hide
            if (msg.lParam == 1)
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
                            node.Show();
                        }
                        else
                        {
                            node.Hide();
                        }
                    }
                    else
                    {
                        //Log.Warning($"WindowNode.Desktop is null ({node})");
                    }
                }
                else
                {
                    var canHandle = windowTracker.CanHandleHwnd(hwnd, new ValidateHwndParams(validatevisible: false));
                    if (canHandle)
                    {
                        node = desktops.ActiveDesktop.HandleNewWindow(hwnd, new ValidateHwndParams(doValidate: false));
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
                    node.Parent?.DisconnectChild(node);
                    node.Dispose();
                }
            }
        }

        public void DumpDebug()
        {
            Log.Information($"{nameof(StartupHandler)} active desktop #{desktops.ActiveDesktop?.Index + 1} (id: {desktops.ActiveDesktop?.Index})");
            commandHelper.GetCommand("debug graph");
        }
    }
}