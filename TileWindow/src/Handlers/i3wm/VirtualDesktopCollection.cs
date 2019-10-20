using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TileWindow.Handlers.I3wm
{
    public interface IVirtualDesktopCollection: IEnumerable<IVirtualDesktop>
    {
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
        public event EventHandler ActiveDesktopChanged;

        private IVirtualDesktop[] _desktops;
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
                    return;

                if (_activeDesktop == value)
                    return;
                
                _desktops[_activeDesktop].Hide();
                _activeDesktop = value;
                _desktops[_activeDesktop].Show();
                OnActiveDesktopChanged();
            }
        }

        public IVirtualDesktop this[int index]
        {
            get => _desktops[index];
            set => _desktops[index] = value;
        }

        public int Count => _desktops.Length;
        public void CopyTo(IVirtualDesktop[] array, int arrayIndex) => _desktops.CopyTo(array, arrayIndex);
        IEnumerator IEnumerable.GetEnumerator() => _desktops.GetEnumerator();

        protected virtual void OnActiveDesktopChanged()
        {
            ActiveDesktopChanged?.Invoke(this, null);
        }

        public IEnumerator<IVirtualDesktop> GetEnumerator() => _desktops.ToList().GetEnumerator();
    }
}