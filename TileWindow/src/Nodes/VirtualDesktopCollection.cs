using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace TileWindow.Nodes
{
    public class DesktopChangeEventArg
    {
        public int Index { get; }
        public bool Visible { get; }
        public bool Focus { get; }

        public DesktopChangeEventArg(int index, bool visible, bool focus)
        {
            Index = index;
            Visible = visible;
            Focus = focus;
        }
    }

    public interface IVirtualDesktopCollection : IEnumerable<IVirtualDesktop>
    {
        event EventHandler<DesktopChangeEventArg> DesktopChange;
        int Count { get; }
        IVirtualDesktop ActiveDesktop { get; }

        int Index { get; set; }

        IVirtualDesktop this[int index] { get; set; }

        event EventHandler ActiveDesktopChanged;
        void CopyTo(IVirtualDesktop[] array, int arrayIndex);
    }

    // Describes an desktop that can contains multiple real screens
    public class VirtualDesktopCollection : IVirtualDesktopCollection
    {
        public event EventHandler<DesktopChangeEventArg> DesktopChange;
        public event EventHandler ActiveDesktopChanged;

        private readonly IVirtualDesktop[] _desktops;
        private int _activeDesktop;

        public VirtualDesktopCollection(int nrOfDesktops)
        {
            _desktops = new IVirtualDesktop[nrOfDesktops];
            _activeDesktop = 0;
        }

        public IVirtualDesktop ActiveDesktop => _desktops[_activeDesktop];

        public int Index
        {
            get => _activeDesktop;
            set
            {
                if (value < 0 || value >= _desktops.Length)
                {
                    return;
                }

                if (_activeDesktop == value)
                {
                    return;
                }

                var oldDesktop = _activeDesktop;
                _desktops[_activeDesktop].Hide();
                _activeDesktop = value;
                _desktops[_activeDesktop].Show();
                RaiseActiveDesktopChanged();
                _desktops[oldDesktop].ReRaiseChildCountChange();
                _desktops[_activeDesktop].ReRaiseChildCountChange();
            }
        }

        public IVirtualDesktop this[int index]
        {
            get => _desktops[index];
            set
            {
                if (_desktops[index] != null)
                {
                    _desktops[index].ChildSetChange -= OnChildCountChange;
                }

                _desktops[index] = value;
                _desktops[index].ChildSetChange += OnChildCountChange;
                _desktops[index].ReRaiseChildCountChange();
            }
        }

        public int Count => _desktops.Length;
        public void CopyTo(IVirtualDesktop[] array, int arrayIndex) => _desktops.CopyTo(array, arrayIndex);
        IEnumerator IEnumerable.GetEnumerator() => _desktops.GetEnumerator();
        public IEnumerator<IVirtualDesktop> GetEnumerator() => _desktops.ToList().GetEnumerator();

        protected virtual void RaiseDesktopChange(DesktopChangeEventArg args)
        {
            DesktopChange?.Invoke(this, args);
        }

        protected virtual void RaiseActiveDesktopChanged()
        {
            ActiveDesktopChanged?.Invoke(this, null);
        }

        private void OnChildCountChange(object sender, ChildSetChangeArg e)
        {
            var o = sender as IVirtualDesktop;
            if (o == null)
            {
                return;
            }
            
            RaiseDesktopChange(new DesktopChangeEventArg(o.Index, e.Visible, ActiveDesktop.Index == o.Index));
        }
    }
}