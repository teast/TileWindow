using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using TileWindow.Nodes.Renderers;
using TileWindow.Trackers;

namespace TileWindow.Nodes
{
    public enum Direction
    {
        Horizontal,
        Vertical
    }

    public enum NodeTypes
    {
        Screen,
        Container,
        Leaf
    }

    /// <summary>
    /// Various style that a node can have
    /// </summary>
    public enum NodeStyle
    {
        /// <summary>
        /// Normal tile mode
        /// </summary>
        Tile,

        /// <summary>
        /// Fullscreen mode that covers one screen
        /// </summary>
        FullscreenOne,

        /// <summary>
        /// Fullscreen mode that covers all screens
        /// </summary>
        FullscreenAll,

        /// <summary>
        /// Floating node
        /// </summary>
        Floating
    }

    public enum TransferDirection
    {
        Left,
        Up,
        Right,
        Down
    }

    public class RequestRectChangeEventArg
    {
        public Node Requester { get; }
        public RECT OldRect { get; }
        public RECT NewRect { get; }

        public RequestRectChangeEventArg(Node requester, RECT oldRect, RECT newRect)
        {
            Requester = requester;
            OldRect = oldRect;
            NewRect = newRect;
        }
    }

    public class StyleChangedEventArg
    {
        public Node Source { get; set; }
        public NodeStyle Prev { get; set; }

        public StyleChangedEventArg()
        {

        }

        public StyleChangedEventArg(Node source, NodeStyle prev)
        {
            Source = source;
            Prev = prev;
        }
    }

    public class WantFocusEventArg
    {
        public Node Source { get; set; }

        public WantFocusEventArg()
        {

        }

        public WantFocusEventArg(Node source)
        {
            Source = source;
        }
    }
    public abstract class Node : IDisposable, IEquatable<Node>
    {
        private static long _globalCounter = 1;
        private readonly long _id = (_globalCounter++);
        public long Id => _id;
        public string Name { get; set; }
        public string ShortName => Name?.Substring(0, Math.Min(Name?.Length ?? 0, 10)) ?? "";

        public Node Parent { get; set; }
        public IRenderer Renderer { get; set; }
        public virtual IVirtualDesktop Desktop => Parent?.Desktop;
        public Node MyFocusNode => Desktop?.FocusTracker?.MyLastFocusNode(this);
        public Node FocusNode => Desktop?.FocusTracker?.FocusNode();

        /// <summary>
        /// Current size and position of this node
        /// </summary>
        public virtual RECT Rect { get; private set; }

        /// <summary>
        /// True if this nodes Rect should be fixed
        /// </summary>
        public bool FixedRect { get; set; }

        #region Events
        public event EventHandler MyFocusNodeChanged;

        /// <summary>
        /// Gets called when the nodes Style property changes
        /// </summary>
        public event EventHandler<StyleChangedEventArg> StyleChanged;

        /// <summary>
        /// Gets called when the node want a new position and/or size
        /// </summary>
        public event EventHandler<RequestRectChangeEventArg> RequestRectChange;

        /// <summary>
        /// gets called when the node want focus
        /// </summary>
        public event EventHandler<WantFocusEventArg> WantFocus;

        /// <summary>
        /// Gets called when the node should be deleted and disposed
        /// </summary>
        public event EventHandler Deleted;

        #endregion

        #region "Event triggers"
        public void RaiseMyFocusNodeChanged()
        {
            MyFocusNodeChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Trigger an <see cref="Deleted" /> event
        /// </summary>
        /// <param name="sender">The object that triggers the event</param>
        protected virtual void OnDeleted(Node sender) => Deleted?.Invoke(sender, null);

        protected virtual void OnRequestRectChange(Node sender, RequestRectChangeEventArg arg)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            var handler = RequestRectChange;
            if (handler != null)
            {
                handler?.Invoke(sender, arg);
            }
        }

        protected virtual void OnWantFocus(Node sender, WantFocusEventArg arg)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            var handler = WantFocus;
            if (handler != null)
            {
                handler?.Invoke(sender, arg);
            }
        }

        protected virtual void OnStyleChanged(Node sender, StyleChangedEventArg args)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            var handler = StyleChanged;
            if (handler != null)
            {
                handler(sender, args);
            }
        }
        #endregion

        private NodeStyle _style;

        /// <summary>
        /// The nodes current style
        /// </summary>
        public virtual NodeStyle Style
        {
            get => _style;
            set
            {
                var prev = _style;
                _style = value;
                StyleChanged?.Invoke(this, new StyleChangedEventArg(this, prev));
            }
        }

        public int Depth { get; set; }
        public Direction Direction { get; protected set; }

        public abstract NodeTypes WhatType { get; }

        public virtual bool CanHaveChilds => false;

        protected bool IsDisposed { get; set; }
        public Node(RECT rect, Direction direction = Direction.Horizontal, Node parent = null)
        {
            FixedRect = false;
            IsDisposed = false;
            Rect = rect;
            Direction = direction;
            Parent = parent;
            Style = NodeStyle.Tile;
        }

        /// <summary>
        /// Should be called directly after ctor call
        /// </summary>
        public abstract void PostInit();

        /// <summary>
        /// Update renderer for given node
        /// </summary>
        /// <param name="newRenderer">the new renderer to use</param>
        public virtual void SetRenderer(IRenderer newRenderer) => Parent?.SetRenderer(newRenderer);

        /// <summary>
        /// Retrieves the closest renderer to this node
        /// </summary>
        /// <returns>closest renderer or null if no renderer were found</returns>
        public virtual IRenderer GetRenderer()
        {
            if (Renderer != null)
            {
                return Renderer;
            }

            return Parent?.GetRenderer();
        }

        public virtual void Resize(int val, TransferDirection direction)
        {
            var r = Rect;
            switch (direction)
            {
                case TransferDirection.Left:
                    r.Left += val;
                    FixedRect = true;
                    break;
                case TransferDirection.Up:
                    r.Top += val;
                    FixedRect = true;
                    break;
                case TransferDirection.Right:
                    r.Right += val;
                    FixedRect = true;
                    break;
                case TransferDirection.Down:
                    r.Bottom += val;
                    FixedRect = true;
                    break;
                default:
                    Log.Warning($"{nameof(Node)}.{nameof(Resize)}({this.ToString()}) unknown direction: {direction.ToString()}");
                    break;
            }

            var old = Rect;
            Rect = r;
            OnRequestRectChange(this, new RequestRectChangeEventArg(this, old, r));
        }

        /// <summary>
        /// Remove a child node
        /// </summary>
        /// <returns>return true if the child was removed</returns>
        public abstract bool RemoveChild(Node child);

        /// <summary>
        /// Remove a child node with a window handler
        /// </summary>
        /// <param name="hWnd">window handler to remove child for</param>
        /// <returns>return true if the child was removed</returns>
        //public abstract bool RemoveWindow(IntPtr hWnd);

        /// <summary>
        /// Add a new node for a window handler
        /// </summary>
        /// <param name="hWnd">window handler to add a node for</param>
        /// <param name="validation">Instruction on how to validate window handler</param>
        /// <returns>return the new node</returns>
        public abstract Node AddWindow(IntPtr hWnd, ValidateHwndParams validation = null);

        public abstract bool AddNodes(params Node[] nodes);

        public abstract bool ReplaceNode(Node node, Node newNode);

        public abstract Node FindNodeWithId(long id);

        /// <summary>
        /// The node should do whatever it needs to make itself invisible for the user
        /// </summary>
        /// <returns>true if no problems</returns>
        public virtual bool Hide()
        {
            return Renderer?.Hide() ?? true;
        }

        /// <summary>
        /// The node should do whatever it needs to make itself visible for the user
        /// </summary>
        /// <returns>true if no problems</returns>
        public virtual bool Show()
        {
            return Renderer?.Show() ?? true;
        }

        /// <summary>
        /// Should restore everything to its original state (before TileWindow took over)
        /// </summary>
        /// <returns>true if no problems</returns>
        public abstract bool Restore();

        /// <summary>
        /// If a child node want to change direction. Then the parent node could do the direction change
        /// instead of child node
        /// </summary>
        /// <param name="oldChild">child node that want to chagne direction</param>
        /// <param name="direction">direction to chagne to</param>
        /// <returns>return true if action was taken</returns>
        /// <remarks>
        /// ContainerNode will change its own direction if it only has one child on it.
        /// But if ContainerNode got multiple childs, then it will add a new ContainerNode
        /// and put <c ref="oldChild" /> in the new ContainerNode.
        /// </remarks>
        //public abstract bool ReplaceChildDirection(Node oldChild, Direction direction);

        /// <summary>
        /// Change direction on this node
        /// </summary>
        /// <param name="dir">new direction</param>
        /// <remarks>
        /// Default this till update the nodes direction and then call <c ref="ReplaceChildDirection"/> on Parent node
        /// </remarks>
        public virtual void ChangeDirection(Direction dir)
        {
            Direction = dir;
        }

        /// <summary>
        /// Remove node and its content
        /// </summary>
        /// <returns>return true if handled</returns>
        public virtual bool QuitNode()
        {
            Log.Warning($"{this.GetType().ToString()}(Node).{nameof(QuitNode)} not implemented");
            return false;
        }

        /// <summary>
        /// Specific node will now have focus
        /// </summary>
        public virtual void SetFocus(TransferDirection? dir = null)
        {
            OnWantFocus(this, new WantFocusEventArg(this));
        }

        /// <summary>
        /// Disconnect an child node from this node
        /// </summary>
        /// <param name="child">node to disconnect from</param>
        /// <returns>true if no problems</returns>
        public virtual bool DisconnectChild(Node child)
        {
            return false;
        }

        /// <summary>
        /// Move child node that got focus to <c ref="dir"/> direction
        /// </summary>
        /// <param name="dir">direction to move the child node in</param>
        //public abstract void ChangeDirectionOnFocusChild(Direction dir);

        /// <summary>
        /// Move <c ref="child" /> in <c ref="direction"/> direction
        /// </summary>
        /// <param name="child">node to move</param>
        /// <param name="direction">direction to move the node in</param>
        public virtual void ChildWantMove(Node child, TransferDirection direction) { }

        /// <summary>
        /// Indicate that focus should change from <c ref="focusNode" /> to next one in <c ref="direction" />
        /// </summary>
        /// <param name="focusNode">current node that got focus</param>
        /// <param name="direction">direction to move focus from that node</param>
        /// <returns>true if it was a success</returns>
        public virtual bool FocusNodeInDirection(Node focusNode, TransferDirection direction)
        {
            return Parent?.FocusNodeInDirection(this, direction) ?? false;
        }

        /// <summary>
        /// Request to transfer <c ref="nodeToTransfer" /> to this <c ref="Node" /> instance.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="nodeToTransfer"></param>
        /// <param name="direction"></param>
        /// <param name="nodeGotFocus"></param>
        /// <returns></returns>
        /// <remarks>
        /// if <c ref="child" /> is null then it is parent node that calls this method.
        /// </remarks>
        public virtual bool TransferNode(Node child, Node nodeToTransfer, TransferDirection direction, bool nodeGotFocus)
        {
            return false;
        }

        public virtual bool TransferNodeToAnotherDesktop(Node child, IVirtualDesktop destination)
        {
            return false;
        }

        /// <summary>
        /// Move current node left
        /// </summary>
        public virtual void MoveLeft()
        {
            if (Style == NodeStyle.FullscreenOne)
            {
                return;
            }

            Parent?.ChildWantMove(this, TransferDirection.Left);
        }

        /// <summary>
        /// Move current node up
        /// </summary>
        public virtual void MoveUp()
        {
            if (Style == NodeStyle.FullscreenOne)
            {
                return;
            }

            Parent?.ChildWantMove(this, TransferDirection.Up);
        }

        /// <summary>
        /// Move current node right
        /// </summary>
        public virtual void MoveRight()
        {
            if (Style == NodeStyle.FullscreenOne)
            {
                return;
            }

            Parent?.ChildWantMove(this, TransferDirection.Right);
        }

        /// <summary>
        /// Move current node down
        /// </summary>
        public virtual void MoveDown()
        {
            if (Style == NodeStyle.FullscreenOne)
            {
                return;
            }

            Parent?.ChildWantMove(this, TransferDirection.Down);
        }

        public virtual void SetFullscreenRect(RECT rect)
        {
        }

        /// <summary>
        /// Update current nodes <c ref="Rect" />
        /// </summary>
        /// <param name="r">new rect</param>
        /// <returns>true if the new rect was accepted. if False then check the nodes Rect for what values are accepted</returns>
        /// <remarks>
        /// One way when this can return False is if the window in this node cannot shrink as much as the container wants
        /// </remarks>
        public virtual bool UpdateRect(RECT r)
        {
            this.Rect = r;
            return true;
        }

        /// <summary>
        /// Copy <c ref="source" /> ndoes values to <c ref="dest" /> node
        /// </summary>
        /// <param name="source">node to copy values from</param>
        /// <param name="dest">node to copy values to</param>
        public virtual void CopyValues(Node source, Node dest)
        {
            dest.Parent = source.Parent;
            dest.Rect = source.Rect;
            dest.Direction = source.Direction;
        }

        public virtual void Dispose()
        {
            Log.Verbose($"(NODE.cs){this} Dispose (IsDisposed == {IsDisposed}");
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
        }

        public override bool Equals(object obj)
        {
            var o = obj as Node;
            if (o == null)
            {
                return false;
            }

            return Equals(o);
        }

        public bool Equals([AllowNull] Node other)
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
                int hash = (int)2166136261;
                hash = (hash * 16777619) ^ Id.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => $"[{nameof(Node)}{this.GetType().ToString()}) (id: {Id})]";
    }
}