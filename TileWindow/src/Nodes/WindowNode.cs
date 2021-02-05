using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using TileWindow.Handlers;
using TileWindow.Trackers;
using static TileWindow.PInvoker;

namespace TileWindow.Nodes
{
    public class WindowNode : Node, IEquatable<WindowNode>
    {
        private RECT _fullscreenRect;

        private int _width;
        private int _height;
        private long _exStyleBeforeHide;
        private bool _quit;
        private readonly long _origStyle;
        private readonly long _origExStyle;
        private readonly RECT _origRect;
        private readonly IDragHandler dragHandler;
        private readonly IFocusHandler focusHandler;
        private readonly ISignalHandler signalHandler;
        private readonly IWindowTracker windowTracker;
        private readonly IPInvokeHandler pinvokeHandler;
        private readonly IWindowEventHandler windowHandler;
        private Guid _focusListener = Guid.Empty;
        private Guid _closeListener = Guid.Empty;
        private Guid _styleChangedListener = Guid.Empty;
        public string ClassName { get; }
        private readonly long _parentHwnd;
        private bool _insideDragAction;
        public IntPtr Hwnd { get; private set; }

        public override NodeTypes WhatType => NodeTypes.Leaf;

        public override NodeStyle Style
        {
            get => base.Style;
            set
            {
                var old = base.Style;
                base.Style = value;

                if (Hwnd != IntPtr.Zero)
                {
                    if (value == NodeStyle.Floating || value == NodeStyle.FullscreenOne)
                    {
                        pinvokeHandler.SetWindowPos(Hwnd, HWND_TOPMOST, 0, 0, 0, 0, SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);
                    }
                    else if (value == NodeStyle.Tile)
                    {
                        pinvokeHandler.SetWindowPos(Hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);
                    }
                }
            }
        }

        public WindowNode(IDragHandler dragHandler, IFocusHandler focusHandler, ISignalHandler signalHandler, IWindowEventHandler windowHandler, IWindowTracker windowTracker, IPInvokeHandler pinvokeHandler, RECT rect, IntPtr hwnd, Direction direction = Direction.Horizontal, Node parent = null) : base(rect, direction, parent)
        {
            //Log.Information($"{nameof(WindowNode)}.ctor, Depth: {Depth}, hwnd: {hwnd} parent: {Parent?.GetType().ToString()} ({Parent?.WhatType.ToString()})");
            _quit = false;
            _width = rect.Right - rect.Left;
            _height = rect.Bottom - rect.Top;
            _insideDragAction = false;
            this.dragHandler = dragHandler;
            this.focusHandler = focusHandler;
            this.signalHandler = signalHandler;
            this.windowTracker = windowTracker;
            this.pinvokeHandler = pinvokeHandler;
            this.windowHandler = windowHandler;
            Hwnd = hwnd;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            _origStyle = pinvokeHandler.GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();
            _origExStyle = pinvokeHandler.GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            this.Name = GetWindowText();
            ClassName = GetClassName();
            _parentHwnd = pinvokeHandler.GetWindow(hwnd, GetWindowType.GW_OWNER).ToInt64();

            pinvokeHandler.GetWindowRect(hwnd, out _origRect);

            _focusListener = focusHandler.AddListener(Hwnd, isFocus =>
            {
                if (isFocus)
                {
                    OnWantFocus(this, new WantFocusEventArg(this));
                }
            });

            _closeListener = this.windowHandler.AddWindowCloseListener(Hwnd, _ =>
            {
                Parent?.RemoveChild(this);
            });

            _styleChangedListener = this.windowHandler.AddWindowStyleChangedListener(Hwnd, arg =>
            {
                if (Hwnd == IntPtr.Zero)
                {
                    Log.Warning($"Got WindowStyleChanged event (type: {arg.StyleChangedType.ToString()}) but Hwnd is zero...");
                    return;
                }

                var updateRect = false;
                switch (arg.StyleChangedType)
                {
                    case WindowStyleChangedType.Minimize:
                        {
                            if (_quit)
                            {
                                this.Parent?.RemoveChild(this);
                                return;
                            }

                            updateRect = true;
                        }
                        break;
                    case WindowStyleChangedType.Maximize:
                        updateRect = true;
                        break;
                    case WindowStyleChangedType.Style:
                        {
                            var style = pinvokeHandler.GetWindowLongPtr(Hwnd, GWL_STYLE);
                            var exstyle = pinvokeHandler.GetWindowLongPtr(Hwnd, GWL_EXSTYLE);
                            //Log.Information($"WindowNode [{Name}] {Hwnd} got StyleChanged event {arg.Style} (style: {style}, exstyle: {exstyle}");
                        }
                        break;
                }

                if (updateRect)
                {
                    this.SetHwndPos(this.Rect, out _);
                }
            });

            if (Hwnd != IntPtr.Zero)
            {
                dragHandler.OnDragStart += HandleOnDragStart;
                dragHandler.OnDragMove += HandleOnDragMove;
                dragHandler.OnDragEnd += HandleOnDragEnd;
            }

            SetupWindowNode(_origStyle, _origExStyle, ClassName);
            windowTracker.AddWindow(hwnd, this);
            Log.Information($"WindowNode.ctor done: {this} ({this.Rect}) (orig style: {_origStyle}, exStyle: {_origExStyle}, rect: {_origRect})");
        }

        public override void PostInit()
        {
            // Nothing to do here
        }

        public override bool Hide()
        {
            if (Hwnd != IntPtr.Zero)
            {
                var h = new HWnd(Hwnd);

                // Make sure we do not override a previous exStyleBeforeHide (if Hide() gets called multiple times before Show)
                if (_exStyleBeforeHide == 0)
                {
                    _exStyleBeforeHide = pinvokeHandler.GetWindowLongPtr(Hwnd, GWL_EXSTYLE).ToInt64();
                }

                var exStyle = _exStyleBeforeHide;
                exStyle |= WS_EX_TOOLWINDOW;
                exStyle &= ~(WS_EX_APPWINDOW);
                pinvokeHandler.SetWindowLongPtr(new HandleRef(h, h.Hwnd), GWL_EXSTYLE, new IntPtr(exStyle));
                pinvokeHandler.SetWindowPos(Hwnd, IntPtr.Zero, Rect.Left, Rect.Top, _width, _height, SetWindowPosFlags.SWP_HIDEWINDOW);
            }

            return base.Hide();
        }

        public override bool Show()
        {
            if (Hwnd != IntPtr.Zero)
            {
                var h = new HWnd(Hwnd);

                // Make sure to reset exstyle only if we have a value on it
                if (_exStyleBeforeHide > 0)
                {
                    pinvokeHandler.SetWindowLongPtr(new HandleRef(h, h.Hwnd), GWL_EXSTYLE, new IntPtr(_exStyleBeforeHide));
                }

                _exStyleBeforeHide = 0;

                var show = pinvokeHandler.SetWindowPos(Hwnd, IntPtr.Zero, Rect.Left, Rect.Top, _width, _height, SetWindowPosFlags.SWP_SHOWWINDOW);

                if (!show)
                {
                    var revalidate = windowTracker.RevalidateHwnd(this, Hwnd);
                    Log.Warning($"{this} could not show window again. goingto revalidate window... ...result: {revalidate}");
                    if (revalidate == false)
                    {
                        Parent?.RemoveChild(this);
                    }

                }
            }

            return base.Show();
        }

        public override bool Restore()
        {
            if (Hwnd != IntPtr.Zero)
            {
                var h = new HWnd(Hwnd);
                pinvokeHandler.SetWindowLongPtr(new HandleRef(h, h.Hwnd), GWL_STYLE, new IntPtr(_origStyle));
                pinvokeHandler.SetWindowLongPtr(new HandleRef(h, h.Hwnd), GWL_EXSTYLE, new IntPtr(_origExStyle));
                pinvokeHandler.SetWindowPos(Hwnd, HWND_NOTOPMOST, _origRect.Left, _origRect.Top, _origRect.Right - _origRect.Left, _origRect.Bottom - _origRect.Top, SetWindowPosFlags.SWP_SHOWWINDOW);
            }

            return true;
        }

        public virtual Task QuitNodeImpl()
        {
            Task task = null;
            _quit = true;
            if (Hwnd != IntPtr.Zero)
            {
                task = Task.Run(() =>
                {
                    if (pinvokeHandler.SendMessage(Hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero).ToInt32() != 0)
                    {
                        Log.Error($"received false from PostMessage when sending WM_CLOSE to ({this}), PARENT is null?: {Parent == null}");
                        //Parent?.RemoveChild(this);
                    }

                    if (windowTracker.RevalidateHwnd(this, Hwnd) == false)
                    {
                        Startup.ParserSignal.QueuePipeMessage(new PipeMessageEx
                        {
                            msg = signalHandler.WMC_DESTROY,
                            wParam = (ulong)Hwnd,
                            lParam = 0
                        });
                    }
                });
            }
            else
            {
                task = Task.Run(() => { });
                Parent?.RemoveChild(this);
            }

            return task;
        }

        public override bool QuitNode()
        {
            QuitNodeImpl();
            return true;
        }

        public override Node FindNodeWithId(long id)
        {
            if (this.Id == id)
            {
                return this;
            }

            return null;
        }

        public override void SetFocus(TransferDirection? dir = null)
        {
            if (Hwnd != IntPtr.Zero)
            {
                if (windowTracker.RevalidateHwnd(this, Hwnd) == false)
                {
                    Log.Warning($"{this} should get focus but it no longer exist...");
                    Parent?.RemoveChild(this);
                    return;
                }

                // This is a trick to get foreground focus and then pass on to another process/window
                pinvokeHandler.AllocConsole();
                var hWndConsole = pinvokeHandler.GetConsoleWindow();
                windowTracker.IgnoreHwnd(new IgnoreHwndInfo(hWndConsole, 2));
                pinvokeHandler.SetWindowPos(hWndConsole, IntPtr.Zero, 0, 0, 10, 10, SetWindowPosFlags.SWP_NOZORDER);
                pinvokeHandler.FreeConsole();
                pinvokeHandler.SetForegroundWindow(Hwnd);
            }

            OnWantFocus(this, new WantFocusEventArg(this));
        }

        public override Node AddWindow(IntPtr hWnd, ValidateHwndParams validation = null)
        {
            //Log.Information($"{nameof(WindowNode)}.{nameof(AddWindow)}, (Style: {Style.ToString()}) calling Parent (type: {Parent?.GetType()?.ToString()})");
            return Parent?.AddWindow(hWnd, validation);
        }

        public override bool AddNodes(params Node[] nodes)
        {
            return Parent?.AddNodes(nodes) ?? false;
        }

        public override bool ReplaceNode(Node node, Node newNode)
        {
            return false;
        }

        public override bool RemoveChild(Node child)
        {
            return false;
        }

        public override void SetFullscreenRect(RECT rect)
        {
            _fullscreenRect = rect;

            if (Style == NodeStyle.FullscreenOne)
            {
                SetHwndPos(rect, out _);
            }
        }

        public override bool UpdateRect(RECT r)
        {
            base.UpdateRect(r);
            _width = r.Right - r.Left;
            _height = r.Bottom - r.Top;

            if ((Style == NodeStyle.Tile || Style == NodeStyle.Floating) && Hwnd != IntPtr.Zero)
            {
                if (!SetHwndPos(r, out RECT winRect))
                {
                    return true;
                }

                if (!r.Equals(winRect))
                {
                    var w2 = winRect.Right - winRect.Left;
                    var h2 = winRect.Bottom - winRect.Top;
                    if (w2 <= _width && h2 <= _height)
                    {
                        return true;
                    }

                    //Log.Information($"   ...window ({Name}) dont like the size! {_width}/{_height} != {w2}/{h2}");
                    _width = Math.Max(_width, w2);
                    _height = Math.Max(_height, h2);
                    r.Right = r.Left + _width;
                    r.Bottom = r.Top + _height;
                    base.UpdateRect(r);
                    FixedRect = true;
                    return false;
                }
            }

            return true;
        }

        public override void Dispose()
        {
            Log.Verbose($"{this} Dispose (IsDisposed == {IsDisposed}");
            if (IsDisposed == false)
            {
                OnDeleted(this);

                if (Hwnd != IntPtr.Zero)
                {
                    dragHandler.OnDragStart -= HandleOnDragStart;
                    dragHandler.OnDragMove -= HandleOnDragMove;
                    dragHandler.OnDragEnd -= HandleOnDragEnd;

                    windowTracker.removeWindow(Hwnd);

                    if (_focusListener != Guid.Empty)
                    {
                        focusHandler.RemoveListener(Hwnd, _focusListener);
                    }

                    if (_closeListener != Guid.Empty)
                    {
                        windowHandler.RemoveWindowCloseListener(_closeListener);
                    }

                    if (_styleChangedListener != Guid.Empty)
                    {
                        windowHandler.RemoveWindowStyleChangedListener(_styleChangedListener);
                    }
                }
            }

            base.Dispose();

            Hwnd = IntPtr.Zero;
            _closeListener = _styleChangedListener = _focusListener = Guid.Empty;
        }

        public override string ToString()
        {
            return $"<WindowNode: (id: {Id}) {Hwnd} \"{Name}\" [{ClassName}] style: {_origStyle}, exStyle: {_origExStyle}, parent: {_parentHwnd}>";
        }

        public override bool Equals(object obj)
        {
            var o = obj as WindowNode;
            if (o == null)
            {
                return false;
            }

            return Equals(o);
        }

        public bool Equals([AllowNull] WindowNode other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!EqualityComparer<long>.Default.Equals(Id, other.Id))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136262;
                hash = (hash * 16777617) ^ Id.GetHashCode();
                return hash;
            }
        }

        private void HandleOnDragStart(object _, DragStartEvent arg)
        {
            if (arg.hWnd != Hwnd)
            {
                return;
            }

            if (Style != NodeStyle.Floating)
            {
                Style = NodeStyle.Floating;
            }

            _insideDragAction = true;
        }

        private void HandleOnDragMove(object _, DragMoveEvent arg)
        {
            if (arg.hWnd != Hwnd)
            {
                return;
            }

            // TODO: Maybe we should handle this anyway... make sure x/y equals our current x/y?
            if (_insideDragAction == false)
            {
                return;
            }

            if (Style != NodeStyle.Floating)
            {
                Style = NodeStyle.Floating;
            }

            if (!pinvokeHandler.GetWindowRect(Hwnd, out RECT rect))
            {
                Log.Warning($"{nameof(WindowNode)}.{nameof(HandleOnDragMove)} Could not retrieve window rect for {this}");
                rect = Rect;
                rect.Left = arg.X;
                rect.Top = arg.Y;
            }

            if (!UpdateRect(rect))
            {
                Log.Warning($"{nameof(WindowNode)}.{nameof(HandleOnDragMove)} Could not update window rect for {this} (new rect: {rect})");
            }
        }

        private void HandleOnDragEnd(object sender, DragEndEvent arg)
        {
            if (arg.hWnd != Hwnd)
            {
                return;
            }

            if (Style != NodeStyle.Floating)
            {
                Style = NodeStyle.Floating;
            }

            _insideDragAction = false;
        }

        private string GetWindowText()
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

        private string GetClassName()
        {
            var cb = new StringBuilder(1024);
            if (pinvokeHandler.GetClassName(Hwnd, cb, cb.Capacity) == 0)
            {
                throw new Exception($"{nameof(WindowNode)}.ctor could not retrieve class name for {Hwnd} [{Name}], error: {pinvokeHandler.GetLastError()}");
            }

            return cb.ToString();
        }

        private void SetupWindowNode(long style, long exstyle, string className)
        {
            var _style = NodeStyle.Tile;

            if (((style & PInvoker.WS_CAPTION) != PInvoker.WS_CAPTION) ||
                ((style & PInvoker.WS_SIZEBOX) != PInvoker.WS_SIZEBOX))
            {
                _style = NodeStyle.Floating;
            }

            /* Example to hide window bars.
                NOTE: that it wont work on dwm bars (custom bars like chrome)
            var prev = style;
            style = WS_POPUP;
            var HWnd = new HWnd(Hwnd);
            var result = pinvokeHandler.SetWindowLongPtr(new HandleRef(HWnd, HWnd.Hwnd), GWL_STYLE, new IntPtr(style));
            Log.Information($"WindowNode, removing caption (prev: {prev}, new: {style}) result: {result}");
            */

            Style = _style;
            pinvokeHandler.SetWindowPos(Hwnd, IntPtr.Zero, Rect.Left, Rect.Top, _width, _height, 0);
        }

        private bool SetHwndPos(RECT r, out RECT winRect)
        {
            RestoreHwndIfNeeded();
            _width = r.Right - r.Left;
            _height = r.Bottom - r.Top;
            pinvokeHandler.SetWindowPos(Hwnd, IntPtr.Zero, r.Left, r.Top, _width, _height, 0);
            return pinvokeHandler.GetWindowRect(Hwnd, out winRect);
        }

        private void RestoreHwndIfNeeded()
        {
            var style = pinvokeHandler.GetWindowLongPtr(Hwnd, PInvoker.GWL_STYLE).ToInt64();
            if (((style & PInvoker.WS_MAXIMIZE) == PInvoker.WS_MAXIMIZE) ||
                ((style & PInvoker.WS_MINIMIZE) == PInvoker.WS_MINIMIZE))
            {
                //Log.Information($"{nameof(WindowNode)}.{nameof(RestoreHwndIfNeeded)} goingt o bring hwnd out of minimize/maximize ({this})");
                var h = new HWnd(Hwnd);
                if (pinvokeHandler.SetWindowLongPtr(new HandleRef(h, h.Hwnd), PInvoker.GWL_STYLE, new IntPtr(style & (~PInvoker.WS_MINIMIZE & ~PInvoker.WS_MAXIMIZE))) == IntPtr.Zero)
                {
                    Log.Error($"{nameof(WindowNode)}.{nameof(RestoreHwndIfNeeded)} Could not change {Hwnd} [{Name}] from minimize/maximize to normal (with SetWindowLongPtr), error: {pinvokeHandler.GetLastError()}");
                }

                if (pinvokeHandler.ShowWindow(Hwnd, ShowWindowCmd.SW_RESTORE) == false)
                {
                    Log.Error($"{nameof(WindowNode)}.{nameof(RestoreHwndIfNeeded)} Could not change {Hwnd} [{Name}] from minimize/maximize to normal (With ShowWindow), error: {pinvokeHandler.GetLastError()}");
                }
            }
        }
    }
}