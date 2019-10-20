using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using TileWindow.Nodes.Creaters;
using TileWindow.Trackers;

namespace TileWindow.Nodes
{
    public class ContainerNode: Node, IEquatable<Node>
    {
        private int _width;
        private int _height;
        private int _allocatableWidth;
        private int _allocatableHeight;
        private bool _isVisible = false;
        public List<Node> Childs { get; protected set; }

        private readonly IContainerNodeCreater containerNodeCreator;
        private readonly IWindowTracker windowTracker;
        private List<int> _ignoreChildsOnUpdateRect;
        public override NodeTypes WhatType =>NodeTypes.Container;
        public override bool CanHaveChilds => true;

        public ContainerNode(/*IFocusTracker focusTracker, */IContainerNodeCreater containerNodeCreator, IWindowTracker windowTracker, RECT rect, Direction direction = Direction.Horizontal, Node parent = null) : base(rect, direction, parent)
        {
            this.containerNodeCreator = containerNodeCreator;
            this.windowTracker = windowTracker;
            Childs = new List<Node>();
            _ignoreChildsOnUpdateRect = new List<int>();
        }

        public override void PostInit()
        {
            PostInit(new Node[0]);
        }

        public virtual void PostInit(params Node[] childs)
        {
            if (childs?.Length > 0)
                foreach(var c in childs)
                    InsertChildAt(c);

            RecalcDeltaWithHeight();
//Log.Information($"{nameof(ContainerNode)}.ctor rect: {rect}, Rect: {Rect}, _width, _height: {_width}, {_height}");

            if (Childs.Count > 0)
            {
//Log.Information($"   ...Calling UpdateChildRect from ctor");
                if (UpdateChildRect(0, Childs.Count, out RECT newRect) == false)
                {
                    base.UpdateRect(newRect);
                    RecalcDeltaWithHeight();
                    OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
                }
            }
        }

        protected virtual void ChildNodeStyleChange(object sender, StyleChangedEventArg args)
        {
//Log.Information($"Container.ChildNodeStylechange ({this.ToString()}) called. going to trigger OnSTyleChanged with this, source");
            OnStyleChanged(this, args);
        }

        public override bool Hide()
        {
            var result = true;
            Childs.ForEach(c => result = c.Hide() && result);
            _isVisible = false;
            return result;
        }

        public override bool Show()
        {
            var result = true;
            Childs.ForEach(c => result = c.Show() && result);
            _isVisible = true;
            return result;
        }

        public override bool Restore()
        {
            var result = true;
            Childs.ForEach(c => result = c.Restore() && result);
            return result;
        }

        public override Node AddWindow(IntPtr hWnd)
        {
            int? index = null;
            var focusNode = Desktop.FocusTracker.MyLastFocusNode(this);
            if (focusNode != null)
            {
                if (focusNode.CanHaveChilds)
                {
//Log.Information($"Container.AddWindow {hWnd} goingt o call AddWindow on FocusNode ({FocusNode?.GetType()?.ToString()}");
                    return focusNode.AddWindow(hWnd);
                }

                index = Childs.IndexOf(focusNode) + 1;
            }

            var r = GetRect(Childs.Count, Childs.Count + 1);
            var node = windowTracker.CreateNode(hWnd);
//Log.Information($"ContainerNode.AddWindow {hWnd} Creating node (null? {(node == null)})");
            if (node == null)
                return null;

            if (node.Style == NodeStyle.Floating)
            {
//Log.Information($"ContainerNode.AddWindow, Floating node, going to call parent AddNodes");
                if (Parent?.AddNodes(node) ?? false)
                    return node;
                else
                    return null;
            }

//Log.Information($"ContainerNode.AddWindow going to insert node at index {index}");
            //focusTracker.Track(node);
            InsertChildAt(node, index);
            RecalcDeltaWithHeight();
            if (!UpdateChildRect(0, Childs.Count, out RECT newRect))
            {
                base.UpdateRect(newRect);
                RecalcDeltaWithHeight();
                OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
            }

            return node;
        }

        public override bool DisconnectChild(Node child)
        {
            var i = Childs.IndexOf(child);
            if (i == -1)
                return false;
            
            RemoveChildAt(i, false);
            return true;
        }

        public override bool AddNodes(params Node[] nodes)
        {
            if ((nodes?.Length ?? 0) == 0)
            {
                Log.Warning("ContainerNode.AddNode was called with null/empty nodes array!");
                return false;
            }

            int? index = null;
            var focus = MyFocusNode;
            if ((index = Childs.IndexOf(focus)) == -1)
                index = null;

//Log.Information($"ContainerNode.AddNode going to try and add {nodes.Count()} nodes");
            var floatingNodes = new List<Node>();
            foreach (var node in nodes)
            {
                if (node.Style == NodeStyle.Floating)
                    floatingNodes.Add(node);
                else
                {
//Log.Information($"   >>> ContainerNode.AddNode Inserting node in my Childs list...");
                    //focusTracker.Track(node);
                    InsertChildAt(node, index);
                }
            }

            if (floatingNodes.Count > 0)
            {
//Log.Information($"   >>> got {floatingNodes.Count} floating nodes to pass to parent");
                Parent?.AddNodes(floatingNodes.ToArray());
            }

//Log.Information($"   >>> ContainerNode.AddNode RecalcDeltaWithHeight");
            RecalcDeltaWithHeight();
            if (!UpdateChildRect(0, Childs.Count, out RECT newRect))
            {
                base.UpdateRect(newRect);
                RecalcDeltaWithHeight();
                OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
            }

//Log.Information($"   >>> ContainerNode.AddNode return true");
            return true;
        }
        
        public override void ChangeDirection(Direction dir)
        {
            var old = Direction;
            base.ChangeDirection(dir);

            if (old != dir)
            {
                if (!UpdateChildRect(0, Childs.Count, out RECT newRect))
                {
                    base.UpdateRect(newRect);
                    RecalcDeltaWithHeight();
                    OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
                }
            }
        }

        public override void SetFocus(TransferDirection? dir = null)
        {
            if (Childs.Count == 0)
            {
                base.SetFocus(dir);
                return;
            }

            if (dir == TransferDirection.Left || dir == TransferDirection.Up)
                Childs[Childs.Count - 1].SetFocus(dir);
            else
                Childs[0].SetFocus(dir);
        }

        public override bool FocusNodeInDirection(Node focusNode, TransferDirection direction)
        {
            if (focusNode.Style == NodeStyle.FullscreenOne)
                return Parent?.FocusNodeInDirection(focusNode, direction) ?? false;

            if (focusNode == this)
                return Parent?.FocusNodeInDirection(focusNode, direction) ?? false;
            var i = Childs.IndexOf(focusNode);
//Log.Information($"Container.FocusNodeInDirection child: {focusNode.GetType().ToString()}, i: {i}, direction: {direction.ToString()}");
            if (i == -1)
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
                    Log.Error($"{nameof(ContainerNode)}.{nameof(FocusNodeInDirection)} was called with unknown {nameof(TransferDirection)} ({direction.ToString()})");
                    break;
            }

            return false;
        }

        public override bool TransferNodeToAnotherDesktop(Node child, IVirtualDesktop destination)
        {
            var ret = false;
            var i = Childs.IndexOf(child);
            if (i == -1)
                return false;

//Log.Information($"ContainerNode.TransferNodeToAnotherDesktop, child: {child.GetType().ToString()}, goign to call destinations AddNodes....");            
            _ignoreChildsOnUpdateRect.Add(i);
            if (destination.AddNodes(child))
            {
                _ignoreChildsOnUpdateRect.Remove(i);
                ret = true;
                Desktop.FocusTracker.Untrack(Childs[i]);
                RemoveChildAt(i, false);
                if (i > 0)
                    Childs[i-1].SetFocus();
                else if (i < Childs.Count)
                    Childs[i].SetFocus();
            }
            else
            {
                _ignoreChildsOnUpdateRect.Remove(i);
            }

            return ret;
        }

        public override bool TransferNode(Node child, Node nodeToTransfer, TransferDirection direction, bool nodeGotFocus)
        {
            var i = child == null ? Childs.Count : Childs.IndexOf(child);
//Log.Information($"{nameof(ContainerNode)}.{nameof(TransferNode)} called, i: {i}, Node.Direction: {Direction.ToString()}, direction: {direction.ToString()} Parent: {Parent.GetType().ToString()} ({Parent.WhatType.ToString()})");
            if (i == -1)
                return false;
            
            if ( child != null &&
                ((Direction == Direction.Horizontal && (direction == TransferDirection.Up || direction == TransferDirection.Down)) ||
                (Direction == Direction.Vertical && (direction == TransferDirection.Left || direction == TransferDirection.Right))))
            {
                // It would seem strange if an node that is moved up ends up to the left/right on an horizontal node.. so lets just move it further up
                // It would seem strange if an node that is moved left ends up above/below on an vertical node.. so lets just move it further up
                return (Parent?.TransferNode(this, nodeToTransfer, direction, nodeGotFocus) ?? false);
            }

            switch (direction)
            {
                case TransferDirection.Left:
                {
                    InsertChildAt(nodeToTransfer, i);
                    RecalcDeltaWithHeight();

                    // Only ask parent to recalc if the call is not from parent (child != null)
                    if (!UpdateChildRect(0, Childs.Count, out RECT newRect) && child != null)
                    {
                        base.UpdateRect(newRect);
                        RecalcDeltaWithHeight();
                        OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
                    }

                    return true;
                }
                case TransferDirection.Up:
                {
                    InsertChildAt(nodeToTransfer, i);
                    RecalcDeltaWithHeight();

                    // Only ask parent to recalc if the call is not from parent (child != null)
                    if (!UpdateChildRect(0, Childs.Count, out RECT newRect) && child != null)
                    {
                        base.UpdateRect(newRect);
                        RecalcDeltaWithHeight();
                        OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
                    }

                    return true;
                }
                case TransferDirection.Right:
                {
                    InsertChildAt(nodeToTransfer, child == null ? 0 : Math.Min(Childs.Count, i + 1));
                    RecalcDeltaWithHeight();

                    // Only ask parent to recalc if the call is not from parent (child != null)
                    if (!UpdateChildRect(0, Childs.Count, out RECT newRect) && child != null)
                    {
                        base.UpdateRect(newRect);
                        RecalcDeltaWithHeight();
                        OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
                    }

                    return true;
                }
                case TransferDirection.Down:
                {
                    InsertChildAt(nodeToTransfer, child == null ? 0 : Math.Min(Childs.Count, i + 1));
                    RecalcDeltaWithHeight();

                    // Only ask parent to recalc if the call is not from parent (child != null)
                    if (!UpdateChildRect(0, Childs.Count, out RECT newRect) && child != null)
                    {
                        base.UpdateRect(newRect);
                        RecalcDeltaWithHeight();
                        OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
                    }

                    return true;
                }
                default:
                    Log.Error($"{nameof(ContainerNode)}.{nameof(TransferNode)} was called with unknown {typeof(TransferDirection).ToString()} ({direction.ToString()})");
                    break;
            }

            return false;
        }

        public override void ChildWantMove(Node child, TransferDirection direction)
        {
            switch (direction)
            {
                case TransferDirection.Left:
                    MoveChildLeft(child);
                    break;
                case TransferDirection.Up:
                    MoveChildUp(child);
                    break;
                case TransferDirection.Right:
                    MoveChildRight(child);
                    break;
                case TransferDirection.Down:
                    MoveChildDown(child);
                    break;
                default:
                    Log.Error($"{nameof(ContainerNode)}.{nameof(ChildWantMove)} was called with unknown {typeof(TransferDirection).ToString()} ({direction.ToString()})");
                    break;
            }
        }

        public override bool RemoveChild(Node child)
        {
            var i = Childs.IndexOf(child);
            if (i == -1)
            {
                Log.Warning($"{nameof(ContainerNode)}.{nameof(RemoveChild)} Could not find node {child}");
                return false;
            }

            RemoveChildAt(i);
            return true;
        }

        /*
        public void OnChildWantFocus(object sender, WantFocusEventArg args)
        {
            FocusNode = args.Source;
            //Log.Information($"{nameof(ContainerNode)}.{nameof(ChildWantFocus)}  calling parent: {Parent?.GetType()?.ToString()??"null"}");
            OnWantFocus(this, args);
        }
        */

        public override bool UpdateRect(RECT r)
        {
            base.UpdateRect(r);
            RecalcDeltaWithHeight();
            return UpdateChildRect(0, Childs.Count, out _);
        }

        public override void Dispose()
        {
            if (IsDisposed)
                return;

            Childs?.ForEach(c => c.Dispose());
            base.Dispose();
        }

        protected virtual void OnChildRequestRectChange(object sender, RequestRectChangeEventArg args)
        {
            var i = Childs.IndexOf(args.Requester);
//Log.Information($"################ Container.ChildRectChanged.{Direction.ToString()} (total: {Childs.Count}) Rect: {Rect}) Child {child.GetType().ToString()} ({child.ToString()})");
            if (i >= 0)
            {
                Childs[i].FixedRect = true;
            }

            RecalcDeltaWithHeight();
            if (!UpdateChildRect(0, Childs.Count, out RECT newRect))
            {
                base.UpdateRect(newRect);
                RecalcDeltaWithHeight();
                OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
            }
        }

        protected void RecalcDeltaWithHeight()
        {
            _width = Rect.Right - Rect.Left;
            _height = Rect.Bottom - Rect.Top;
            if (Childs.Count == 0)
            {
                _allocatableHeight = _height;
                _allocatableWidth = _width;
//Log.Information($"RecalcDeltaWidthHeight({Direction.ToString()}): _allocatable: {_allocatableWidth}/{_allocatableHeight}, zero Child count");
                return;
            }

            _allocatableHeight = _height / Childs.Count;
            _allocatableWidth = _width / Childs.Count;

            int w = 0, h = 0, c = 0;
            for(var i = 0; i  < Childs.Count; i++)
            {
                if (Childs[i].FixedRect == false)
                    continue;
                
                c++;
                w += (Childs[i].Rect.Right - Childs[i].Rect.Left);
                h += (Childs[i].Rect.Bottom - Childs[i].Rect.Top);
            }

            // All childs are marked as fixed but they do not cover the whole container
            if (c == Childs.Count && ((Direction == Direction.Horizontal && w < _width) || (Direction == Direction.Vertical && h < _height)))
            {
//Log.Information($"RecalcDeltaWidthHeight({Direction.ToString()}): ALL CHILDS ARE FIXED BUT THEY DO NOT COVER WHOLE PARENT {_width}/{_height} != {w}/{h}");
                w = h = c = 0;
                Childs.ForEach(c => c.FixedRect = false);
            }

//Log.Information($"RecalcDeltaWidthHeight({Direction.ToString()}): width/height: {_width}/{_height}, _allocatable: {_allocatableWidth}/{_allocatableHeight}, child count: {Childs.Count} (w: h and c: {w}, {h}, {c})");
            if (Direction == Direction.Horizontal && w > 0 && w < _width)
                _allocatableWidth = (_width - w) / Math.Max((Childs.Count - c), 1);

            if (Direction == Direction.Vertical && h > 0 && h < _height)
                _allocatableHeight = (_height - h) / Math.Max((Childs.Count - c), 1);
        }

        /// <summary>
        /// Will call UpdateRect on all childs that is not located in <c ref="_ignoreChildsOnUpdateRect" />
        /// </summary>
        /// <param name="from">start index in Childs</param>
        /// <param name="to">end index in Childs</param>
        protected bool UpdateChildRect(int from, int to, out RECT newRect)
        {
            var mustRestart = false;
            var safety = 0;
            var maxWidth = _width;
            var maxHeight = _height;
            Func<int, int, RECT> setRectToDefault = (l, t) => {
                return new RECT {
                    Left = l,
                    Top = t,
                    Right = l + (Direction == Direction.Horizontal ? _allocatableWidth : _width),
                    Bottom = t + (Direction == Direction.Vertical ? _allocatableHeight : _height)
                };
            };

            do
            {
                mustRestart = false;
                safety++;
                int left = Rect.Left, top = Rect.Top;
//Log.Information($"============ Container.{Direction.ToString()} (from: {from} to: {to} total: {Childs.Count}) Try {safety} (Rect: {Rect}) ==============");

                if (from > 0)
                {
                    if (Direction == Direction.Horizontal)
                        left = Childs[from-1].Rect.Right;
                    else
                        top = Childs[from-1].Rect.Bottom;
//Log.Information($"  >>>> Because we start in the middle then fetch Left/Top from previous child... left/top: {left}/{top}");
                }

                for(var i = from; i < to; i++)
                {
                    if (_ignoreChildsOnUpdateRect.Contains(i))
                        continue;

                    RECT r;
                    if (Childs[i].FixedRect)
                    {
                        r = Childs[i].Rect;
//Log.Information($"   Child[{i}] has ActualRect \"{Childs[i].Name}\": {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                        if (r.Left != left || r.Top != top)
                        {
                            r.Right -= (r.Left - left);
                            r.Bottom -= (r.Top - top);
                            r.Left = left;
                            r.Top = top;

                            // Make sure this child wont span over our boundry...
                            r.Right = Math.Min(r.Right, Rect.Right);
                            r.Bottom = Math.Min(r.Bottom, Rect.Bottom);
//Log.Information($"   >>>>1 Child[{i}] CHANGE ActualRect: {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                        }
                        else
                        if (r.Right > Rect.Right || r.Bottom > Rect.Bottom)
                        {
                            // Make sure this child wont span over our boundry...
                            r.Right = Math.Min(r.Right, Rect.Right);
                            r.Bottom = Math.Min(r.Bottom, Rect.Bottom);
//Log.Information($"   >>>>2 Child[{i}] CHANGE ActualRect: {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                        }

                        if (Direction == Direction.Horizontal)
                        {
                            if ((r.Bottom - r.Top) != _height)
                            {
                                r.Bottom = r.Top + _height;
//Log.Information($"   >>>>3 Child[{i}] CHANGE ActualRect: {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                                Childs[i].FixedRect = false;
                            }

                            if (Childs.Count > 1 && (r.Right - r.Left) == _width)
                            {
                                Childs[i].FixedRect = false;
//Log.Information($"   >>>>3.5 Child[{i}] Span whole parent but there are more childs, changing to \"normal\" rect!: {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                                r = setRectToDefault(left, top);
                            }
                        }

                        if (Direction == Direction.Vertical)
                        {
                            if ((r.Right - r.Left) != _width)
                            {
                                r.Right = r.Left + _width;
//Log.Information($"   >>>>4 Child[{i}] CHANGE ActualRect: {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                                Childs[i].FixedRect = false;
                            }

                            if (Childs.Count > 1 && (r.Bottom - r.Top) == _height)
                            {
                                Childs[i].FixedRect = false;
//Log.Information($"   >>>>5 Child[{i}] Span whole parent but there are more childs, changing to \"normal\" rect!: {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                                r = setRectToDefault(left, top);
                            }
                        }
                    }
                    else
                    {
                        r = setRectToDefault(left, top);
//Log.Information($"   Child[{i}] has NO actualRect \"{Childs[i].Name}\": setting rect to {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                    }

//Log.Information($".....Now updating rect for child {i}: {r}");
                    if (Childs[i].UpdateRect(r) == false)
                    {
//Log.Information($"Oh-no! Container.{Direction.ToString()} Child ({Childs[i].Name}) node says no on UpdateRect, wanted: {r} but actual: {Childs[i].Rect}");
                        // Child want to be bigger than what it got...
                        var r2 = Childs[i].Rect;
                        if (r2.Right > r.Right || r2.Bottom > r.Bottom)
                        {
                            Childs[i].FixedRect = true;
                            mustRestart = true;

                            RecalcDeltaWithHeight();

                            maxWidth = Math.Max(maxWidth, r2.Right - r2.Left);
                            maxHeight = Math.Max(maxHeight, r2.Bottom - r2.Top);
                        }
                    }

                    if (Direction == Direction.Horizontal)
                        left += (r.Right - r.Left);
                    else
                        top += (r.Bottom - r.Top);
                }

                if (safety > 2)
                {
                    Log.Warning($"{nameof(ContainerNode)}.{nameof(UpdateChildRect)} cannot resolve size. aborting after 2 tries...(rect: {Rect}, new width/height: {maxWidth}/{maxHeight})");
                    mustRestart = false;
                }
            } while (mustRestart);

            if (maxWidth != _width || maxHeight != _height)
            {
//Log.Information($"Oh-no2! Container want to change its rect: {Rect} (new width/height: {maxWidth}/{maxHeight})");
                RECT r = Rect;
                r.Right = r.Left + maxWidth;
                r.Bottom = r.Top + maxHeight;
                newRect = r;
//Log.Information($"==============Container.{Direction.ToString()} DONE(false) ====================");
                return false;
            }

//Log.Information($"==============Container.{Direction.ToString()} DONE(true) ====================");
            newRect = Rect;
            return true;
        }

        protected void TakeOverNode(Node childToAdd)
        {
            childToAdd.Parent = this;
            childToAdd.Depth = this.Depth + 1;
            childToAdd.StyleChanged += ChildNodeStyleChange;
            childToAdd.RequestRectChange += OnChildRequestRectChange;
            //childToAdd.WantFocus += OnChildWantFocus;
            Desktop.NodeAdded(childToAdd);
        }

        protected Node InsertChildAt(Node childToAdd, int? index = null)
        {
            TakeOverNode(childToAdd);

            if (index == null)
            {
                Childs.Add(childToAdd);
            }
            else
            {
                Childs.Insert(index.Value, childToAdd);
            }

            if (_isVisible)
            {
//Log.Information($"   Container showing newly inserted child");
                childToAdd.Show();
            }
            else
            {
//Log.Information($"   Container hiding newly inserted child ({childToAdd.GetType().ToString()})");
                childToAdd.Hide();
            }

            return childToAdd;
        }

        protected void RemoveChildAt(int index, bool callChildsDispose = true, bool doRecalcAfterDelete = true)
        {
            var n = Childs[index];
            var focusNode = Desktop.FocusTracker.MyLastFocusNode(this);
            var gotFocus = focusNode == Childs[index];

            if (gotFocus)
            {
                var newFocusNode = Childs[index+1 < Childs.Count ? index+1 : Math.Max(0, index-1)];
                Desktop.FocusTracker.ExplicitSetMyFocusNode(this, newFocusNode);
            }

            Childs.RemoveAt(index);
            n.StyleChanged -= ChildNodeStyleChange;
            n.RequestRectChange -= OnChildRequestRectChange;
            //n.WantFocus -= OnChildWantFocus;

            if (callChildsDispose)
            {
                n.Dispose();
            }
    
            // Bug #78, only remove parent if this object is the parent (if another node have taken over the ownership of the child node, then 
            //          node will be its parent)
            if (n.Parent == this)
                n.Parent = null;

            if (Childs.Count == 0)
            {
                Desktop.FocusTracker.ExplicitSetMyFocusNode(this, null);
                Parent?.RemoveChild(this);
            }
            else
            {
                if (doRecalcAfterDelete)
                {
                    RecalcDeltaWithHeight();
                    if (!UpdateChildRect(0, Childs.Count, out RECT newRect))
                    {
                        base.UpdateRect(newRect);
                        RecalcDeltaWithHeight();
                        OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
                    }
                }
            }
        }

        protected virtual RECT GetRect(int index)
        {
            return GetRect(index, Childs.Count, _allocatableWidth, _allocatableHeight, _width, _height);
        }

        protected virtual RECT GetRect(int index, int childCount)
        {
            return GetRect(index, childCount, _allocatableWidth, _allocatableHeight, _width, _height);
        }

        protected virtual RECT GetRect(int index, int childCount, int widthPerChild, int heightPerChild, int maxWidth, int maxHeight)
        {
            var start = index;
            var max = childCount;

            var r = new RECT
            {
                Left = Direction == Direction.Vertical ? Rect.Left : Rect.Left + (start * widthPerChild),
                Top = Direction == Direction.Horizontal ? Rect.Top : Rect.Top + (start * heightPerChild),
            };

            // Let it cover the whole screen if it is only one child
            if (max == 1)
            {
                r.Right = r.Left + maxWidth;
                r.Bottom = r.Top + maxHeight;
                return r;
            }

            r.Right = r.Left + (Direction == Direction.Horizontal ? widthPerChild : maxWidth);
            r.Bottom = r.Top + (Direction == Direction.Vertical ? heightPerChild : maxHeight);

            return r;
        }

        private void MoveChildLeft(Node child)
        {
            var i = Childs.IndexOf(child);
            var focusNode = Desktop.FocusTracker.MyLastFocusNode(this);
//Log.Information($"{nameof(ContainerNode)}.{nameof(MoveChildLeft)} called, i: {i}, Direction: {Direction.ToString()} Parent: {Parent.GetType().ToString()} ({Parent.WhatType.ToString()})");
            if (i == -1)
                return;
            
            if (i == 0 || Direction == Direction.Vertical)
            {
                _ignoreChildsOnUpdateRect.Add(i);
                if (Parent.TransferNode(this, child, TransferDirection.Left, focusNode == child))
                {
                    _ignoreChildsOnUpdateRect.Remove(i);
                    RemoveChildAt(i, false);
                }

                _ignoreChildsOnUpdateRect.Remove(i);
                return;
            }

            // from here on: i > 0

            // If the node to the left can have childs in it, then insert this into that node
            if (Childs[i-1].CanHaveChilds)
            {
                if (Childs[i - 1].TransferNode(null, child, TransferDirection.Left, focusNode == child))
                {
                    RemoveChildAt(i, false);
                    return;
                }
            }

            // No, then jump over that node and insert this before it
            Childs.RemoveAt(i);
            Childs.Insert(i-1, child);
            if (!UpdateChildRect(i-1, Childs.Count, out RECT newRect))
            {
                base.UpdateRect(newRect);
                RecalcDeltaWithHeight();
                OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
            }
        }

        private void MoveChildUp(Node child)
        {
            var i = Childs.IndexOf(child);
            var focusNode = Desktop.FocusTracker.MyLastFocusNode(this);
//Log.Information($"{nameof(ContainerNode)}.{nameof(MoveChildUp)} called, i: {i}, Direction: {Direction.ToString()} Parent: {Parent.GetType().ToString()} ({Parent.WhatType.ToString()})");
            if (i == -1)
                return;
            
            if (i == 0 || Direction == Direction.Horizontal)
            {
                _ignoreChildsOnUpdateRect.Add(i);
                if (Parent.TransferNode(this, child, TransferDirection.Up, focusNode == child))
                {
                    _ignoreChildsOnUpdateRect.Remove(i);
                    RemoveChildAt(i, false);
                }

                _ignoreChildsOnUpdateRect.Remove(i);
                return;
            }

            // from here on: i > 0

            // If the node to the up can have childs in it, then insert this into that node
            if (Childs[i-1].CanHaveChilds)
            {
                if (Childs[i - 1].TransferNode(null, child, TransferDirection.Up, focusNode == child))
                {
                    RemoveChildAt(i, false);
                    return;
                }
            }

            // No, then jump over that node and insert this before it
            Childs.RemoveAt(i);
            Childs.Insert(i-1, child);
            if (!UpdateChildRect(i-1, Childs.Count, out RECT newRect))
            {
                base.UpdateRect(newRect);
                RecalcDeltaWithHeight();
                OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
            }
        }
        
        private void MoveChildRight(Node child)
        {
            var i = Childs.IndexOf(child);
            var focusNode = Desktop.FocusTracker.MyLastFocusNode(this);
//Log.Information($"{nameof(ContainerNode)}.{nameof(MoveChildRight)} called, i: {i}, Direction: {Direction.ToString()} Parent: {Parent.GetType().ToString()} ({Parent.WhatType.ToString()})");
            if (i == -1)
                return;
            
            if (i == Childs.Count - 1 || Direction == Direction.Vertical)
            {
                _ignoreChildsOnUpdateRect.Add(i);
                if (Parent.TransferNode(this, child, TransferDirection.Right, focusNode == child))
                {
                    _ignoreChildsOnUpdateRect.Remove(i);
                    RemoveChildAt(i, false);
                }

                _ignoreChildsOnUpdateRect.Remove(i);
                return;
            }

            // From here on i at least 1 less than Childs.Count

            // If the node to the right can have childs in it, then insert this into that node
            if (Childs[i + 1].CanHaveChilds)
            {
                if (Childs[i + 1].TransferNode(null, child, TransferDirection.Right, focusNode == child))
                {
                    RemoveChildAt(i, false);
                    return;
                }
            }

            Childs.RemoveAt(i);
            Childs.Insert(i+1, child);
            if (!UpdateChildRect(i, Childs.Count, out RECT newRect))
            {
                base.UpdateRect(newRect);
                RecalcDeltaWithHeight();
                OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
            }
        }
        
        private void MoveChildDown(Node child)
        {
            var i = Childs.IndexOf(child);
            var focusNode = Desktop.FocusTracker.MyLastFocusNode(this);
//Log.Information($"{nameof(ContainerNode)}.{nameof(MoveChildDown)} called, i: {i}, Direction: {Direction.ToString()} Parent: {Parent.GetType().ToString()} ({Parent.WhatType.ToString()})");
            if (i == -1)
                return;
            
            if (i == Childs.Count - 1 || Direction == Direction.Horizontal)
            {
                _ignoreChildsOnUpdateRect.Add(i);
                if (Parent.TransferNode(this, child, TransferDirection.Down, focusNode == child))
                {
                    _ignoreChildsOnUpdateRect.Remove(i);
                    RemoveChildAt(i, false);
                }

                _ignoreChildsOnUpdateRect.Remove(i);
                return;
            }

            // From here on i at least 1 less than Childs.Count

            // If the node to the down can have childs in it, then insert this into that node
            if (Childs[i + 1].CanHaveChilds)
            {
                if (Childs[i + 1].TransferNode(null, child, TransferDirection.Down, focusNode == child))
                {
                    RemoveChildAt(i, false);
                    return;
                }
            }

            Childs.RemoveAt(i);
            Childs.Insert(i+1, child);
            if (!UpdateChildRect(i, Childs.Count, out RECT newRect))
            {
                base.UpdateRect(newRect);
                RecalcDeltaWithHeight();
                OnRequestRectChange(this, new RequestRectChangeEventArg(this, Rect, newRect));
            }
        }
        
        public override bool Equals(object obj)
        {
            var o = obj as ContainerNode;
            if (o == null)
                return false;
            return Equals(o);
        }

        public bool Equals([AllowNull] ContainerNode other)
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
                int hash = (int)2166136861;
                hash = (hash * 16777613) ^ Id.GetHashCode();
                return hash;
            }
        }
    }
}