using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace TileWindow
{
    public delegate bool EnumWindowsProc(IntPtr hwnd, ref EnumExtraData data);
    public class EnumExtraData
    {
        public List<IntPtr> Hwnd { get; set; }

        public EnumExtraData()
        {
            Hwnd = new List<IntPtr>();
        }
    }

    public class HWnd
    {
        public IntPtr Hwnd { get; set; }

        public HWnd()
        {

        }

        public HWnd(IntPtr ptr)
        {
            Hwnd = ptr;
        }
    }

    public enum DWMWINDOWATTRIBUTE : uint
    {
        NCRenderingEnabled = 1,
        NCRenderingPolicy,
        TransitionsForceDisabled,
        AllowNCPaint,
        CaptionButtonBounds,
        NonClientRtlLayout,
        ForceIconicRepresentation,
        Flip3DPolicy,
        ExtendedFrameBounds,
        HasIconicBitmap,
        DisallowPeek,
        ExcludedFromPeek,
        Cloak,
        Cloaked,
        FreezeRepresentation
    }

    public enum ShowWindowCmd: int
    {
        SW_FORCEMINIMIZE = 11,
        SW_HIDE = 0,
        SW_MAXIMIZE = 3,
        SW_MINIMIZE = 6,
        SW_RESTORE = 9,
        SW_SHOW = 5,
        SW_SHOWDEFAULT = 10,
        SW_SHOWMAXIMIZED = 3,
        SW_SHOWMINIMIZED = 2,
        SW_SHOWMINNOACTIVE = 7,
        SW_SHOWNA = 8,
        SW_SHOWNOACTIVATE = 4,
        SW_SHOWNORMAL = 1,
    }

    public enum GetWindowType : uint
    {
        /// <summary>
        /// The retrieved handle identifies the window of the same type that is highest in the Z order.
        /// <para/>
        /// If the specified window is a topmost window, the handle identifies a topmost window.
        /// If the specified window is a top-level window, the handle identifies a top-level window.
        /// If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        GW_HWNDFIRST = 0,
        /// <summary>
        /// The retrieved handle identifies the window of the same type that is lowest in the Z order.
        /// <para />
        /// If the specified window is a topmost window, the handle identifies a topmost window.
        /// If the specified window is a top-level window, the handle identifies a top-level window.
        /// If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        GW_HWNDLAST = 1,
        /// <summary>
        /// The retrieved handle identifies the window below the specified window in the Z order.
        /// <para />
        /// If the specified window is a topmost window, the handle identifies a topmost window.
        /// If the specified window is a top-level window, the handle identifies a top-level window.
        /// If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        GW_HWNDNEXT = 2,
        /// <summary>
        /// The retrieved handle identifies the window above the specified window in the Z order.
        /// <para />
        /// If the specified window is a topmost window, the handle identifies a topmost window.
        /// If the specified window is a top-level window, the handle identifies a top-level window.
        /// If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        GW_HWNDPREV = 3,
        /// <summary>
        /// The retrieved handle identifies the specified window's owner window, if any.
        /// </summary>
        GW_OWNER = 4,
        /// <summary>
        /// The retrieved handle identifies the child window at the top of the Z order,
        /// if the specified window is a parent window; otherwise, the retrieved handle is NULL.
        /// The function examines only child windows of the specified window. It does not examine descendant windows.
        /// </summary>
        GW_CHILD = 5,
        /// <summary>
        /// The retrieved handle identifies the enabled popup window owned by the specified window (the
        /// search uses the first such window found using GW_HWNDNEXT); otherwise, if there are no enabled
        /// popup windows, the retrieved handle is that of the specified window.
        /// </summary>
        GW_ENABLEDPOPUP = 6
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT : IEquatable<RECT>
    {
        public int Left;        // x position of upper-left corner
        public int Top;         // y position of upper-left corner
        public int Right;       // x position of lower-right corner
        public int Bottom;      // y position of lower-right corner

        public RECT(int left = 0, int top = 0, int right = 0, int bottom = 0)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public override string ToString() => $"{{Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom} (Width/Height: {Right - Left}/{Bottom - Top}}}";

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                hash = (hash * 16777619) ^ Left.GetHashCode();
                hash = (hash * 16777619) ^ Top.GetHashCode();
                hash = (hash * 16777619) ^ Right.GetHashCode();
                hash = (hash * 16777619) ^ Bottom.GetHashCode();
                return hash;
            }
        }

        public bool Equals([AllowNull] RECT other)
        {
            if (!EqualityComparer<int>.Default.Equals(Left, other.Left) ||
                !EqualityComparer<int>.Default.Equals(Top, other.Top) ||
                !EqualityComparer<int>.Default.Equals(Right, other.Right) ||
                !EqualityComparer<int>.Default.Equals(Bottom, other.Bottom))
            {
                return false;
            }

            return true;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct STYLESTRUCT
    {
        public int styleOld;
        public int styleNew;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public POINT(System.Drawing.Point pt) : this(pt.X, pt.Y) { }

        public static implicit operator System.Drawing.Point(POINT p)
        {
            return new System.Drawing.Point(p.X, p.Y);
        }

        public static implicit operator POINT(System.Drawing.Point p)
        {
            return new POINT(p.X, p.Y);
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        /// <summary>
        /// The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WINDOWPLACEMENT).
        /// <para>
        /// GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
        /// </para>
        /// </summary>
        public int Length;

        /// <summary>
        /// Specifies flags that control the position of the minimized window and the method by which the window is restored.
        /// </summary>
        public int Flags;

        /// <summary>
        /// The current show state of the window.
        /// </summary>
        public ShowWindowCommands ShowCmd;

        /// <summary>
        /// The coordinates of the window's upper-left corner when the window is minimized.
        /// </summary>
        public POINT MinPosition;

        /// <summary>
        /// The coordinates of the window's upper-left corner when the window is maximized.
        /// </summary>
        public POINT MaxPosition;

        /// <summary>
        /// The window's coordinates when the window is in the restored position.
        /// </summary>
        public RECT NormalPosition;

        /// <summary>
        /// Gets the default (empty) value.
        /// </summary>
        public static WINDOWPLACEMENT Default
        {
            get
            {
                WINDOWPLACEMENT result = new WINDOWPLACEMENT();
                result.Length = Marshal.SizeOf( result );
                return result;
            }
        }    
    }

    [Flags]
    public enum ShowWindowCommands : uint
    {
        WPF_SETMINPOSITION = 0x1,
        WPF_RESTORETOMAXIMIZED = 0x2,
        WPF_ASYNCWINDOWPLACEMENT = 0x4
    }

    [Flags()]
    public enum SetWindowPosFlags : uint
    {
        /// <summary>If the calling thread and the thread that owns the window are attached to different input queues, 
        /// the system posts the request to the thread that owns the window. This prevents the calling thread from 
        /// blocking its execution while other threads process the request.</summary>
        SWP_ASYNCWINDOWPOS = 0x4000,
        /// <summary>Prevents generation of the WM_SYNCPAINT message.</summary>
        /// <remarks>SWP_DEFERERASE</remarks>
        SWP_DEFERERASE = 0x2000,
        /// <summary>Draws a frame (defined in the window's class description) around the window.</summary>
        /// <remarks>SWP_DRAWFRAME</remarks>
        SWP_DRAWFRAME = 0x0020,
        /// <summary>Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to 
        /// the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE 
        /// is sent only when the window's size is being changed.</summary>
        /// <remarks>SWP_FRAMECHANGED</remarks>
        SWP_FRAMECHANGED = 0x0020,
        /// <summary>Hides the window.</summary>
        /// <remarks>SWP_HIDEWINDOW</remarks>
        SWP_HIDEWINDOW = 0x0080,
        /// <summary>Does not activate the window. If this flag is not set, the window is activated and moved to the 
        /// top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter 
        /// parameter).</summary>
        /// <remarks>SWP_NOACTIVATE</remarks>
        SWP_NOACTIVATE = 0x0010,
        /// <summary>Discards the entire contents of the client area. If this flag is not specified, the valid 
        /// contents of the client area are saved and copied back into the client area after the window is sized or 
        /// repositioned.</summary>
        /// <remarks>SWP_NOCOPYBITS</remarks>
        SWP_NOCOPYBITS = 0x0100,
        /// <summary>Retains the current position (ignores X and Y parameters).</summary>
        /// <remarks>SWP_NOMOVE</remarks>
        SWP_NOMOVE = 0x0002,
        /// <summary>Does not change the owner window's position in the Z order.</summary>
        /// <remarks>SWP_NOOWNERZORDER</remarks>
        SWP_NOOWNERZORDER = 0x0200,
        /// <summary>Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to 
        /// the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent 
        /// window uncovered as a result of the window being moved. When this flag is set, the application must 
        /// explicitly invalidate or redraw any parts of the window and parent window that need redrawing.</summary>
        /// <remarks>SWP_NOREDRAW</remarks>
        SWP_NOREDRAW = 0x0008,
        /// <summary>Same as the SWP_NOOWNERZORDER flag.</summary>
        /// <remarks>SWP_NOREPOSITION</remarks>
        SWP_NOREPOSITION = 0x0200,
        /// <summary>Prevents the window from receiving the WM_WINDOWPOSCHANGING message.</summary>
        /// <remarks>SWP_NOSENDCHANGING</remarks>
        SWP_NOSENDCHANGING = 0x0400,
        /// <summary>Retains the current size (ignores the cx and cy parameters).</summary>
        /// <remarks>SWP_NOSIZE</remarks>
        SWP_NOSIZE = 0x0001,
        /// <summary>Retains the current Z order (ignores the hWndInsertAfter parameter).</summary>
        /// <remarks>SWP_NOZORDER</remarks>
        SWP_NOZORDER = 0x0004,
        /// <summary>Displays the window.</summary>
        /// <remarks>SWP_SHOWWINDOW</remarks>
        SWP_SHOWWINDOW = 0x0040
    }

    public interface IPInvokeHandler
    {
        uint RegisterWindowMessage(string lpString);
        IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
        bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);
        bool PostThreadMessage(uint threadId, uint msg, UIntPtr wParam, IntPtr lParam);
        IntPtr WindowFromPoint(POINT Point);
        IntPtr GetActiveWindow();
        IntPtr SetFocus(IntPtr hWnd);
        bool SetForegroundWindow(IntPtr hWnd);
        IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        bool EnumWindows(EnumWindowsProc lpEnumFunc, ref EnumExtraData data);
        uint GetLastError();
        int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);
        bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, ref EnumExtraData data);
        IntPtr SetWindowLongPtr(HandleRef hWnd, int nIndex, IntPtr dwNewLong);

        int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out bool pvAttribute, int cbAttribute);
        bool IsWindowVisible(IntPtr hWnd);
        bool ShowWindow(IntPtr hWnd, ShowWindowCmd nCmdShow);
        IntPtr FindWindow (string lpClassName, string lpWindowName);
        void SwitchToThisWindow (IntPtr hWnd, bool fUnknown);
        IntPtr GetForegroundWindow();
        uint GetCurrentThreadId();
        uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        // When you don't want the ProcessId, use this overload and pass IntPtr.Zero for the second parameter
        uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
        bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        IntPtr SetActiveWindow(IntPtr hWnd);
        bool AllocConsole();
        IntPtr GetConsoleWindow();
        bool FreeConsole();
        bool IsWindow(IntPtr hWnd);
        bool WTSRegisterSessionNotification(IntPtr hWnd, int dwFlags);
        bool WTSUnRegisterSessionNotification(IntPtr hWnd);
        Int32 OpenDesktop(string lpszDesktop, Int32 dwFlags, bool fInherit, Int32 dwDesiredAccess);
        Int32 CloseDesktop(Int32 hDesktop);
        Int32 SwitchDesktop(Int32 hDesktop);
        IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);
        bool GetUserObjectInformation(IntPtr hObj, int nIndex, [Out] byte[] pvInfo, uint nLength, out uint lpnLengthNeeded);
    }

    /// <summary>
    /// Most of the stuff in here are taken from pinvoke.net
    /// </summary>
    public class PInvokeHandler: IPInvokeHandler
    {
        public uint RegisterWindowMessage(string lpString) { return PInvoker.RegisterWindowMessage(lpString); }
        public bool GetWindowRect(IntPtr hwnd, out RECT lpRect) { return PInvoker.GetWindowRect(hwnd, out lpRect); }
        public bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags) { return PInvoker.SetWindowPos(hWnd, hWndInsertAfter, X, Y, cx, cy, uFlags); }
        public bool PostThreadMessage(uint threadId, uint msg, UIntPtr wParam, IntPtr lParam) { return PInvoker.PostThreadMessage(threadId, msg, wParam, lParam); }
        public IntPtr WindowFromPoint(POINT Point) { return TileWindow.PInvoker.WindowFromPoint(Point); }
        public IntPtr GetActiveWindow() { return PInvoker.GetActiveWindow(); }
        public IntPtr SetFocus(IntPtr hWnd) { return PInvoker.SetFocus(hWnd); }
        public bool SetForegroundWindow(IntPtr hWnd) { return PInvoker.SetForegroundWindow(hWnd); }
        public IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam) { return PInvoker.SendMessage(hWnd, Msg, wParam, lParam); }
        public bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam) { return PInvoker.PostMessage(hWnd, Msg, wParam, lParam); }
        public bool EnumWindows(EnumWindowsProc lpEnumFunc, ref EnumExtraData data) { return PInvoker.EnumWindows(lpEnumFunc, ref data); }
        public uint GetLastError() { return PInvoker.GetLastError(); }
        public int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount) { return PInvoker.GetClassName(hWnd, lpClassName, nMaxCount); }
        public int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount) { return PInvoker.GetWindowText(hWnd, lpString, nMaxCount); }
        public IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd) { return PInvoker.GetWindow(hWnd, uCmd); }
        public bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, ref EnumExtraData data) { return PInvoker.EnumChildWindows(hwndParent, lpEnumFunc, ref data); }

        public int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out bool pvAttribute, int cbAttribute) { return PInvoker.DwmGetWindowAttribute(hwnd, dwAttribute, out pvAttribute, cbAttribute); }

        public bool IsWindowVisible(IntPtr hWnd) { return PInvoker.IsWindowVisible(hWnd); }

        public bool ShowWindow(IntPtr hWnd, ShowWindowCmd nCmdShow) { return PInvoker.ShowWindow(hWnd, nCmdShow); }

        public IntPtr FindWindow (string lpClassName, string lpWindowName) { return PInvoker.FindWindow(lpClassName, lpWindowName); }

        public void SwitchToThisWindow (IntPtr hWnd, bool fUnknown) { PInvoker.SwitchToThisWindow(hWnd, fUnknown); }

        public IntPtr GetForegroundWindow() { return PInvoker.GetForegroundWindow(); }

        public uint GetCurrentThreadId() { return PInvoker.GetCurrentThreadId(); }

        public uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId) { return PInvoker.GetWindowThreadProcessId(hWnd, out lpdwProcessId); }

        // When you don't want the ProcessId, use this overload and pass IntPtr.Zero for the second parameter
        public uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId) { return PInvoker.GetWindowThreadProcessId(hWnd, ProcessId); }

        public bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach) { return PInvoker.AttachThreadInput(idAttach, idAttachTo, fAttach); }
        
        public IntPtr SetActiveWindow(IntPtr hWnd) { return PInvoker.SetActiveWindow(hWnd); }

        public bool AllocConsole() { return PInvoker.AllocConsole(); }
        public IntPtr GetConsoleWindow() { return PInvoker.GetConsoleWindow(); }
        public bool FreeConsole() { return PInvoker.FreeConsole(); }

        public bool IsWindow(IntPtr hWnd) { return PInvoker.IsWindow(hWnd); }

        public bool WTSRegisterSessionNotification(IntPtr hWnd, int dwFlags) => PInvoker.WTSRegisterSessionNotification(hWnd, dwFlags);
        public bool WTSUnRegisterSessionNotification(IntPtr hWnd) => PInvoker.WTSUnRegisterSessionNotification(hWnd);

        public Int32 OpenDesktop(string lpszDesktop, Int32 dwFlags, bool fInherit, Int32 dwDesiredAccess) => PInvoker.OpenDesktop(lpszDesktop, dwFlags, fInherit, dwDesiredAccess);
        public Int32 CloseDesktop(Int32 hDesktop) => PInvoker.CloseDesktop(hDesktop);
        public Int32 SwitchDesktop(Int32 hDesktop) => PInvoker.SwitchDesktop(hDesktop);
        public IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess) => PInvoker.OpenInputDesktop(dwFlags, fInherit, dwDesiredAccess);
        public bool GetUserObjectInformation(IntPtr hObj, int nIndex, [Out] byte[] pvInfo, uint nLength, out uint lpnLengthNeeded) => PInvoker.GetUserObjectInformation(hObj, nIndex, pvInfo, nLength, out lpnLengthNeeded);

        // This helper static method is required because the 32-bit version of user32.dll does not contain this API
        // (on any versions of Windows), so linking the method will fail at run-time. The bridge dispatches the request
        // to the correct function (GetWindowLong in 32-bit mode and GetWindowLongPtr in 64-bit mode)
        public IntPtr SetWindowLongPtr(HandleRef hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return PInvoker.SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(PInvoker.SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        public IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return PInvoker.GetWindowLongPtr64(hWnd, nIndex);
            else
                return PInvoker.GetWindowLongPtr(hWnd, nIndex);

        }

        private static class PInvoker
        {
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern uint RegisterWindowMessage(string lpString);

            [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
            public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
            public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool PostThreadMessage(uint threadId, uint msg, UIntPtr wParam, IntPtr lParam);


            [DllImport("user32.dll")]
            public static extern IntPtr GetActiveWindow();

            [DllImport("user32.dll")]
            public static extern IntPtr SetFocus(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("user32.dll")]
            public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, ref EnumExtraData data);

            [DllImport("kernel32.dll")]
            public static extern uint GetLastError();

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, ref EnumExtraData data);

            [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
            public static extern int SetWindowLong32(HandleRef hWnd, int nIndex, int dwNewLong);

            [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
            public static extern IntPtr SetWindowLongPtr64(HandleRef hWnd, int nIndex, IntPtr dwNewLong);

            [DllImport("dwmapi.dll", PreserveSig = true)]
            public static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out bool pvAttribute, int cbAttribute);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

            [DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCmd nCmdShow);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindow (string lpClassName, string lpWindowName);

            [DllImport("user32.dll")]
            public static extern void SwitchToThisWindow (IntPtr hWnd, bool fUnknown);

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("kernel32.dll")]
            public static extern uint GetCurrentThreadId();

            [DllImport("user32.dll", SetLastError=true)]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            // When you don't want the ProcessId, use this overload and pass IntPtr.Zero for the second parameter
            [DllImport("user32.dll")]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

            [DllImport("user32.dll")]
            public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

            [DllImport("user32.dll", SetLastError=true)]
            public static extern IntPtr SetActiveWindow(IntPtr hWnd);

            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AllocConsole();

            [DllImport("kernel32.dll")]
            public static extern IntPtr GetConsoleWindow();

            [DllImport("kernel32.dll", SetLastError=true, ExactSpelling=true)]
            public static extern bool FreeConsole();

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWindow(IntPtr hWnd);

            [DllImport("WtsApi32.dll")]
            public static extern bool WTSRegisterSessionNotification(IntPtr hWnd, [MarshalAs(UnmanagedType.U4)]int dwFlags);

            [DllImport("WtsApi32.dll")]
            public static extern bool WTSUnRegisterSessionNotification(IntPtr hWnd);

            [DllImport("user32", EntryPoint = "OpenDesktopA", CharSet = CharSet.Ansi,SetLastError = true, ExactSpelling = true)]
            public static extern Int32 OpenDesktop(string lpszDesktop, Int32 dwFlags, bool fInherit, Int32 dwDesiredAccess);

            [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
            public static extern Int32 CloseDesktop(Int32 hDesktop);

            [DllImport("user32", CharSet = CharSet.Ansi,SetLastError = true,ExactSpelling = true)]
            public static extern Int32 SwitchDesktop(Int32 hDesktop);
            [DllImport("user32.dll", SetLastError=true)]
            public static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);
            [DllImport("user32.dll", EntryPoint = "GetUserObjectInformationA", SetLastError=true)]
            public static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex, [Out] byte[] pvInfo, uint nLength, out uint lpnLengthNeeded);
        }
    }

    public static class PInvoker
    {
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT Point);

        public const int GWL_HWNDPARENT = (-8);
        public const int GWL_STYLE = (-16);
        public const int GWL_EXSTYLE = (-20);
        public const int WS_CAPTION = 0x00c00000;
        public const int WS_VISIBLE = 0x10000000;
        public const int WS_MAXIMIZE = 0x01000000;
        public const int WS_MINIMIZE = 0x20000000;
        public const int WS_SIZEBOX = 0x00040000;
        public const int WS_CHILD = 0x40000000;
        public const int WS_THICKFRAME =0x00040000;
        public const int WS_MINIMIZEBOX = 0x00020000;
        public const int WS_MAXIMIZEBOX = 0x00010000;
        public const int WS_SYSMENU = 0x00080000;
        public const uint WS_POPUP = 0x80000000;
        public const int WS_EX_APPWINDOW = 0x00040000;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const uint WM_CLOSE = 16;
        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly uint WM_LBUTTONDOWN = 0x0201;
        public static readonly uint WM_LBUTTONUP = 0x0202;
        public static readonly uint WM_DISPLAYCHANGE = 0x007e;
        public static readonly uint WM_SETTINGCHANGE = 0x001A;
        public static readonly uint SPI_SETWORKAREA = 0x002F;
        
        public static readonly uint MK_LBUTTON = 0x1;

        public const int NOTIFY_FOR_THIS_SESSION = 0;
        public const int WM_WTSSESSION_CHANGE = 0x2b1;
        public const int WTS_SESSION_LOCK = 0x7;
        public const int WTS_SESSION_UNLOCK = 0x8;
    }
}
