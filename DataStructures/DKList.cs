using System.Collections;
using System.Diagnostics;

namespace DataStructures
{
    public class DKList<T> : IList<T>, IReadOnlyList<T>
    {
        private const int _defaultCapacity = 4;
        private T[] _items;
        private int _size;

        public DKList()
        {
            _items = new T[4];
            _size = 0;
        }

        public DKList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException();

            if (capacity == 0)
                _items = Array.Empty<T>();
            else
                _items = new T[capacity];
        }

        public DKList(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException();

            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count == 0)
                    _items = Array.Empty<T>();
                else
                {
                    _items = new T[count];
                    c.CopyTo(_items, 0);
                    _size = count;
                }
            }
            else
            {
                _items = Array.Empty<T>();
                using IEnumerator<T> en = collection.GetEnumerator();
                while (en.MoveNext())
                    Add(en.Current);
            }
        }

        public int Capacity
        {
            get => _items.Length;
            set
            {
                if (value < _size)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        if (_size > 0)
                        {
                            Array.Copy(_items, newItems, _size);
                        }
                        _items = newItems;
                    }
                    else
                        _items = Array.Empty<T>();
                }
            }
        }

        public int Count => _size;

        public bool IsReadOnly => false;

        T IList<T>.this[int index]
        {
            get => this[index];
            set
            {
                try
                {
                    this[index] = value;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException(nameof(value));
                }
            }
        }

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_size)
                    throw new ArgumentOutOfRangeException();

                return _items[index];
            }
            set
            {
                if ((uint)index >= (uint)_size)
                    throw new ArgumentOutOfRangeException();

                _items[index] = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _size; i++)
                yield return _items[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int IndexOf(T item) => Array.IndexOf(_items, item, 0, _size);

        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)_size)
                throw new ArgumentOutOfRangeException();

            if (_size == _items.Length)
                Grow(_size + 1);

            if (index < _size)
                Array.Copy(_items, index, _items, index + 1, _size - index);

            _items[index] = item;
            _size++;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_size)
                throw new ArgumentOutOfRangeException();

            _size--;

            if (index < _size)
                Array.Copy(_items, index + 1, _items, index, _size - index);
        }

        public void Add(T item)
        {
            T[] array = _items;
            int size = _size;
            if ((uint)size < (uint)array.Length)
            {
                _size = size + 1;
                array[size] = item;
            }
            else
                AddWithResize(item);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException();

            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count > 0)
                {
                    if (_items.Length - _size < count)
                        Grow(checked(_size + count));

                    c.CopyTo(_items, _size);
                    _size += count;
                }
            }
            else
            {
                using IEnumerator<T> en = collection.GetEnumerator();
                while (en.MoveNext())
                    Add(en.Current);
            }
        }

        public void Clear()
        {
            Array.Clear(_items, 0, _size);
            _size = 0;
        }

        public bool Contains(T item) => _size != 0 && IndexOf(item) >= 0;

        public void CopyTo(T[] array, int arrayIndex)
        {
            if ((array != null) && (array.Rank != 1))
                throw new ArgumentException();

            try
            {
                Array.Copy(_items, 0, array!, arrayIndex, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException();
            }
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        public DKList<T> GetRange(int index, int count)
        {
            if (index < 0)
                throw new ArgumentException();

            if (count < 0)
                throw new ArgumentOutOfRangeException();

            if (_size - index < count)
                throw new ArgumentException();

            DKList<T> list = new(count);
            Array.Copy(_items, index, list._items, 0, count);
            list._size = count;
            return list;
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0)
                throw new IndexOutOfRangeException();

            if (count < 0)
                throw new ArgumentOutOfRangeException();

            if (_size - index < count)
                throw new ArgumentException();

            if (count > 0)
            {
                _size -= count;
                if (index < _size)
                    Array.Copy(_items, index + count, _items, index, _size - index);
            }
        }

        private void AddWithResize(T item)
        {
            Debug.Assert(_size == _items.Length);
            int size = _size;
            Grow(size + 1);
            _size = size + 1;
            _items[size] = item;
        }

        private void Grow(int capacity)
        {
            int newCapacity = _items.Length == 0 ? _defaultCapacity : 2 * _items.Length;

            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newCapacity > Array.MaxLength) newCapacity = Array.MaxLength;

            // If the computed capacity is still less than specified, set to the original argument.
            // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
            if (newCapacity < capacity) newCapacity = capacity;

            Capacity = newCapacity;
        }
    }
}
