using System;
using System.Collections.Generic;
using System.Text;
using Serilog;
using TileWindow.Dto;

namespace TileWindow.Handlers
{
    public enum WindowStyleChangedType
    {
        Style,
        Minimize,
        Maximize,
        Restore
    }

    public class WindowStyleChangedArg
    {
        public WindowStyleChangedType StyleChangedType { get; private set; }
        public long Style { get; private set; }

        public WindowStyleChangedArg(WindowStyleChangedType type, long style)
        {
            StyleChangedType = type;
            Style = style;
        }
    }

    public interface IWindowEventHandler: IHandler
    {
        event EventHandler<Tuple<IntPtr, long>> UnknownWindow;
        Guid AddWindowStyleChangedListener(IntPtr hWnd, Action<WindowStyleChangedArg> handler);
        void RemoveWindowStyleChangedListener(Guid id);
        Guid AddWindowCloseListener(IntPtr hWnd, Action<IntPtr> handler);
        void RemoveWindowCloseListener(Guid id);
    }

    public class WindowEventHandler : IWindowEventHandler
    {
        private readonly ISignalHandler signalHandler;
        private readonly IPInvokeHandler pinvokeHandler;
        private Dictionary<Guid, Tuple<IntPtr, Action<IntPtr>>> _closeListeners;
        private Dictionary<Guid, Tuple<IntPtr, Action<WindowStyleChangedArg>>> _styleChangedListeners;

        public event EventHandler<Tuple<IntPtr, long>> UnknownWindow;

        public WindowEventHandler(ISignalHandler signalHandler, IPInvokeHandler pinvokeHandler)
        {
            this.signalHandler = signalHandler;
            this.pinvokeHandler = pinvokeHandler;
            this._closeListeners = new Dictionary<Guid, Tuple<IntPtr, Action<IntPtr>>>();
            this._styleChangedListeners = new Dictionary<Guid, Tuple<IntPtr, Action<WindowStyleChangedArg>>>();
        }

        public void Init()
        {
        }

        public void Quit()
        {
        }

        public void ReadConfig(AppConfig config)
        {
        }

        public Guid AddWindowCloseListener(IntPtr hWnd, Action<IntPtr> handler)
        {
            var guid = Guid.NewGuid();

            _closeListeners.Add(guid, Tuple.Create(hWnd, handler));
            return guid;
        }

        public void RemoveWindowCloseListener(Guid id)
        {
            _closeListeners.Remove(id);
        }

        public Guid AddWindowStyleChangedListener(IntPtr hWnd, Action<WindowStyleChangedArg> handler)
        {
            var guid = Guid.NewGuid();

            _styleChangedListeners.Add(guid, Tuple.Create(hWnd, handler));
            return guid;
        }

        public void RemoveWindowStyleChangedListener(Guid id)
        {
            _styleChangedListeners.Remove(id);
        }

        public void HandleMessage(PipeMessage msg)
        {
            if (msg.msg == signalHandler.WMC_SCMINIMIZE ||
                msg.msg == signalHandler.WMC_SCMAXIMIZE)
            {
                var type = msg.msg == signalHandler.WMC_SCMAXIMIZE ? WindowStyleChangedType.Maximize : WindowStyleChangedType.Minimize;
                var hwnd = new IntPtr((long)msg.wParam);
                foreach(var l in _styleChangedListeners)
                {
                    if (l.Value.Item1 == hwnd)
                    {
                        l.Value.Item2(new WindowStyleChangedArg(type, 0));
                    }
                }
            }
            else if (msg.msg == signalHandler.WMC_STYLECHANGED)
            {
                // NOTE, this messages lParam should be an pointer
                // to an STYLESTRUCT object... how to fix so we can
                // read this object in here???
                var hwnd = new IntPtr((long)msg.wParam);
                var hit = false;

//                var style1 = pinvokeHandler.GetWindowLongPtr(hwnd, PInvoker.GWL_STYLE).ToInt64();
//                var exstyle1 = pinvokeHandler.GetWindowLongPtr(hwnd, PInvoker.GWL_EXSTYLE).ToInt64();
//                pinvokeHandler.GetWindowRect(hwnd, out RECT r);
//Log.Information($"StyleChanged for {hwnd} (visible: {pinvokeHandler.IsWindowVisible(hwnd)}, window: {pinvokeHandler.IsWindow(hwnd)}, style: {style1}, exstyle: {exstyle1}, rect: {r})");
                foreach(var l in _styleChangedListeners)
                {
                    if (l.Value.Item1 == hwnd)
                    {
                        hit = true;
                        l.Value.Item2(new WindowStyleChangedArg(WindowStyleChangedType.Style, msg.lParam));
                    }
                }

                if (!hit)
                {
                    UnknownWindow?.Invoke(this, Tuple.Create(hwnd, msg.lParam));
                }
            }
            else if (msg.msg == signalHandler.WMC_DESTROY)
            {
                var hwnd = new IntPtr((long)msg.wParam);
                //Log.Information($"WindoeEvent.WMC_DESTROY hwnd: {msg.wParam} ({GetWindowText(hwnd)}) [{GetClassName(hwnd)}]");
                foreach(var l in _closeListeners)
                    if (l.Value.Item1 == hwnd)
                        l.Value.Item2(hwnd);
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

        public void DumpDebug()
        {
            
        }

        public void Dispose()
        {
        }
    }
}