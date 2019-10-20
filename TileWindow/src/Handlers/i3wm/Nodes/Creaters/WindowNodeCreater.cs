using System;
using Serilog;

namespace TileWindow.Handlers.I3wm.Nodes.Creaters
{
    /// <summary>
    /// Handler for creating an WindowNode (to hide dependencies)
    /// </summary>
    public interface IWindowNodeCreater
    {
        WindowNode Create(RECT rect, IntPtr hWnd, Direction dir = Direction.Horizontal);
    }

    public class WindowNodeCreater: IWindowNodeCreater
    {
        private readonly IFocusHandler focusHandler;
        private readonly IWindowEventHandler windowHandler;
        private readonly IWindowTracker windowTracker;
        private readonly IPInvokeHandler pinvokeHandler;

        public WindowNodeCreater(IFocusHandler focusHandler, IWindowEventHandler windowHandler, IWindowTracker windowTracker, IPInvokeHandler pinvokeHandler)
        {
            this.focusHandler = focusHandler;
            this.windowHandler = windowHandler;
            this.windowTracker = windowTracker;
            this.pinvokeHandler = pinvokeHandler;
        }

        public virtual WindowNode Create(RECT rect, IntPtr hWnd, Direction dir = Direction.Horizontal)
        {
            try
            {
                var node = new WindowNode(
                    focusHandler, windowHandler,
                    windowTracker, pinvokeHandler,
                    rect, hWnd, dir);
                node.PostInit();
                return node;
            }
            catch(Exception ex)
            {
                Log.Fatal(ex, $"Could not create an window node for {hWnd}");
            }

            return null;
        }
    }
}