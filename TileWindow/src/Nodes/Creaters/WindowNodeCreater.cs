using System;
using Serilog;
using TileWindow.Handlers;
using TileWindow.Trackers;

namespace TileWindow.Nodes.Creaters
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
        private readonly ISignalHandler signalHandler;
        private readonly IWindowEventHandler windowHandler;
        private readonly IWindowTracker windowTracker;
        private readonly IPInvokeHandler pinvokeHandler;

        public WindowNodeCreater(IFocusHandler focusHandler, ISignalHandler signalHandler, IWindowEventHandler windowHandler, IWindowTracker windowTracker, IPInvokeHandler pinvokeHandler)
        {
            this.focusHandler = focusHandler;
            this.signalHandler = signalHandler;
            this.windowHandler = windowHandler;
            this.windowTracker = windowTracker;
            this.pinvokeHandler = pinvokeHandler;
        }

        public virtual WindowNode Create(RECT rect, IntPtr hWnd, Direction dir = Direction.Horizontal)
        {
            try
            {
                var node = new WindowNode(
                    focusHandler, signalHandler, windowHandler,
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