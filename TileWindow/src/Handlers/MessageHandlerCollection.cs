using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TileWindow.Handlers
{
    public class MessageHandlerCollection : IList<IHandler>
    {
        private readonly IList<IHandler> _handlers;

        public MessageHandlerCollection(params IHandler[] handlers)
        {
            _handlers = handlers?.ToList() ?? new List<IHandler>();
        }

        public IHandler this[int index]
        {
            get => _handlers[index];
            set => _handlers[index] = value;
        }

        public int Count => _handlers.Count;

        public bool IsReadOnly => false;

        public void Add(IHandler item) => _handlers.Add(item);

        public void Clear() => _handlers.Clear();

        public bool Contains(IHandler item) => _handlers.Contains(item);

        public void CopyTo(IHandler[] array, int arrayIndex) => _handlers.CopyTo(array, arrayIndex);

        public IEnumerator<IHandler> GetEnumerator() => _handlers.GetEnumerator();

        public int IndexOf(IHandler item) => _handlers.IndexOf(item);

        public void Insert(int index, IHandler item) => _handlers.Insert(index, item);

        public bool Remove(IHandler item) => _handlers.Remove(item);

        public void RemoveAt(int index) => _handlers.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => _handlers.GetEnumerator();
    }
}