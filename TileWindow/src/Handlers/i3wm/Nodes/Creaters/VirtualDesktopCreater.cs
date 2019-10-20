using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TileWindow.Handlers.I3wm.Trackers;

namespace TileWindow.Handlers.I3wm.Nodes.Creaters
{
    /// <summary>
    /// Handler for creating an VirtualDesktop (to hide dependencies)
    /// </summary>
    public interface IVirtualDesktopCreater
    {
        VirtualDesktop Create(int index, RECT rect, Direction dir = Direction.Horizontal, params ScreenNode[] childs);
    }

    public class VirtualDesktopCreater: IVirtualDesktopCreater
    {
        public IServiceProvider service { get; private set; }

        private readonly IFocusHandler focusHandler;
        private readonly IWindowEventHandler windowHandler;
        private readonly IWindowTracker windowTracker;
        private readonly IPInvokeHandler pinvokeHandler;
        private readonly IContainerNodeCreater containerCreater;
        private readonly IScreenNodeCreater screenCreater;

        public VirtualDesktopCreater(IServiceProvider service)// IFocusTracker focusTracker, IFocusHandler focusHandler, IWindowEventHandler windowHandler, IWindowTracker windowTracker, IPInvokeHandler pinvokeHandler, IContainerNodeCreater containerCreater)
        {
            this.service = service;
            this.focusHandler = service.GetRequiredService<IFocusHandler>();
            this.windowHandler = service.GetRequiredService<IWindowEventHandler>();
            this.windowTracker = service.GetRequiredService<IWindowTracker>();
            this.pinvokeHandler = service.GetRequiredService<IPInvokeHandler>();
            this.containerCreater = service.GetRequiredService<IContainerNodeCreater>();
            this.screenCreater = service.GetRequiredService<IScreenNodeCreater>();
        }

        public virtual VirtualDesktop Create(int index, RECT rect, Direction dir = Direction.Horizontal, params ScreenNode[] childs)
        {
            var desktop = new VirtualDesktop(index, screenCreater, service.GetRequiredService<IFocusTracker>(), containerCreater, windowTracker, rect, dir);
            desktop.PostInit(childs);
            return desktop;
        }
    }
}