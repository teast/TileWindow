using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Serilog;
using TileWindow.Nodes.Creaters;
using TileWindow.Nodes.Renderers;
using TileWindow.Trackers;

namespace TileWindow.Nodes
{
    public class ScreenNode : ContainerNode, IEquatable<ScreenNode>
    {
        private Node _fullscreenNode = null;

        public Node FullscreenNode => _fullscreenNode;

        public ScreenNode(string name, IRenderer renderer, IContainerNodeCreater containerNodeCreator, IWindowTracker windowTracker, RECT rect, Direction direction = Direction.Horizontal) :
        base(renderer, containerNodeCreator, windowTracker, rect, direction, null)
        {
            Name = name;
            FixedRect = true;
        }

        /// <summary>
        /// Will transfer all childs to destination <see cref="ScreenNode" />
        /// </summary>
        /// <param name="destination">Destination <see cref="ScreenNode" /> that all Childs should be transfered to</param>
        /// <param name="direction"><see cref="TransferDirection" /> of the transfer</param>
        public virtual void TransferAllChilds(ScreenNode destination, TransferDirection direction)
        {
            foreach (var child in Childs.ToList())
            {
                if (child.Style == NodeStyle.FullscreenOne)
                {
                    child.Style = NodeStyle.Tile;
                }

                if (destination.TransferNode(null, child, direction, child == FocusNode))
                {
                    DisconnectChild(child);
                }
                else
                {
                    Log.Error($"{nameof(ScreenNode)}.{nameof(TransferAllChilds)} Could not transfer child node \"{child}\"");
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

            if (_fullscreenNode != null)
            {
                _fullscreenNode.SetFocus(dir);
            }
            else
            {
                base.SetFocus(dir);
            }
        }

        public override bool TransferNode(Node child, Node nodeToTransfer, TransferDirection direction, bool nodeGotFocus)
        {
            var result = base.TransferNode(child, nodeToTransfer, direction, nodeGotFocus);

            if (result && _fullscreenNode != null)
            {
                _fullscreenNode.SetFocus(direction);
            }

            return result;
        }

        public override bool Equals(object obj)
        {
            var o = obj as ScreenNode;
            if (o == null)
            {
                return false;
            }

            return Equals(o);
        }

        public bool Equals([AllowNull] ScreenNode other)
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
                int hash = (int)2166136265;
                hash = (hash * 16777612) ^ Id.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => $"[{nameof(ScreenNode)}]";

        protected override void ChildNodeStyleChange(object sender, StyleChangedEventArg args)
        {
            if (args.Source.Style == NodeStyle.FullscreenOne)
            {
                if (_fullscreenNode != null && _fullscreenNode != args.Source && _fullscreenNode.Style == NodeStyle.FullscreenOne)
                {
                    _fullscreenNode.Style = NodeStyle.Tile;
                }

                args.Source.SetFullscreenRect(Rect);
                _fullscreenNode = args.Source;
            }
            else if (args.Source.Style == NodeStyle.Tile && args.Prev == NodeStyle.FullscreenOne)
            {
                _fullscreenNode = null;
                args.Source.UpdateRect(args.Source.Rect);
            }

            OnStyleChanged(this, args);
        }
    }
}