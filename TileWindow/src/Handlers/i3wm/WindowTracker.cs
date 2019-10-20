using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;
using TileWindow.Handlers.I3wm.Nodes;

namespace TileWindow.Handlers.I3wm
{
    /// <summary>
    /// This delegate exist only to get rid of circle reference between <see cref="WindowTracker" /> and <see cref="FocusHandler" />
    /// </summary>
    /// <param name="rect">RECT for the new <see cref="WindowNode" /></param>
    /// <param name="hWnd">Window handler for the new <see cref="WindowNode" /></param>
    /// <returns>a new <see cref="WindowNode" /> if success, else null</returns>
    public delegate WindowNode CreateWindowNode(RECT rect, IntPtr hWnd, Direction direction = Direction.Horizontal);

    public class IgnoreHwndInfo
    {
        public static readonly int Infinitive = -1;
        private IntPtr _hWnd;
        private int _count;
        private int _max;

        public IntPtr Hwnd => _hWnd;

        public IgnoreHwndInfo(IntPtr hWnd, int count = 1)
        {
            _hWnd = hWnd;
            _count = 0;
            _max = count;
        }
        public (bool ignoreIt, bool killMe) ShouldIgnore(IntPtr hWnd)
        {
            if (hWnd != _hWnd)
                return (false, false);
            
            _count++;
            return (true, (_max != -1 && _count >= _max));
        }
    }

    public interface IWindowTracker
    {
        bool IgnoreVisualFlag { get; set; }
        void removeWindow(IntPtr hWnd);
        void AddWindow(IntPtr hWnd, Node node);
        void IgnoreHwnd(IgnoreHwndInfo hwnd);
        Node GetNodes(IntPtr hWnd);
        bool Contains(IntPtr hWnd);
        bool Contains(Node nodes);
        bool CanHandleHwnd(IntPtr hWnd);
        WindowNode CreateNode(IntPtr hWnd, bool doValidate = true);
        bool RevalidateHwnd(WindowNode node, IntPtr hWnd);
    }

    public class WindowTracker : IWindowTracker
    {
        private Dictionary<IntPtr, Node> _windows = new Dictionary<IntPtr, Node>();
        private List<IgnoreHwndInfo> _ignoreHwnds;
        private readonly IList<Regex> classNamesToIgnore;
        private readonly IPInvokeHandler pinvokeHandler;
        private readonly CreateWindowNode windowNodeCreater;
        private readonly IWindowEventHandler windowHandler;

        public bool IgnoreVisualFlag { get; set; }

        public WindowTracker(IPInvokeHandler pinvokeHandler, CreateWindowNode windowNodeCreater, IWindowEventHandler windowHandler)
        {
            this._ignoreHwnds = new List<IgnoreHwndInfo>();
            this.IgnoreVisualFlag = false;
            this.pinvokeHandler = pinvokeHandler;
            this.windowNodeCreater = windowNodeCreater;
            this.windowHandler = windowHandler;
            this.classNamesToIgnore = new List<Regex>
            {
                new Regex(@"^WindowsForms10\.Window\.8\.app\..+_ad1$", RegexOptions.Compiled), // Desktop (I think): WindowsForms10.Window.8.app.*_ad1
                new Regex(@"^Shell_TrayWnd$", RegexOptions.Compiled),
                new Regex(@"^Progman$", RegexOptions.Compiled), // Program Manager === Desktop in win
                new Regex(@"^VirtualConsoleClassApp$", RegexOptions.Compiled), // ConEmu
                new Regex(@"^LockScreenBackstopFrame$", RegexOptions.Compiled), // Windows 10 Lock screen
                new Regex(@"^MultitaskingViewFrame$", RegexOptions.Compiled), // Windows 10 multitasking screen
                new Regex(@"^MSO_BORDEREFFECT_WINDOW_CLASS$", RegexOptions.Compiled),
                new Regex(@"GlowWindow", RegexOptions.Compiled), // Full: VisualStudioGlowWindow
            };
        }

        public void AddWindow(IntPtr hWnd, Node node)
        {
            if (_windows.ContainsKey(hWnd))
                throw new Exception($"{nameof(WindowTracker)}.{nameof(AddWindow)} Trying to add hwnd {hWnd} that allready exist!");

            _windows.Add(hWnd, node);
        }

        public void IgnoreHwnd(IgnoreHwndInfo hwnd)
        {
            _ignoreHwnds.Add(hwnd);
        }

        public bool Contains(IntPtr hWnd) => _windows.ContainsKey(hWnd);

        public bool Contains(Node node) => _windows.ContainsValue(node);

        public Node GetNodes(IntPtr hWnd) => _windows.TryGetValue(hWnd, out Node v) ? v : null;

        public void removeWindow(IntPtr hWnd) => _windows.Remove(hWnd);

        private bool ShouldIgnoreHwnd(IntPtr hWnd)
        {
            for(int i = 0; i < _ignoreHwnds.Count; i++)
            {
                (bool ignoreIt, bool killIt) =_ignoreHwnds[i].ShouldIgnore(hWnd);
                if (ignoreIt)
                {
                    if (killIt)
                        _ignoreHwnds.RemoveAt(i);

                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Validates if an specific hWnd could probably be handled by WindowNode
        /// </summary>
        /// <param name="hWnd">window handler to validate</param>
        /// <returns>true if it probably can</returns>
        public bool CanHandleHwnd(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            if (ShouldIgnoreHwnd(hWnd))
                return false;

            var cb = new StringBuilder(1024);
            var style = pinvokeHandler.GetWindowLongPtr(hWnd, PInvoker.GWL_STYLE).ToInt64();
            var exstyle = pinvokeHandler.GetWindowLongPtr(hWnd, PInvoker.GWL_EXSTYLE).ToInt64();
            if (style == 0)
            {
                Log.Warning($"{nameof(WindowNode)}.{nameof(CanHandleHwnd)} Could not retrieve GWL_STYLE for hWnd {hWnd}, error: {pinvokeHandler.GetLastError()}");
                return false;
            }

            if ((style & PInvoker.WS_CHILD) == PInvoker.WS_CHILD)
            {
                //Log.Information($"{nameof(WindowNode)}.{nameof(CanHandleHwnd)} Going to ignore {hWnd} because it is a child window");
                return false;
            }

            if ((style & PInvoker.WS_VISIBLE) != PInvoker.WS_VISIBLE)
            {
                //Log.Information($"{nameof(WindowNode)}.{nameof(CanHandleHwnd)} Going to ignore {hWnd} because it is not visible");
                return false;
            }

            if ((exstyle & PInvoker.WS_EX_NOACTIVATE) == PInvoker.WS_EX_NOACTIVATE)
            {
                //Log.Information($"{nameof(WindowNode)}.{nameof(CanHandleHwnd)} Going to ignore {hWnd} because it got WS_EX_NOACTIVATE");
                return false;
            }

            if (pinvokeHandler.GetClassName(hWnd, cb, cb.Capacity) == 0)
            {
                Log.Warning($"{nameof(WindowNode)}.{nameof(CanHandleHwnd)} Could not retrieve class name for hWnd {hWnd}, error: {pinvokeHandler.GetLastError()}");
                return false;
            }

            var className = cb.ToString();
            if (classNamesToIgnore.Any(regex => regex.IsMatch(className)))
            {
                return false;
            }

            if (className == "ApplicationFrameWindow" && IgnoreVisualFlag == false)
            {
                if (IsSpecialAppVisible(hWnd) == false)
                {
                    //var visible = pinvokeHandler.IsWindowVisible(hWnd);
                    //Log.Warning($"{nameof(WindowNode)}.{nameof(CanHandleHwnd)} Going to ignore {hWnd} \"{GetWindowText(hWnd)}\" [{className}] because it is not visible according to window 10 special thingy, visible: {visible}");
                    return false;
                }
            }

            if (className == "Windows.UI.Core.CoreWindow")
            {
                var result = 0;
                if ((result = pinvokeHandler.DwmGetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.Cloaked, out bool pvAttribute, sizeof(int))) != 0 || pvAttribute)
                {
                    //var visible = pinvokeHandler.IsWindowVisible(hWnd);
                    //Log.Warning($"{nameof(WindowNode)}.{nameof(CanHandleHwnd)} Going to ignore {hWnd} \"{GetWindowText(hWnd)}\" because it is not visible according to cloaked (result: {result:X}, cloaked: {pvAttribute}), visible: {visible}");
                    return false;
                }
            }

            return true;
        }

        private string GetWindowText(IntPtr Hwnd)
        {
            var tb = new StringBuilder(1024);
            if (pinvokeHandler.GetWindowText(Hwnd, tb, tb.Capacity) > 0)
                return tb.ToString();
            else
                return Hwnd.ToString();
        }

        /// <summary>
        /// Creates an WindowNode based on <c ref="hWnd" />
        /// </summary>
        /// <param name="hWnd">window handler to base our windownode on</param>
        /// <param name="doValidate">If true will call <c ref="CanHandleHwnd" /> before tryingt o create the node</param>
        /// <returns>null if anything wrong or if window handler deems not possible</returns>
        public WindowNode CreateNode(IntPtr hWnd, bool doValidate = true)
        {
            WindowNode node = null;
            if (doValidate == false || CanHandleHwnd(hWnd))
            {
                if (!pinvokeHandler.GetWindowRect(hWnd, out RECT rect))
                {
                    Log.Warning($"{nameof(WindowNode)}.{nameof(CreateNode)} Could not retrieve rect for hWnd {hWnd}, error: {pinvokeHandler.GetLastError()}");
                    return null;
                }

                node = windowNodeCreater?.Invoke(rect, hWnd);
            }

            return node;
        }

        public bool RevalidateHwnd(WindowNode node, IntPtr hWnd)
        {
            var cb = new StringBuilder(1024);

            if (node == null)
                return false;

            if (!pinvokeHandler.IsWindow(hWnd))
                return false;

            if (pinvokeHandler.GetClassName(hWnd, cb, cb.Capacity) == 0)
                return false;

            return (cb.ToString() == node.ClassName);
        }

 
        /// <summary>
        /// Helper method to enumerate all process
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static bool EnumProc(IntPtr hWnd, ref EnumExtraData data)
        {
            data.Hwnd.Add(new IntPtr(hWnd.ToInt64()));
            return true;
        }

        /// <summary>
        /// Windows 10 programs now uses class names to hide themself (wut!?)
        /// </summary>
        /// <param name="hWnd">window handler to check if it is visible or not</param>
        /// <returns>true if the app is visible, else false</returns>
        private bool IsSpecialAppVisible(IntPtr hWnd)
        {
            var childData = new EnumExtraData();
            pinvokeHandler.EnumChildWindows(hWnd, new EnumWindowsProc(EnumProc), ref childData);
            var hit = false;
            foreach(var ch in childData.Hwnd)
            {
                var ccb = new StringBuilder(1024);
                pinvokeHandler.GetClassName(ch, ccb, ccb.Capacity);
                if (ccb.ToString() == "Windows.UI.Core.CoreWindow")
                {
                    hit = true;
                    break;
                }
            }

            return hit;
        }
   }
}