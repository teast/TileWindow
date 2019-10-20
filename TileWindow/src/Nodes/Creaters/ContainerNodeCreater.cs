using TileWindow.Handlers;
using TileWindow.Trackers;

namespace TileWindow.Nodes.Creaters
{
    /// <summary>
    /// Handler for creating an ContainerNode (to hide dependencies)
    /// </summary>
    public interface IContainerNodeCreater
    {
        ContainerNode Create(RECT rect, Direction dir = Direction.Horizontal, Node parent = null, params Node[] childs);
    }

    public class ContainerNodeCreater: IContainerNodeCreater
    {
        private readonly IFocusHandler focusHandler;
        private readonly IWindowEventHandler windowHandler;
        private readonly IWindowTracker windowTracker;
        private readonly IPInvokeHandler pinvokeHandler;

        public ContainerNodeCreater(IFocusHandler focusHandler, IWindowEventHandler windowHandler, IWindowTracker windowTracker, IPInvokeHandler pinvokeHandler)
        {
            this.focusHandler = focusHandler;
            this.windowHandler = windowHandler;
            this.windowTracker = windowTracker;
            this.pinvokeHandler = pinvokeHandler;
        }

        public virtual ContainerNode Create(RECT rect, Direction dir = Direction.Horizontal, Node parent = null, params Node[] childs)
        {
            var node = new ContainerNode(
                this,
                windowTracker,
                rect, dir, parent);
            node.PostInit(childs);
            return node;
        }
    }
}