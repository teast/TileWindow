using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using TileWindow.Nodes.Creaters;
using TileWindow.Nodes.Renderers;
using TileWindow.Trackers;

namespace TileWindow.Nodes
{
    public class FixedContainerNode : ContainerNode, IEquatable<FixedContainerNode>
    {
        public override NodeTypes WhatType =>NodeTypes.Container;
        public override bool CanHaveChilds => true;

        public FixedContainerNode(IRenderer renderer, IContainerNodeCreater containerNodeCreator, IWindowTracker windowTracker, RECT rect, Direction direction = Direction.Horizontal, Node parent = null)
        : base(renderer, containerNodeCreator, windowTracker, rect, direction, parent)
        {
        }

        public override bool UpdateRect(RECT r)
        {
            if (r.Left == Rect.Left && r.Top == Rect.Top)
                return r.Equals(Rect);

            RecalcDeltaWithHeight();
            return UpdateChildRect(0, Childs.Count, out _);
        }

        public override Node AddWindow(IntPtr hWnd)
        {
            if (MyFocusNode != null)
            {
                if (MyFocusNode.Style == NodeStyle.Floating)
                {
//Log.Information($"{nameof(FixedContainerNode)}.{nameof(AddWindow)}, FocusNode is of Style Floating, lets try and find another Child");
                    foreach(var child in Childs)
                    {
                        if (child.CanHaveChilds && typeof(ContainerNode).IsInstanceOfType(child))
                        {
//Log.Information($"  >>> {nameof(FixedContainerNode)}.{nameof(AddWindow)}, Found a child (type: {child.GetType().ToString()} ({child.WhatType.ToString()}))");
                            return child.AddWindow(hWnd);
                        }
                    }

                    throw new Exception($"{nameof(FixedContainerNode)}.{nameof(AddWindow)} Unable to find a child node capable of storing nodes. Not possible to add hwnd: {hWnd}");
                }

//Log.Information($"{nameof(FixedContainerNode)}.{nameof(AddWindow)}, calling FocusNode (type: {FocusNode.GetType().ToString()} ({FocusNode.WhatType.ToString()})) with rect {FocusNode.Rect} (my rect: {Rect})");
                return MyFocusNode.AddWindow(hWnd);
            }

            throw new Exception($"{nameof(FixedContainerNode)}.{nameof(AddWindow)} was called with no FocusNode... what to do???");
        }

        public override bool AddNodes(params Node[] nodes)
        {
            //Log.Warning($"{nameof(FixedContainerNode)}.{nameof(AddNodes)} was called but this is a fixed node!");
//Log.Information($"FixedContainerNode.AddNodes going to try and add {nodes?.Length} nodes");
            foreach(var child in Childs)
            {
                if (child.CanHaveChilds)
                {
//Log.Information($"   >>> Will try and let child node take the nodes...");
                    child.AddNodes(nodes);
                    return true;
                }
            }

            return false;
        }

        public override bool TransferNode(Node child, Node nodeToTransfer, TransferDirection direction, bool nodeGotFocus)
        {
            var i = Childs.IndexOf(child);
//Log.Information($"FixedContainer.TransferNode dir: {direction.ToString()}, i: {i}");
            if (i == -1)
                return false;

            if ((Direction == Direction.Horizontal && (direction == TransferDirection.Up || direction == TransferDirection.Down)) ||
                (Direction == Direction.Vertical && (direction == TransferDirection.Left || direction == TransferDirection.Right)))
            {
                // It would seem strange if an node that is moved up ends up to the left/right on an horizontal node.. so lets just move it further up
                // It would seem strange if an node that is moved left ends up above/below on an vertical node.. so lets just move it further up
                return Parent?.TransferNode(this, nodeToTransfer, direction, nodeGotFocus) ?? false;
            }

            switch (direction)
            {
                case TransferDirection.Left:
                    if (i == 0)
                        return false;
                    
                    if (Childs[i-1].CanHaveChilds == false)
                        return false;
//Log.Information($"FixedContainer.TransferNode dir: Left, calling Child {Childs[i-1].GetType().ToString()}, with null as child param.");
                    return Childs[i-1].TransferNode(null, nodeToTransfer, direction, nodeGotFocus);
                case TransferDirection.Up:
                    if (i == 0)
                        return false;
                    
                    if (Childs[i-1].CanHaveChilds == false)
                        return false;
                    
//Log.Information($"FixedContainer.TransferNode dir: Up, calling Child {Childs[i-1].GetType().ToString()}, with null as child param.");
                    return Childs[i-1].TransferNode(null, nodeToTransfer, direction, nodeGotFocus);
                case TransferDirection.Right:
                    if (i == Childs.Count - 1)
                        return false;
                    
                    if (Childs[Childs.Count - 1].CanHaveChilds == false)
                        return false;
                    
//Log.Information($"FixedContainer.TransferNode dir: Right, calling Child {Childs[Childs.Count - 1].GetType().ToString()}, with null as child param.");
                    return Childs[Childs.Count - 1].TransferNode(null, nodeToTransfer, direction, nodeGotFocus);
                case TransferDirection.Down:
                    if (i == Childs.Count - 1)
                        return false;
                    
                    if (Childs[Childs.Count - 1].CanHaveChilds == false)
                        return false;
                    
//Log.Information($"FixedContainer.TransferNode dir: Down, calling Child {Childs[Childs.Count - 1].GetType().ToString()}, with null as child param.");
                    return Childs[Childs.Count - 1].TransferNode(null, nodeToTransfer, direction, nodeGotFocus);
                default:
                    Log.Error($"{nameof(FixedContainerNode)}.{nameof(TransferNode)} was called with unknown {nameof(TransferDirection)} ({direction.ToString()})");
                    break;
            }

            return false;
        }

        public override void ChildWantMove(Node child, TransferDirection direction)
        {
            switch (direction)
            {
                case TransferDirection.Left:
                    Log.Warning($"{nameof(FixedContainerNode)}.{nameof(ChildWantMove)} was called with Left, ({child.GetType()}) but Fixed container cannot change its childs nodes");
                    break;
                case TransferDirection.Up:
                    Log.Warning($"{nameof(FixedContainerNode)}.{nameof(ChildWantMove)} was called with up, ({child.GetType()}) but Fixed container cannot change its childs nodes");
                    break;
                case TransferDirection.Right:
                    Log.Warning($"{nameof(FixedContainerNode)}.{nameof(ChildWantMove)} was called with right, ({child.GetType()}) but Fixed container cannot change its childs nodes");
                    break;
                case TransferDirection.Down:
                    Log.Warning($"{nameof(FixedContainerNode)}.{nameof(ChildWantMove)} was called with down, ({child.GetType()}) but Fixed container cannot change its childs nodes");
                    break;
                default:
                    Log.Error($"{nameof(ContainerNode)}.{nameof(ChildWantMove)} was called with unknown {typeof(TransferDirection).ToString()} ({direction.ToString()})");
                    break;
            }
        }

        public override bool RemoveChild(Node child)
        {
            Log.Warning($"{nameof(FixedContainerNode)}.{nameof(RemoveChild)} was called, but Fixed container cannot change its childs nodes");
            return false;
        }

        public override void ChangeDirection(Direction dir)
        {
            Log.Warning($"{nameof(FixedContainerNode)}.{nameof(ChangeDirection)} was called, but Fixed container cannot change its childs nodes");
        }

        public override bool Equals(object obj)
        {
            var o = obj as FixedContainerNode;
            if (o == null)
                return false;
            return Equals(o);
        }

        public bool Equals([AllowNull] FixedContainerNode other)
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
                int hash = (int)2166136266;
                hash = (hash * 16777611) ^ Id.GetHashCode();
                return hash;
            }
        }
   }
}