using System.Collections;

namespace DataStructures
{
    public class DKDictionary<K, V> : IDictionary<K, V> where K : notnull
    {
        private const int INITIAL_SIZE = 16;
        private DKLinkedList<KeyValuePair<K, V>>[] values = new DKLinkedList<KeyValuePair<K, V>>[INITIAL_SIZE];

        public int Count { get; private set; }
        public bool IsReadOnly { get; }
        public int Capacity => this.values.Length;

        public ICollection<K> Keys { get; }
        public ICollection<V> Values { get; }

        public V this[K key]
        {
            get
            {
                if (TryGetValue(key, out V value))
                    return value;
                throw new KeyNotFoundException("The key was not found in the dictionary.");
            }
            set
            {
                int hash = HashKey(key);
                var collection = values[hash];
                for (var node = collection.Head; node != null; node = node.Next)
                {
                    if (node.Value.Key.Equals(key))
                    {
                        node.Value = new KeyValuePair<K, V>(key, value);
                        return;
                    }
                }

                collection.AddLast(new KeyValuePair<K, V>(key, value));
                Count++;
            }
        }

        public void Add(KeyValuePair<K, V> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            values = Array.Empty<DKLinkedList<KeyValuePair<K, V>>>();
            Count = 0;
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            int hash = HashKey(item.Key);
            if (values[hash] is null)
                return false;

            DKLinkedList<KeyValuePair<K, V>> collection = this.values[hash];
            return collection.Any(pair => pair.Key.Equals(item.Key) && pair.Value.Equals(item.Value));
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            foreach (DKLinkedList<KeyValuePair<K, V>> collection in values)
                if (collection is not null)
                    foreach (KeyValuePair<K, V> value in collection)
                        yield return value;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(K key, V value)
        {
            int hash = this.HashKey(key);
            if (values[hash] is null)
                this.values[hash] = new DKLinkedList<KeyValuePair<K, V>>();

            bool keyExistAlready = values[hash].Any(p => p.Key.Equals(key));
            if (keyExistAlready)
                throw new InvalidOperationException("Key already exists");

            this.values[hash].AddLast(new KeyValuePair<K, V>(key, value));
            Count++;

            if (this.Count >= this.Capacity * 0.75)
                this.ResizeAndReAddValues();
        }

        public bool ContainsKey(K key)
        {
            int hash = HashKey(key);
            if (values[hash] is null)
                return false;

            DKLinkedList<KeyValuePair<K, V>> collection = this.values[hash];
            return collection.Any(pair => pair.Key.Equals(key));
        }

        public bool Remove(K key)
        {
            int hash = HashKey(key);
            var collection = values[hash];
            if (collection is null)
                return false;

            for (var node = collection.Head; node != null; node = node.Next)
            {
                if (node.Value.Key.Equals(key))
                {
                    collection.Remove(node.Value);
                    Count--;
                    return true;
                }
            }

            return false;
        }

        public void RemoveAll(Func<K, V, bool> predicate)
        {
            for (int i = 0; i < this.Capacity; i++)
            {
                DKLinkedList<KeyValuePair<K, V>> collection = this.values[i];
                if (collection == null) continue;

                var node = collection.Head;
                while (node is not null)
                {
                    var nextNode = node.Next;
                    if (predicate(node.Value.Key, node.Value.Value))
                    {
                        collection.Remove(node.Value);
                        Count--;
                    }

                    node = nextNode;
                }

                if (collection.Count == 0)
                    this.values[i] = null;
            }
        }

        public bool TryGetValue(K key, out V value)
        {
            int hash = HashKey(key);
            var collection = values[hash];
            if (collection is not null)
            {
                foreach (var pair in collection)
                {
                    if (pair.Key.Equals(key))
                    {
                        value = pair.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        public V Find(K key)
        {
            int hash = this.HashKey(key);
            if (values[hash] is null)
                return default;

            DKLinkedList<KeyValuePair<K, V>> collection = values[hash];
            return collection.First(p => p.Key.Equals(key)).Value;
        }

        private int HashKey(K key) => Math.Abs(key.GetHashCode()) % this.Capacity;

        private void ResizeAndReAddValues()
        {
            DKLinkedList<KeyValuePair<K, V>>[] cachedValues = values;
            values = new DKLinkedList<KeyValuePair<K, V>>[2 * Capacity];

            Count = 0;
            foreach (DKLinkedList<KeyValuePair<K, V>> collection in cachedValues)
                if (collection is not null)
                    foreach (KeyValuePair<K, V> value in collection)
                        Add(value.Key, value.Value);
        }
    }
}
