using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Serilog;
using TileWindow.Extensions;
using TileWindow.Nodes.Creaters;
using TileWindow.Nodes.Renderers;
using TileWindow.Trackers;

namespace TileWindow.Nodes
{
    public interface IVirtualDesktop
    {
        bool IsVisible { get; }
        List<Node> FloatingNodes { get; }
        int ActiveScreenIndex { get; }
        int Index { get; }
        Node MyFocusNode { get; }
        Node FocusNode { get; }

        List<Node> Childs { get; }
        Direction Direction { get; }

        IFocusTracker FocusTracker { get; }

        // TODO: Maybe do this as an property and override Childs and then have an underlying collection
        // that both shares (but are of type ScreenNode)
        ScreenNode Screen(int index);

        void NodeAdded(Node node);

        Node FindNodeWithId(long id);

        // TODO: See TODO for Screen
        bool GetScreenRect(int screenIndex, out RECT rect);
        void ScreensChanged(RECT[] screens, Direction direction);
		void HandleMessageDestroy(PipeMessage msg);
        Node HandleNewWindow(IntPtr hwnd);
        void HandleResize(int val, TransferDirection dir);
        void HandleVerticalDirection();
        void HandleHorizontalDirection();
        void HandleFullscreen();
        bool AddFloatingNode(Node node);
        void HandleSwitchFloating();
        bool HandleMoveNodeLeft();
        bool HandleMoveNodeUp();
        bool HandleMoveNodeRight();
        bool HandleMoveNodeDown();
        /// <summary>
        /// switch focus from current focus node to next one in direction
        /// </summary>
        /// <param name="direction">direction to move focus to</param>
        void HandleMoveFocus(TransferDirection direction);
        void TransferFocusNodeToDesktop(IVirtualDesktop destination);
        bool AddNodes(params Node[] nodes);
        void QuitFocusNode();
        bool Show();
        bool Hide();
        bool Restore();
        void HandleLayoutStacking();
        void HandleLayoutToggleSplit();
    }
    
    // Describes an desktop that can contains multiple real screens
    public class VirtualDesktop: FixedContainerNode, IVirtualDesktop, IEquatable<VirtualDesktop>
    {
        private readonly IPInvokeHandler pinvokeHandler;
        private readonly ISignalHandler signalHandler;
        private readonly IScreenNodeCreater screenNodeCreater;
        private readonly IContainerNodeCreater containerNodeCreator;
        private readonly IWindowTracker windowTracker;
        public bool IsVisible { get; private set; }

        public override IVirtualDesktop Desktop => this;

        public virtual List<Node> FloatingNodes { get; private set; }

        public int ActiveScreenIndex
        {
            get
            {
                var node = MyFocusNode;
                if (MyFocusNode == null)
                {
                    FocusTracker.ExplicitSetMyFocusNode(this, Childs.First());
                    return 0;
                }

                for(var i = 0; i < Childs.Count; i++)
                    if (Childs[i] == node)
                        return i;
                
                Log.Warning($"{this} got an unknown node as activenode ({node})");
                FocusTracker.ExplicitSetMyFocusNode(this, Childs.First());
                return 0;
            }
        }

        public int Index { get; private set; }

        public IFocusTracker FocusTracker { get; }

        public ScreenNode Screen(int i)
        {
            if (i >= 0 && i < Childs.Count)
                return Childs[i] as ScreenNode;
            else
                return null;
        }

        public VirtualDesktop(int index, IPInvokeHandler pinvokeHandler, ISignalHandler signalHandler, IRenderer renderer, IScreenNodeCreater screenNodeCreater, IFocusTracker focusTracker, IContainerNodeCreater containerNodeCreator, IWindowTracker windowTracker, RECT rect, Direction direction = Direction.Horizontal)
        : base(renderer, containerNodeCreator, windowTracker, rect, direction, null)
        {
            this.IsVisible = true;
            this.FocusTracker = focusTracker;
            this.Index = index;
            this.pinvokeHandler = pinvokeHandler;
            this.signalHandler = signalHandler;
            this.screenNodeCreater = screenNodeCreater;
            this.FloatingNodes = new List<Node>();
            this.windowTracker = windowTracker;
            this.containerNodeCreator = containerNodeCreator;
            this.FocusTracker.DesktopIndex = index;
            FocusTracker.Track(this);
        }

        public override void PostInit(params Node[] childs)
        {
            if ((childs?.Length ?? 0) == 0)
                throw new ArgumentNullException($"{nameof(VirtualDesktop)} requires at least one {nameof(childs)}");

            base.PostInit(childs);
            childs.First()?.SetFocus();
        }

        public void ScreensChanged(RECT[] screens, Direction direction)
        {
            if (screens.Length < Childs.Count)
            {
                for (int i = Childs.Count - 1; i >= screens.Length; i--)
                {
                    Screen(i).TransferAllChilds(Screen(0), TransferDirection.Left);
                    RemoveChildAt(i);
                }
            }

            for(var i = 0; i <Math.Min(screens.Length, Childs.Count); i++)
            {
                if (Childs[i].Rect.Equals(screens[i]) == false)
                {
                    Childs[i].UpdateRect(screens[i]);
                }
            }

            if (screens.Length > Childs.Count)
            {
                for(var i = Childs.Count; i < screens.Length; i++)
                {
                    InsertChildAt(screenNodeCreater.Create("Screen" + i, screens[i], dir: direction));
                }
            }
        }

        public void NodeAdded(Node node)
        {
            if (node == null)
                return;
            FocusTracker.Track(node);
        }

        public bool GetScreenRect(int screenIndex, out RECT rect)
        {
            rect = new RECT();
            if (screenIndex < 0 || screenIndex >= Childs.Count)
                return false;

            rect = Childs[screenIndex].Rect;
            return true;
        }

		public void HandleMessageDestroy(PipeMessage msg)
        {
            var hwnd = new IntPtr((long)msg.wParam);
            var n = windowTracker.GetNodes(hwnd);
            //Log.Information($"{this}.{nameof(HandleMessageDestroy)} for {hwnd} ({n})");
            n?.QuitNode();
        }

        public Node HandleNewWindow(IntPtr hwnd)
        {
            //Log.Information($"{this}, HandleNewWindow {hwnd}");
            return AddWindow(hwnd);
        }

        public void HandleResize(int val, TransferDirection dir)
        {
            FocusNode?.Resize(val, dir);
        }

        private void ChangeDirectionOnChild(Node child, Direction direction)
        {
            if (child == null)
                return;
            
            if (child.CanHaveChilds)
            {
                child.ChangeDirection(direction);
                return;
            }

            var newChild = containerNodeCreator.Create(child.Rect, dir: direction);
            if (child.Parent?.ReplaceNode(child, newChild) ?? false)
            {
                newChild.AddNodes(child);
            }
            else
            {
                Log.Error($"{this} Could not ReplaceNode {child} on parent: {child.Parent}");
                newChild.Dispose();
            }
        }

        public void HandleVerticalDirection()
        {
            ChangeDirectionOnChild(FocusNode, Direction.Vertical);
        }
        public void HandleHorizontalDirection()
        {
            ChangeDirectionOnChild(FocusNode, Direction.Horizontal);
        }

        public void HandleFullscreen()
        {
            if (FocusNode == null)
            {
                Log.Warning($"VirtualDesktop got HandleFullscreen but I have no FocusNode");
                return;
            }

            if (FocusNode.Style == NodeStyle.FullscreenOne)
                FocusNode.Style = NodeStyle.Tile;
            else
                FocusNode.Style = NodeStyle.FullscreenOne;
        }

        public bool AddFloatingNode(Node node)
        {
            TakeOverNode(node);
            node.Style = NodeStyle.Floating;
            FloatingNodes.Add(node);

            if(IsVisible)
                node.Show();
            else
                node.Hide();

            return true;
        }

        public void HandleLayoutStacking()
        {
            if (FocusNode == null)
                return;

            if (typeof(StackRenderer).IsInstanceOfType(FocusNode.GetRenderer()) == false)
                FocusNode.SetRenderer(new StackRenderer(pinvokeHandler, signalHandler));
        }

        public void HandleLayoutToggleSplit()
        {
            if (FocusNode == null)
                return;

            if (typeof(TileRenderer).IsInstanceOfType(FocusNode.GetRenderer()) == false)
                FocusNode.SetRenderer(new TileRenderer());
            else
            {
                var p = FocusNode;
                while(p != null)
                {
                    if (p.CanHaveChilds)
                    {
                        if (p.Direction == Direction.Horizontal)
                            p.ChangeDirection(Direction.Vertical);
                        else
                            p.ChangeDirection(Direction.Horizontal);
                        break;
                    }

                    p = p.Parent;
                }
            }
        }

        public void HandleSwitchFloating()
        {
            if (FocusNode == null || Childs.IndexOf(FocusNode) >= 0)
                return;

            // Going to switch to floating
            if (FocusNode.Style != NodeStyle.Floating)
            {
                if (!(FocusNode.Parent?.DisconnectChild(FocusNode) ?? false))
                {
                    return;
                }

                TakeOverNode(FocusNode);
                FocusNode.Style = NodeStyle.Floating;
                FloatingNodes.Add(FocusNode);
            }
            else
            {
                var i = FloatingNodes.IndexOf(FocusNode);
                DisconnectChild(FocusNode);
                var screen = Childs.FirstOrDefault();
                var area = 0L;
                foreach(var c in Childs)
                {
                    if (!typeof(ContainerNode).IsInstanceOfType(c))
                        continue;

                    var t = c.Rect.Intersection(FocusNode.Rect).CalcArea();
                    if (t > area)
                    {
                        area = t;
                        screen = c;
                    }
                }

                if (screen != null)
                {
                    FocusNode.Style = NodeStyle.Tile;
                    if (!((ContainerNode)screen).AddNodes(FocusNode))
                    {
                        Log.Warning($"Could not take node from Floating to Tile");
                        TakeOverNode(FocusNode);
                        FocusNode.Style = NodeStyle.Floating;
                        return;
                    }
                }
            }
        }

        public bool HandleMoveNodeLeft()
        {
            FocusNode?.MoveLeft();
            return FocusNode?.Style == NodeStyle.Floating;
        }

        public bool HandleMoveNodeUp()
        {
            FocusNode?.MoveUp();
            return FocusNode?.Style == NodeStyle.Floating;
        }

        public bool HandleMoveNodeRight()
        {
            FocusNode?.MoveRight();
            return FocusNode?.Style == NodeStyle.Floating;
        }

        public bool HandleMoveNodeDown()
        {
            FocusNode?.MoveDown();
            return FocusNode?.Style == NodeStyle.Floating;
        }

        /// <summary>
        /// switch focus from current focus node to next one in direction
        /// </summary>
        /// <param name="direction">direction to move focus to</param>
        public void HandleMoveFocus(TransferDirection direction)
        {
            FocusNode?.FocusNodeInDirection(FocusNode, direction);
        }

        public void TransferFocusNodeToDesktop(IVirtualDesktop destination)
        {
            if (FocusNode == null)
            {
                Log.Warning($"{nameof(VirtualDesktop)}.{nameof(TransferFocusNodeToDesktop)} called but FocusNode is null");
                return;
            }

            if (FocusNode.Style == NodeStyle.Floating)
            {
                MoveFloatingNodeToDesktop(FocusNode, destination);
                return;
            }

            var old = FocusNode;
            // TODO: Will focus node get updated by an event?
            if (old.Parent?.TransferNodeToAnotherDesktop(old, destination) ?? false)
            {
                FocusTracker.Untrack(old);
                if (FocusNode == null)
                {
                    Childs.FirstOrDefault()?.SetFocus();
                }
            }
        }

        protected virtual void MoveFloatingNodeToDesktop(Node node, IVirtualDesktop destination)
        {
            var old = node;
            if (!DisconnectChild(node))
            {
                Log.Warning($"{nameof(VirtualDesktop)}.{nameof(MoveFloatingNodeToDesktop)} could not disconnect floating node from desktop: {node}");
                return;
            }

            if (!destination.AddNodes(node))
            {
                Log.Warning($"{nameof(VirtualDesktop)}.{nameof(MoveFloatingNodeToDesktop)} could not transfer node to desktop: {destination}, NODE: {node}");
                return;
            }

            FocusTracker.Untrack(old);
        }

        public override bool DisconnectChild(Node child)
        {
            if (child == null || child.Style != NodeStyle.Floating)
                return base.DisconnectChild(child);

            var i = FloatingNodes.IndexOf(child);
            if (i == -1)
            {
                Log.Warning($"{nameof(VirtualDesktop)}.{nameof(DisconnectChild)} could not find floating node in array. node: {child}");
                return false;
            }

            var gotFocus = child == MyFocusNode;
            child.StyleChanged -= ChildNodeStyleChange;
            child.RequestRectChange -= OnChildRequestRectChange;
            if (child.Parent == this)
                child.Parent = null;

            FloatingNodes.RemoveAt(i);

            if (gotFocus)
                FocusTracker.ExplicitSetMyFocusNode(this, Childs.First());

            return true;
        }

        public void QuitFocusNode()
        {
            FocusNode?.QuitNode();
        }

        public override bool Show()
        {
            IsVisible = true;
            var result = base.Show();
            FloatingNodes.ForEach(c => result = c.Show() && result);

            if (FocusTracker.FocusNode() == null)
                Childs.First().SetFocus();
            else
                FocusTracker.FocusNode()?.SetFocus();

            return result;
        }

        public override bool Hide()
        {
            IsVisible = false;
            var result = base.Hide();            
            FloatingNodes.ForEach(c => result = c.Hide() && result);
            return result;
        }

        public override bool RemoveChild(Node child)
        {
            if (child.Style != NodeStyle.Floating)
                return false;
            
            if (!DisconnectChild(child))
            {
                Log.Warning($"{nameof(VirtualDesktop)}.{nameof(RemoveChild)} Could not disconnect floating node {child}");
                return false;
            }

            child.Dispose();
            return true;
        }

        public override bool FocusNodeInDirection(Node focusNode, TransferDirection direction)
        {
            Node f = focusNode;
            var i = Childs.IndexOf(focusNode);

            // if focusnode is not a child and it is fullscreen, then
            // try to traverse its parent path and find the correct child
            if (i == -1 && focusNode.Style == NodeStyle.FullscreenOne)
            {
                Node p = focusNode.Parent;
                while(p != null)
                {
                    if ((i = Childs.IndexOf(p)) >= 0)
                    {
                        f = p;
                        break;
                    }

                    p = p.Parent;
                }

                if (p == null || i == -1)
                {
                    Log.Warning($"{nameof(VirtualDesktop)}.{nameof(FocusNodeInDirection)} Could not find parent node of focusNode that belongs to VirtualDesktop");
                    return false;
                }
            }
            else if (i == -1)
                return false;

            switch(direction)
            {
                case TransferDirection.Left:
                    if (Direction == Direction.Vertical || Childs.Count == 1 || i == 0)
                        return Parent?.FocusNodeInDirection(this, direction) ?? false;

                    i--;
                    Childs[i].SetFocus(direction);
                    return true;
                case TransferDirection.Right:
                    if (Direction == Direction.Vertical || Childs.Count == 1 || i == Childs.Count - 1)
                        return Parent?.FocusNodeInDirection(this, direction) ?? false;

                    i++;
                    Childs[i].SetFocus(direction);
                    return true;
                case TransferDirection.Up:
                    if (Direction == Direction.Horizontal || Childs.Count == 1 || i == 0)
                        return Parent?.FocusNodeInDirection(this, direction) ?? false;

                    i--;
                    Childs[i].SetFocus(direction);
                    return true;
                case TransferDirection.Down:
                    if (Direction == Direction.Horizontal || Childs.Count == 1 || i == Childs.Count - 1)
                        return Parent?.FocusNodeInDirection(this, direction) ?? false;

                    i++;
                    Childs[i].SetFocus(direction);
                    return true;
                default:
                    Log.Error($"{nameof(VirtualDesktop)}.{nameof(FocusNodeInDirection)} was called with unknown {nameof(TransferDirection)} ({direction.ToString()})");
                    break;
            }

            return false;
        }

        public override bool AddNodes(params Node[] nodes)
        {
            var result = true;
//Log.Information($"{this} will handle AddNodes for {nodes?.Count()} nodes");
            foreach(var child in nodes)
            {
                if (child.Style == NodeStyle.Floating)
                {
//Log.Information($"  >>> Adding floating node");
                    result = this.AddFloatingNode(child) && result;
                }
                else
                {
//Log.Information($"   >>> not floating.. calling base add nodes {child.GetType().ToString()}");
                    result = base.AddNodes(child) && result;
                }
            }

Log.Information($"{this} my focus node is: {MyFocusNode}");
            return result;
        }

        public override void ChildWantMove(Node child, TransferDirection direction)
        {
            if (child.Style != NodeStyle.Floating)
            {
                base.ChildWantMove(child, direction);
                return;
            }

            var r = child.Rect;
            switch (direction)
            {
                case TransferDirection.Left:
                    r.Left -= 20;
                    r.Right -= 20;
                    break;
                case TransferDirection.Up:
                    r.Top -= 20;
                    r.Bottom -= 20;
                    break;
                case TransferDirection.Right:
                    r.Left += 20;
                    r.Right += 20;
                    break;
                case TransferDirection.Down:
                    r.Top += 20;
                    r.Bottom += 20;
                    break;
                default:
                    break;
            }

            child.UpdateRect(r);
        }

        protected override void OnChildRequestRectChange(object sender, RequestRectChangeEventArg args)
        {
            if (args.Requester.Style != NodeStyle.Floating)
            {
                base.OnChildRequestRectChange(sender, args);
                return;
            }

            var i = FloatingNodes.IndexOf(args.Requester);
            if (i == -1)
            {
                Log.Warning($"{nameof(VirtualDesktop)}.{nameof(OnChildRequestRectChange)}, Could not find floating node in list");
                return;
            }

            args.Requester.UpdateRect(args.Requester.Rect);
        }
        
        public override bool Equals(object obj)
        {
            var o = obj as VirtualDesktop;
            if (o == null)
                return false;
            return Equals(o);
        }

        public bool Equals([AllowNull] VirtualDesktop other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;

            if (
            !EqualityComparer<long>.Default.Equals(Id, other.Id))
            {
                return false;
            }
            
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166636261;
                hash = (hash * 16787619) ^ Id.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => $"<{nameof(VirtualDesktop)} #{Index}>";
    }
}