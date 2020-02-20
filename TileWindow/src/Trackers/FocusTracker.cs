using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using TileWindow.Nodes;

namespace TileWindow.Trackers
{
    public class FocusChangedEventArg
    {
        public Node OldFocus { get; set; }
        public Node NewFocus { get; set; }

        public FocusChangedEventArg()
        {
        }

        public FocusChangedEventArg(Node oldFocus, Node newFocus)
        {
            OldFocus = oldFocus;
            NewFocus = newFocus;
        }
    }

    public interface IFocusTracker
    {
        /// <summary>
        /// Get triggered when focus node change
        /// </summary>
        event EventHandler<FocusChangedEventArg> FocusChanged;

        /// <summary>
        /// Just a debug parameter to see what desktop this specific focus tracker belongs to
        /// </summary>
        int DesktopIndex { get; set; }

        /// <summary>
        /// Start tracking <see cref="node" />
        /// </summary>
        /// <param name="node">The node to start tracking</param>
        /// <returns><c>True</c> if no problems</returns>
        bool Track(Node node);

        /// <summary>
        /// Stop tracking <see cref="node" />
        /// </summary>
        /// <param name="node">The node to stop tracking</param>
        /// <returns><c>True</c> if no problems</returns>
        bool Untrack(Node node);

        /// <summary>
        /// Get current <see cref="Node" /> that got focus
        /// </summary>
        /// <returns><c>null</c> if no focus</returns>
        Node FocusNode();

        /// <summary>
        /// Retrieve path from <see cref="node" /> to <see cref="FocusNode" />
        /// </summary>
        /// <param name="node">Node to find path for</param>
        /// <returns>Empty array if the path is not possible to calculate, else an array of nodes where last entry in the array is <see cref="FocusNode" /> and first should be an child <see cref="Node" /> of <see cref="node" /></returns>
        Node[] TraceToFocusNode(Node node);

        /// <summary>
        /// Retrieve last child node that had focus for <see cref="node" />
        /// </summary>
        /// <param name="node">node to retrieve last focus node for</param>
        /// <returns><c>null</c> if no child have been focused</returns>
        Node MyLastFocusNode(Node node);

        /// <summary>
        /// To explicit set child focus to a child
        /// </summary>
        /// <param name="node">node to set focus node on</param>
        /// <param name="newFocus">New focus node for <see cref="node" /></param>
        void ExplicitSetMyFocusNode(Node node, Node newFocus);

        /// <summary>
        /// Traverse from FocusNode and up to root and updates each nodes "MyFocusNode"
        /// </summary>
        void UpdateFocusTree();
    }

    public class FocusTracker: IFocusTracker
    {
        public virtual event EventHandler<FocusChangedEventArg> FocusChanged;

        private Dictionary<Node, Node> _tracker;
        private Node _focusNode;

        public int DesktopIndex { get; set; }

        public FocusTracker()
        {
            _tracker = new Dictionary<Node, Node>();
            _focusNode = null;
        }

        public virtual bool Track(Node node)
        {
            if (node == null)
                return false;
            
            if (ContainsKey(node))
                return true;

            node.Deleted += OnDeleted;
            node.WantFocus += OnWantFocus;
            _tracker.Add(node, null);
            return true;
        }

        public virtual bool Untrack(Node node)
        {
            if (node == null)
                return false;
            
            if (_focusNode == node)
                _focusNode = null;
            
            foreach(var key in _tracker.Keys.ToList())
                if(_tracker[key] == node)
                {
                    _tracker[key] = null;
                }

            if (ContainsKey(node) == false)
                return true;
            _tracker.Remove(node);
            node.Deleted -= OnDeleted;
            node.WantFocus -= OnWantFocus;
            return true;
        }

        public virtual Node FocusNode() => _focusNode;

        public virtual Node[] TraceToFocusNode(Node node)
        {
            var result = new List<Node>();
            if(node == null)
                return result.ToArray();
            if (_focusNode == null)
                return result.ToArray();
            
            var prev = _focusNode;
            do
            {
                result.Insert(0, prev);
                if (prev == node)
                    return result.ToArray();

                prev = prev.Parent;
            } while(prev != null);

            return new Node[0];
        }

        public virtual Node MyLastFocusNode(Node node)
        {
            if (node == null)
                return null;
                
            if (_tracker.TryGetValue(node, out Node child))
            {
                return child;
            }

            return null;
        }

        public virtual void ExplicitSetMyFocusNode(Node node, Node newFocus)
        {
            if (node == null)
                return;

            if (ContainsKey(node))
            {
                _tracker[node] = newFocus;
                node.RaiseMyFocusNodeChanged();
            }
        }

        public void UpdateFocusTree()
        {
            Node prev = null;
            var p = _focusNode;
            while(p != null)
            {
                if (ContainsKey(p))
                {
                    var diff = _tracker[p] != prev;
                    _tracker[p] = prev;
                    if (diff)
                        p.RaiseMyFocusNodeChanged();
                }
                else
                {
                    var tmp = _tracker.FirstOrDefault(pp => pp.Key.Id == p.Id);
                    Log.Warning($"{this}.{nameof(UpdateFocusTree)} a node in Focus tree is not tracked. focus node: {_focusNode}, not tracked: {p} (Same id: {tmp}) ({tmp.GetHashCode()} != {p.GetHashCode()}, {ReferenceEquals(tmp, p)})");
                }

                prev = p;
                p = p.Parent;
            }
        }

        public override string ToString() => $"[{nameof(FocusTracker)}({DesktopIndex}]";

        protected virtual void TriggerFocusChanged(Node newFocus)
        {
            if (newFocus == null)
                return;
            if (newFocus == _focusNode)
                return;

            var oldFocus = _focusNode;
            _focusNode = newFocus;
            UpdateFocusTree();
            FocusChanged?.Invoke(this, new FocusChangedEventArg(oldFocus, _focusNode));
        }

        protected virtual void OnWantFocus(object sender, WantFocusEventArg arg)
        {
            TriggerFocusChanged(sender as Node);
        }

        protected virtual void OnDeleted(object sender, EventArgs arg)
        {
            Log.Verbose($"{this} OnDeleted for {sender}");
            Untrack(sender as Node);
        }

        //private bool ContainsKey(Node node) => _tracker.Any(n => ReferenceEquals(n.Key, node));
        private bool ContainsKey(Node node) => _tracker.ContainsKey(node);
    }
}