namespace DataStructures
{
    using System.Collections;

    public class DKDictionary<K, V> : IDictionary<K, V> where K : notnull
    {
        private DKLinkedList<KeyValuePair<K, V>> _items = new();

        public V this[K key]
        {
            get
            {
                foreach (var item in _items)
                    if (item.Key.Equals(key))
                        return item.Value;

                throw new KeyNotFoundException("Key not found: " + key.ToString());
            }
            set
            {
                bool updated = false;
                for (var node = _items.Head; node != null; node = node.Next)
                {
                    if (node.Value.Key.Equals(key))
                    {
                        node.Value = new KeyValuePair<K, V>(key, value);
                        updated = true;
                        break;
                    }
                }

                if (!updated)
                    _items.AddLast(new KeyValuePair<K, V>(key, value));
            }
        }

        public ICollection<K> Keys
        {
            get
            {
                DKList<K> keys = new();
                foreach (var item in _items)
                    keys.Add(item.Key);
                return keys;
            }
        }

        public ICollection<V> Values
        {
            get
            {
                DKList<V> values = new();
                foreach (var item in _items)
                    values.Add(item.Value);
                return values;
            }
        }

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public void Add(K key, V value)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException("An item with the same key has already been added.");
            }

            _items.AddLast(new KeyValuePair<K, V>(key, value));
        }

        public void Add(KeyValuePair<K, V> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            foreach (var pair in _items)
            {
                if (pair.Key.Equals(item.Key) && pair.Value.Equals(item.Value))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsKey(K key)
        {
            foreach (var item in _items)
                if (item.Key.Equals(key))
                    return true;

            return false;
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("The number of elements in the source dictionary is greater than the available space from arrayIndex to the end of the destination array.");

            foreach (var item in _items)
                array[arrayIndex++] = item;
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _items.GetEnumerator();

        public bool Remove(K key)
        {
            for (var node = _items.Head; node != null; node = node.Next)
            {
                if (node.Value.Key.Equals(key))
                {
                    _items.Remove(node.Value);
                    return true;
                }
            }

            return false;
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            for (var node = _items.Head; node != null; node = node.Next)
            {
                if (node.Value.Key.Equals(item.Key) && node.Value.Value.Equals(item.Value))
                {
                    _items.Remove(node.Value);
                    return true;
                }
            }

            return false;
        }

        public bool TryGetValue(K key, out V value)
        {
            foreach (var item in _items)
            {
                if (item.Key.Equals(key))
                {
                    value = item.Value;
                    return true;
                }
            }

            value = default(V);
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
