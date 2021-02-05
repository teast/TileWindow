using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;
using TileWindow.Handlers;
using TileWindow.Nodes;

namespace TileWindow.Trackers
{
    /// <summary>
    /// This delegate exist only to get rid of circle reference between <see cref="WindowTracker" /> and <see cref="FocusHandler" />
    /// </summary>
    /// <param name="rect">RECT for the new <see cref="WindowNode" /></param>
    /// <param name="hWnd">Window handler for the new <see cref="WindowNode" /></param>
    /// <returns>a new <see cref="WindowNode" /> if success, else null</returns>
    public delegate WindowNode CreateWindowNode(RECT rect, IntPtr hWnd, Direction direction = Direction.Horizontal);

    /// <summary>
    /// Describes what to validate when checking an window handle
    /// </summary>
    public class ValidateHwndParams
    {
        /// <summary>
        /// If true then do some of the validations.
        /// </summary>
        /// <remarks>If false then no validation will be made regardless of what rest of the parameters say</remarks>
        public bool DoValidate { get; }

        /// <summary>
        /// Validate that window handler do not have WS_CHILD
        /// </summary>
        public bool ValidateChild { get; }

        /// <summary>
        /// Validate that window handler got WS_VISIBLE
        /// </summary>
        public bool ValidateVisible { get; }

        /// <summary>
        /// Validate that window handler do not have WM_EX_NOACTIVATE
        /// </summary>
        public bool ValidateNoActivate { get; }

        /// <summary>
        /// Validate window handlers that got class name "ApplicationFrameWindow" do have an child with class name "Windows.UI.Core.CoreWindow" (= visible)
        /// </summary>
        public bool ValidateApplicationFrame { get; }

        /// <summary>
        /// Validate that window handlers with class name "Windows.UI.Core.CoreWindow" do not have cloaked attribute: DWMWINDOWATTRIBUTE.Cloaked
        /// </summary>
        public bool ValidateDwm { get; }

        public ValidateHwndParams(bool doValidate = true, bool validateChild = true,
                                bool validatevisible = true, bool validateNoActivate = true,
                                bool validateApplicationFrame = true, bool validateDwm = true)
        {
            DoValidate = doValidate;
            ValidateChild = validateChild;
            ValidateVisible = validatevisible;
            ValidateNoActivate = validateNoActivate;
            ValidateApplicationFrame = validateApplicationFrame;
            ValidateDwm = validateDwm;
        }
    }

    public class IgnoreHwndInfo
    {
        public static readonly int Infinitive = -1;
        private readonly IntPtr _hWnd;
        private readonly int _max;
        private int _count;

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
            {
                return (false, false);
            }

            _count++;
            return (true, (_max != -1 && _count >= _max));
        }
    }

    public interface IWindowTracker
    {
        void removeWindow(IntPtr hWnd);
        void AddWindow(IntPtr hWnd, Node node);
        void IgnoreHwnd(IgnoreHwndInfo hwnd);
        Node GetNodes(IntPtr hWnd);
        bool Contains(IntPtr hWnd);
        bool Contains(Node nodes);
        bool CanHandleHwnd(IntPtr hWnd, ValidateHwndParams validation);
        WindowNode CreateNode(IntPtr hWnd, ValidateHwndParams validation = null);
        bool RevalidateHwnd(WindowNode node, IntPtr hWnd);
    }

    public class WindowTracker : IWindowTracker
    {
        private readonly Dictionary<IntPtr, Node> _windows = new Dictionary<IntPtr, Node>();
        private readonly List<IgnoreHwndInfo> _ignoreHwnds;
        private readonly IList<Regex> classNamesToIgnore;
        private readonly IList<Regex> captionsToIgnore;
        private readonly IPInvokeHandler pinvokeHandler;
        private readonly CreateWindowNode windowNodeCreater;
        private readonly IWindowEventHandler windowHandler;

        public WindowTracker(IPInvokeHandler pinvokeHandler, CreateWindowNode windowNodeCreater, IWindowEventHandler windowHandler)
        {
            this._ignoreHwnds = new List<IgnoreHwndInfo>();
            this.pinvokeHandler = pinvokeHandler;
            this.windowNodeCreater = windowNodeCreater;
            this.windowHandler = windowHandler;
            this.classNamesToIgnore = new List<Regex>
            {
                new Regex(@"^Shell_TrayWnd$", RegexOptions.Compiled),
                new Regex(@"^Progman$", RegexOptions.Compiled), // Program Manager === Desktop in win
                new Regex(@"^VirtualConsoleClassApp$", RegexOptions.Compiled), // ConEmu
                new Regex(@"^LockScreenBackstopFrame$", RegexOptions.Compiled), // Windows 10 Lock screen
                new Regex(@"^MultitaskingViewFrame$", RegexOptions.Compiled), // Windows 10 multitasking screen
                new Regex(@"^MSO_BORDEREFFECT_WINDOW_CLASS$", RegexOptions.Compiled),
                new Regex(@"GlowWindow", RegexOptions.Compiled), // Full: VisualStudioGlowWindow
            };

            this.captionsToIgnore = new List<Regex>
            {
                new Regex(@"^TileWindow - AppBar$", RegexOptions.Compiled)
            };
        }

        public void AddWindow(IntPtr hWnd, Node node)
        {
            if (_windows.ContainsKey(hWnd))
            {
                throw new Exception($"{nameof(WindowTracker)}.{nameof(AddWindow)} Trying to add hwnd {hWnd} that allready exist!");
            }

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
            for (int i = 0; i < _ignoreHwnds.Count; i++)
            {
                (bool ignoreIt, bool killIt) = _ignoreHwnds[i].ShouldIgnore(hWnd);
                if (ignoreIt)
                {
                    if (killIt)
                    {
                        _ignoreHwnds.RemoveAt(i);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Validates if an specific hWnd could probably be handled by WindowNode
        /// </summary>
        /// <param name="hWnd">window handler to validate</param>
        /// <param name="validation">Instruction on what to validate</param>
        /// <returns>true if it probably can</returns>
        public bool CanHandleHwnd(IntPtr hWnd, ValidateHwndParams validation)
        {
            if (validation.DoValidate == false)
            {
                return true;
            }

            if (hWnd == IntPtr.Zero)
            {
                return false;
            }

            if (ShouldIgnoreHwnd(hWnd))
            {
                return false;
            }

            var cb = new StringBuilder(1024);
            var style = pinvokeHandler.GetWindowLongPtr(hWnd, PInvoker.GWL_STYLE).ToInt64();
            var exstyle = pinvokeHandler.GetWindowLongPtr(hWnd, PInvoker.GWL_EXSTYLE).ToInt64();
            if (style == 0)
            {
                Log.Warning($"{nameof(WindowNode)}.{nameof(CanHandleHwnd)} Could not retrieve GWL_STYLE for hWnd {hWnd}, error: {pinvokeHandler.GetLastError()}");
                return false;
            }

            if (validation.ValidateChild && (style & PInvoker.WS_CHILD) == PInvoker.WS_CHILD)
            {
                return false;
            }

            if (validation.ValidateVisible && (style & PInvoker.WS_VISIBLE) != PInvoker.WS_VISIBLE)
            {
                return false;
            }

            if (validation.ValidateNoActivate && (exstyle & PInvoker.WS_EX_NOACTIVATE) == PInvoker.WS_EX_NOACTIVATE)
            {
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

            if (validation.ValidateApplicationFrame && className == "ApplicationFrameWindow")
            {
                if (IsSpecialAppVisible(hWnd) == false)
                {
                    Log.Warning($"{nameof(WindowNode)}.{nameof(CanHandleHwnd)} Going to ignore {hWnd} \"{GetWindowText(hWnd)}\" [{className}] because it is not visible according to window 10 special thingy");
                    return false;
                }
            }

            if (validation.ValidateDwm && className == "Windows.UI.Core.CoreWindow")
            {
                var result = 0;
                if ((result = pinvokeHandler.DwmGetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.Cloaked, out bool pvAttribute, sizeof(int))) != 0 || pvAttribute)
                {
                    return false;
                }
            }

            var text = GetWindowText(hWnd);
            if (captionsToIgnore.Any(regex => regex.IsMatch(text)))
            {
                return false;
            }

            return true;
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

        /// <summary>
        /// Creates an WindowNode based on <c ref="hWnd" />
        /// </summary>
        /// <param name="hWnd">window handler to base our windownode on</param>
        /// <param name="validation">instruction on what to validate</param>
        /// <returns>null if anything wrong or if window handler deems not possible</returns>
        public WindowNode CreateNode(IntPtr hWnd, ValidateHwndParams validation = null)
        {
            WindowNode node = null;
            if (CanHandleHwnd(hWnd, validation ?? new ValidateHwndParams()))
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
            {
                return false;
            }
            
            if (!pinvokeHandler.IsWindow(hWnd))
            {
                return false;
            }

            if (pinvokeHandler.GetClassName(hWnd, cb, cb.Capacity) == 0)
            {
                return false;
            }

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
            foreach (var ch in childData.Hwnd)
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