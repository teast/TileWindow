using System;
using TileWindow.Dto;

namespace TileWindow.Handlers
{
    public interface IDragHandler: IHandler
    {
        event EventHandler<DragStartEvent> OnDragStart;
        event EventHandler<DragMoveEvent> OnDragMove;
        event EventHandler<DragEndEvent> OnDragEnd;
    }

    public class DragStartEvent
    {
        public IntPtr hWnd { get; }

        public DragStartEvent(PipeMessageEx msg)
        {
            hWnd = new IntPtr((long)msg.wParam);
        }
    }

    public class DragMoveEvent
    {
        public int X { get; }
        public int Y { get; }
        public IntPtr hWnd { get; }

        public DragMoveEvent(PipeMessageEx msg)
        {
            X = ToSignedDWord(msg.lParam);
            Y = ToSignedDWord(msg.lParam>>16);
            hWnd = new IntPtr((long)msg.wParam);
        }

        private static int ToSignedDWord(long i)
        {
            // Negative bit on dword: 0x80
            // 0x80 == 128 == 1000 0000
            // 0x7F == 0xFF - 0x80
            int v = (int)(i & 0xFFFF);
            if ((v & 0x8000) == 0x8000)
            {
                v = -(~v & 0x7FFF) - 1;
            }

            return v;
        }
    }

    public class DragEndEvent
    {
        public IntPtr hWnd { get; }

        public DragEndEvent(PipeMessageEx msg)
        {
            hWnd = new IntPtr((long)msg.wParam);
        }
    }

    public class DragHandler : IDragHandler
    {
        private readonly ISignalHandler signalHandler;

        public event EventHandler<DragStartEvent> OnDragStart;
        public event EventHandler<DragMoveEvent> OnDragMove;
        public event EventHandler<DragEndEvent> OnDragEnd;

        public DragHandler(ISignalHandler signalHandler)
        {
            this.signalHandler = signalHandler;
        }
        
        public void HandleMessage(PipeMessageEx msg)
        {
            if (msg.msg == signalHandler.WMC_ENTERMOVE)
            {
                RaiseOnDragStart(new DragStartEvent(msg));
            }
            else if (msg.msg == signalHandler.WMC_MOVE)
            {
                RaiseOnDragMove(new DragMoveEvent(msg));
            }
            else if (msg.msg == signalHandler.WMC_EXITMOVE)
            {
                RaiseOnDragEnd(new DragEndEvent(msg));
            }
        }

        public void Init()
        {
            // nothing to do here
        }

        public void ReadConfig(AppConfig config)
        {
            // nothing to do here
        }

        public void Quit()
        {
            // nothing to do here
        }

        public void Dispose()
        {
            // nothing to do here
        }

        public void DumpDebug()
        {
        }

        protected virtual void RaiseOnDragStart(DragStartEvent arg)
        {
            OnDragStart?.Invoke(this, arg);
        }

        protected virtual void RaiseOnDragMove(DragMoveEvent arg)
        {
            OnDragMove?.Invoke(this, arg);
        }

        protected virtual void RaiseOnDragEnd(DragEndEvent arg)
        {
            OnDragEnd?.Invoke(this, arg);
        }
    }
}
