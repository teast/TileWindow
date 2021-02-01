using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TileWindow.Dto;
using TileWindow.Nodes;

namespace TileWindow.Tests.Gherkin
{
    public class TestWorld
    {
        private bool _haveInit;
        private readonly Dictionary<string, TestWindowInfo> _windows;
        private readonly Dictionary<string, IScreenInfo> _allScreens;
        private readonly Mock<IPInvokeHandler> _pinvoke;
        private readonly Mock<IScreens> _screens;

        private readonly ConcurrentQueue<PipeMessageEx> _queue;
        private IVirtualDesktopCollection _desktops;
        private readonly ServiceProvider _services;
        private readonly AppConfig _config;
        private MessageParser _parser;


        public TestWorld()
        {
            _haveInit = false;
            _pinvoke = new Mock<IPInvokeHandler>();

            _screens = new Mock<IScreens>();
            _windows = new Dictionary<string, TestWindowInfo>();
            _allScreens = new Dictionary<string, IScreenInfo>();
            _screens.SetupGet(_ => _.AllScreens).Returns(() => _allScreens.Select(_ => _.Value));

            _queue = new ConcurrentQueue<PipeMessageEx>();
            _config = new AppConfig();

            // Make sure each window message has an unique fake uint number.
            _pinvoke.Setup(_ => _.RegisterWindowMessage(It.IsAny<string>())).Returns<string>(s => (uint)s.GetHashCode());

            _services = Ioc.Init(new ConfigurationBuilder().Build(), _queue)
            .AddSingleton<AppConfig>(_ => _config)
            .AddSingleton<IScreens>(_ => _screens.Object)
            .AddSingleton<IPInvokeHandler>(_ => _pinvoke.Object)
            .BuildServiceProvider();
        }

        public void CreateDesktop(int desktops)
        {
            _desktops = new VirtualDesktopCollection(9);
        }

        public void CreateScreen(string name, bool primary, int posX, int posY, int resolutionX, int resolutionY)
        {
            _allScreens.Add(name, new ScreenInfo(primary, new RECT(posX, posY, posX + resolutionX, posY + resolutionY)));
        }

        public void ScreenOrientation(string name, string orientation) => DoInit(() =>
        {
            var r = _allScreens[name];
            foreach (var c in _desktops.ActiveDesktop.Childs)
            {
                if (!r.WorkingArea.Equals(c.Rect))
                    continue;

                c.ChangeDirection(orientation.ToLowerInvariant() == "horizontal" ? Direction.Horizontal : Direction.Vertical);
                return;
            }
        });

        public void CreateWindow(string name, string screenName) => DoInit(() =>
        {
            _windows[name] = new TestWindowInfo(name, _allScreens[screenName].WorkingArea.CloneType());
            var hWnd = new IntPtr(_windows[name].Id);
            RECT rect;

            _pinvoke
                .Setup(_ => _.GetWindowLongPtr(hWnd, PInvoker.GWL_STYLE))
                .Returns(new IntPtr(PInvoker.WS_VISIBLE | PInvoker.WS_CAPTION | PInvoker.WS_SIZEBOX));
            _pinvoke
                .Setup(_ => _.GetWindowLongPtr(hWnd, PInvoker.GWL_EXSTYLE))
                .Returns(new IntPtr(0));

            _pinvoke
                .Setup(_ => _.GetClassName(hWnd, It.IsAny<StringBuilder>(), It.IsAny<int>()))
                .Returns<IntPtr, StringBuilder, int>((hWnd, cb, size) =>
                {
                    var lsize = name.Length;
                    cb.Append(name.Substring(0, lsize > size ? size : lsize));
                    return lsize > size ? size : lsize;
                });

            _pinvoke
                .Setup(_ => _.GetWindowText(hWnd, It.IsAny<StringBuilder>(), It.IsAny<int>()))
                .Returns<IntPtr, StringBuilder, int>((hWnd, cb, size) =>
                {
                    var lsize = name.Length;
                    cb.Append(name.Substring(0, lsize > size ? size : lsize));
                    return lsize > size ? size : lsize;
                });

            _pinvoke
                .Setup(_ => _.GetWindowRect(hWnd, out rect))
                .Returns(true)
                .Callback(new HwndWithRectCallback((IntPtr hwnd, out RECT r) => r = _windows[name].InitializeRect));

            _pinvoke
                .Setup(_ => _.SetWindowPos(hWnd, It.IsAny<IntPtr>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SetWindowPosFlags>()))
                .Returns(true);
            _pinvoke
                .Setup(_ => _.IsWindow(hWnd))
                .Returns(true);

            // Signal window create
            var show = _services.GetRequiredService<ISignalHandler>().WMC_SHOW;
            SendAndHandleMessage(show, 1, _windows[name].Id);
        });

        public void WindowFocus(string name) => DoInit(() =>
        {
            SendAndHandleMessage(_services.GetRequiredService<ISignalHandler>().WMC_SETFOCUS, 0, _windows[name].Id);
        });

        public void WindowPosition(string name, int position, string screen) => DoInit(() =>
        {
            var r = _allScreens[screen];
            foreach (var c in _desktops.ActiveDesktop.Childs)
            {
                if (!r.WorkingArea.Equals(c.Rect))
                    continue;

                var sc = c as ScreenNode;

                sc.Childs[position].Name.Should().Be(name);
                return;
            }

            throw new InvalidProgramException($"Screen '{screen}' not found when looking for window '{name}'.");
        });

        public void MoveFocus(string direction) => DoInit(() =>
        {
            TransferDirection dir;

            switch (direction.ToLowerInvariant())
            {
                case "left":
                    dir = TransferDirection.Left;
                    break;
                case "up":
                    dir = TransferDirection.Up;
                    break;
                case "right":
                    dir = TransferDirection.Right;
                    break;
                case "down":
                    dir = TransferDirection.Down;
                    break;
                default:
                    throw new InvalidProgramException($"direction '{direction}' not supported.");
            }

            _desktops.ActiveDesktop.HandleMoveFocus(dir);
        });

        public void MoveFocusWindow(string direction) => DoInit(() =>
        {
            switch (direction.ToLowerInvariant())
            {
                case "left":
                    _desktops.ActiveDesktop.HandleMoveNodeLeft();
                    break;
                case "up":
                    _desktops.ActiveDesktop.HandleMoveNodeUp();
                    break;
                case "right":
                    _desktops.ActiveDesktop.HandleMoveNodeRight();
                    break;
                case "down":
                    _desktops.ActiveDesktop.HandleMoveNodeDown();
                    break;
                default:
                    throw new InvalidProgramException($"direction '{direction}' not supported.");
            }
        });

        /// <summary>
        /// This method make sure we do actual init after desktop + screens have been added
        /// </summary>
        /// <param name="execute">something to run after init</param>
        private void DoInit(Action execute)
        {
            if (_haveInit)
            {
                execute?.Invoke();
                return;
            }

            _haveInit = true;
            _parser = _services.GetRequiredService<MessageParser>();
            _desktops = _services.GetRequiredService<IVirtualDesktopCollection>();
            _parser.PostInit();
            execute?.Invoke();
        }

        private void SendAndHandleMessage(long msg, long lParam, uint wParam)
        {
            _queue.Enqueue(new PipeMessageEx(new PipeMessage
            {
                msg = msg,
                lParam = lParam,
                wParam = wParam
            }, "Gherkin test"));

            _parser.HandleMessages();
        }

        private class TestWindowInfo
        {
            public uint Id { get; }
            public string Name { get; }

            public RECT InitializeRect { get; }
            public TestWindowInfo(string name, RECT initializeRect)
            {
                Name = name;
                var rnd = new Random();
                Id = (uint)rnd.Next();
                InitializeRect = initializeRect;
            }
        }

        delegate void HwndWithRectCallback(IntPtr hWnd, out RECT rect);
    }
}