using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Forms;
using Serilog;
using TileWindow.Forms;

namespace TileWindow.Nodes.Renderers
{
    public class StackRenderer : IRenderer
    {
        private readonly int _captionHeight = 20;
        private readonly int _minHeight = 100;
        private readonly IPInvokeHandler pinvokeHandler;
        private readonly ISignalHandler signalHandler;
        private readonly uint signalRefresh;
        private readonly uint signalNewPosition;
        private readonly uint signalNewSize;
        private readonly uint signalShowHide;
        private bool _disposedCalled = false;
        private bool _changedDirection = false;

        private Thread _formThread = null;
        private AutoResetEvent _formShow = new AutoResetEvent(false);
        private AutoResetEvent _formStopped = new AutoResetEvent(false);
        private IntPtr _formHandle;
        private bool _formVisible;

        public int NumberOfCaptions { get; private set; }
        public RECT WorkArea { get; private set; }
        private RECT captionArea;
        private ContainerNode _owner;

        private Action startThread = null;

        public ContainerNode Owner 
        {
            get => _owner;
            private set => _owner = value;
        }

        public StackRenderer(IPInvokeHandler pinvokeHandler, ISignalHandler signalHandler)
        {
            this.pinvokeHandler = pinvokeHandler;
            this.signalHandler = signalHandler;
            this.signalRefresh = signalHandler.WMC_DISPLAYCHANGE;
            this.signalNewPosition = signalHandler.WMC_MOVE;
            this.signalNewSize = signalHandler.WMC_STYLECHANGED;
            this.signalShowHide = signalHandler.WMC_SHOW;
        }
        
        public void PreUpdate(ContainerNode owner, Collection<Node> childs)
        {
            if (_disposedCalled)
                return;

            if (Owner != null && Owner != owner)
            {
                DisconnectOwner();
            }

            Owner = owner;

            var Childs = childs;
            var left = Owner.Rect.Left;
            var right = Owner.Rect.Right;
            var bottom = Owner.Rect.Bottom;
            var top = Owner.Rect.Top + (_captionHeight * childs.Count);

            if (top < bottom - _minHeight)
            {
                NumberOfCaptions = Childs.Count;
            }
            else
            {
                NumberOfCaptions = Math.Max((Owner.Rect.Bottom - Owner.Rect.Top - _minHeight) / _captionHeight, 0);
                top = Owner.Rect.Top + (_captionHeight * NumberOfCaptions);
            }

            WorkArea = new RECT(left, top, right, bottom);
            captionArea = new RECT(left, Owner.Rect.Top, right, WorkArea.Top);

            if (_formThread == null)
            {
                startThread = () => {
                    _formVisible = false;
                    _formThread = new Thread(() => {
                        try
                        {
                            var _form = new FormStackCaption(captionArea, _captionHeight, ref _owner, signalHandler.WMC_SHOWNODE, signalRefresh, signalNewPosition, signalNewSize, signalShowHide);
                            _formHandle = _form.Handle;

                            _formShow.WaitOne();
                            Application.Run(_form);
                        }
                        catch(Exception ex)
                        {
                            Log.Fatal(ex, $"{nameof(StackRenderer)}.{nameof(PreUpdate)} (Thread) Unhandled exception");
                        }

                        _formStopped.Set();
                    });

                _formThread.Start();
                };

                startThread();
            }
            else
            {
                pinvokeHandler.SendMessage(_formHandle, signalNewPosition, new IntPtr(captionArea.Left), new IntPtr(captionArea.Top));
                pinvokeHandler.SendMessage(_formHandle, signalNewSize, new IntPtr(captionArea.Right - captionArea.Left), new IntPtr(captionArea.Bottom - captionArea.Top));
            }

            if (Owner.Direction != Direction.Vertical)
            {
                _changedDirection = true;
                Owner.ChangeDirection(Direction.Vertical);
            }

            ConnectOwner();
        }

        public (bool result, RECT newRect) Update(List<int> ignoreChildsWithIndex)
        {
            if (_disposedCalled)
                return (true, Owner.Rect);

            if (_formThread?.ThreadState != ThreadState.Running)
            {
                if (startThread != null)
                {
                    Log.Warning($"{nameof(StackRenderer)}.{nameof(Update)} thread is not in running state ({_formThread?.ThreadState}), restarting it");
                    startThread();
                }
                else
                {
                    Log.Warning($"{nameof(StackRenderer)}.{nameof(Update)} thread is not in running state ({_formThread?.ThreadState}) and startThread is not defined");
                }
            }

            if (_formVisible == false)
            {
                _formVisible = true;
                _formShow.Set();
            }

            if (_formHandle != IntPtr.Zero)
                pinvokeHandler.SendMessage(_formHandle, signalRefresh, IntPtr.Zero, IntPtr.Zero);

            var toHide = new List<Node>();
            // TODO: Add IsVisible property to Node and use that to minimize calls to show/hide
            //foreach(var child in Owner.Childs)
            for (var i = 0; i < Owner.Childs.Count; i++)
            {
                if (ignoreChildsWithIndex.Contains(i))
                    continue;
                
                // TODO: Ugly hack.. should probably create an "transferHandler" class and add an
                //       InTransfer state on nodes or something instead of this hack to know a node is
                //       getting transfered
                if (Owner.Childs[i].Parent != Owner)
                    continue;

                if (Owner.Childs[i] == Owner.MyFocusNode)
                {
                    // TODO: Handle false from UpdateRect
                    Owner.Childs[i].UpdateRect(WorkArea);
                    Owner.Childs[i].Show();
                }
                else
                    toHide.Add(Owner.Childs[i]);
            }

            toHide.ForEach(n => n.Hide());

            return (true, Owner.Rect);
        }

        public bool Show()
        {
            if (_disposedCalled)
                return true;

            if (_formHandle != IntPtr.Zero)
                pinvokeHandler.SendMessage(_formHandle, signalShowHide, new IntPtr(1), IntPtr.Zero);
            
            return true;
        }

        public bool Hide()
        {
            if (_disposedCalled)
                return true;

            if (_formHandle != IntPtr.Zero)
                pinvokeHandler.SendMessage(_formHandle, signalShowHide, new IntPtr(0), IntPtr.Zero);

            return true;
        }

        public void Dispose()
        {
            if (_disposedCalled)
                return;
            _disposedCalled = true;

            if (_formVisible)
            {
                pinvokeHandler.SendMessage(_formHandle, PInvoker.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                _formStopped.WaitOne(2000);
            }

            DisconnectOwner();

            if (Owner.Desktop.IsVisible)
            {
                foreach(var child in Owner.Childs)
                    child.Show();
            }
            else
            {
                foreach(var child in Owner.Childs)
                    child.Hide();
            }

            if (_changedDirection)
            {
                Owner.ChangeDirection(Direction.Horizontal);
            }
        }

        protected virtual void HandleMyFocusNodeChanged(object sender, EventArgs args)
        {
            if (_disposedCalled)
                return;

            Update(new List<int>());
        }

        protected virtual void ConnectOwner()
        {
            Owner.MyFocusNodeChanged += HandleMyFocusNodeChanged;
        }

        protected virtual void DisconnectOwner()
        {
            Owner.MyFocusNodeChanged -= HandleMyFocusNodeChanged;
        }
   }
}