using System;
using System.Collections.Generic;
using System.Linq;
using TileWindow.Dto;
using TileWindow.Trackers;

namespace TileWindow.Handlers
{
    public interface IFocusHandler: IHandler
    {
        Guid AddListener(IntPtr hwnd, Action<bool> callback);
        void RemoveListener(IntPtr hwnd, Guid id);
    }

    public class FocusHandler: IFocusHandler
    {
        private readonly ISignalHandler signal;
        private readonly IWindowTracker windowTracker;
        private Dictionary<IntPtr, List<Tuple<Guid, Action<bool>>>> _listeners;
        
        public FocusHandler(ISignalHandler signal, IWindowTracker windowTracker)
        {
            this.signal = signal;
            this.windowTracker = windowTracker;
            this._listeners = new Dictionary<IntPtr, List<Tuple<Guid, Action<bool>>>>();
        }

        public void ReadConfig(AppConfig config)
        {
        }

        public void Init()
        {
        }
        
        public void Quit()
        {
        }

        public Guid AddListener(IntPtr hwnd, Action<bool> callback)
        {
            var guid = Guid.NewGuid();
            if (!this._listeners.TryGetValue(hwnd, out List<Tuple<Guid, Action<bool>>> val))
            {
                val = new List<Tuple<Guid, Action<bool>>>();
                this._listeners.Add(hwnd, val);
            }

            val.Add(Tuple.Create(guid, callback));
            return guid;
        }

        public void RemoveListener(IntPtr hwnd, Guid id)
        {
            if (!this._listeners.TryGetValue(hwnd, out List<Tuple<Guid, Action<bool>>> val))
                return;
            
            val = val.Where(v => v.Item1 != id).ToList();
            this._listeners[hwnd] = val;
        }

        public void HandleMessage(PipeMessage msg)
        {
			if(msg.msg == signal.WMC_SETFOCUS)
            {
                if (msg.wParam > 0)
                {
                    var hwnd = new IntPtr((long)msg.wParam);
                    SignalAllListenersExcept(false, hwnd);
                    SignalListeners(hwnd, true);
                }

            }
            else if (msg.msg == signal.WMC_ACTIVATEAPP)
            {
                // If the current hwnd is the one receiving focus
                if (msg.lParam != 0)
                {
                    var hwnd = new IntPtr((long)msg.wParam);
                    var node = windowTracker.GetNodes(hwnd);

                    // And if we got a node handler for this window
                    if (node != null)
                    {
                        SignalAllListenersExcept(false, hwnd);
                        SignalListeners(hwnd, true);
                    }
                }
            }
        }

        private void SignalListeners(IntPtr hwnd, bool gotFocus)
        {
            if (!this._listeners.TryGetValue(hwnd, out List<Tuple<Guid, Action<bool>>> val))
                return;
            val.ForEach(v => v.Item2(gotFocus));
        }

        private void SignalAllListenersExcept(bool gotFocus, IntPtr hwnd)
        {
            foreach(var listeners in this._listeners)
                if (listeners.Key != hwnd)
                    foreach(var listener in listeners.Value)
                        listener.Item2(gotFocus);
        }

        public void Dispose()
        {
        }
    }
}