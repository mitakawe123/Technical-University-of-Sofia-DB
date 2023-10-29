using System.Collections;

namespace DataStructures
{
    public class DKLinkedList<T> : ICollection<T>, IReadOnlyCollection<T>
    {
        internal DKLinkedListNode<T> head;
        private int count;

        public int Count => count;

        public bool IsReadOnly => false;

        internal DKLinkedListNode<T>? First => head;

        internal DKLinkedListNode<T>? Last => head?.prev;

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal class DKLinkedListNode<T>
    {
        internal DKLinkedList<T>? list;
        internal DKLinkedListNode<T>? next;
        internal DKLinkedListNode<T>? prev;
        internal T item;

        public DKLinkedListNode(T value) => this.item = value;

        internal DKLinkedListNode(DKLinkedList<T> list, T value)
        {
            this.list = list;
            this.item = value;
        }

        public DKLinkedList<T>? List => list;

        public DKLinkedListNode<T>? Next => next == null || next == list.head ? null : next;

        public DKLinkedListNode<T>? Previous => prev == null || this == list.head ? null : prev;

        public T Value
        {
            get => item;
            set => item = value;
        }
    }
}
