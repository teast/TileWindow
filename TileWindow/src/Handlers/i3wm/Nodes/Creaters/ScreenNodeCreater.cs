using System;

namespace TileWindow.Handlers.I3wm.Nodes.Creaters
{
    /// <summary>
    /// Handler for creating an ScreenNode (to hide dependencies)
    /// </summary>
    public interface IScreenNodeCreater
    {
        ScreenNode Create(string name, RECT rect, Direction dir = Direction.Horizontal, params Node[] childs);
    }

    public class ScreenNodeCreater: IScreenNodeCreater
    {
        private readonly IFocusHandler focusHandler;
        private readonly IWindowEventHandler windowHandler;
        private readonly IWindowTracker windowTracker;
        private readonly IPInvokeHandler pinvokeHandler;
        private readonly IContainerNodeCreater containerCreater;

        public ScreenNodeCreater(IFocusHandler focusHandler, IWindowEventHandler windowHandler, IWindowTracker windowTracker, IPInvokeHandler pinvokeHandler, IContainerNodeCreater containerCreater)
        {
            this.focusHandler = focusHandler;
            this.windowHandler = windowHandler;
            this.windowTracker = windowTracker;
            this.pinvokeHandler = pinvokeHandler;
            this.containerCreater = containerCreater;
        }

        public virtual ScreenNode Create(string name, RECT rect, Direction dir = Direction.Horizontal, params Node[] childs)
        {
            var node = new ScreenNode(name, containerCreater, windowTracker, rect, dir);
            node.PostInit(childs);
            return node;
        }
    }
}