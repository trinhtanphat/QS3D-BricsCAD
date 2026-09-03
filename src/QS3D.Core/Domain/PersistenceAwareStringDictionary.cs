using System;
using System.Collections;
using System.Collections.Generic;

namespace QS3D.Core.Domain
{
    /// <summary>
    /// Case-insensitive string dictionary that requests owning persistence freshness
    /// before a real mutation is committed. The callback is deliberately invoked
    /// before the backing dictionary changes so a failing/overflowing owner Touch
    /// leaves the property store unchanged.
    /// </summary>
    internal sealed class PersistenceAwareStringDictionary : IDictionary<string, string>
    {
        private readonly Dictionary<string, string> _inner = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Action _mutationRequested;

        internal PersistenceAwareStringDictionary(Action mutationRequested)
        {
            _mutationRequested = mutationRequested ?? throw new ArgumentNullException(nameof(mutationRequested));
        }

        public string this[string key]
        {
            get => _inner[key];
            set
            {
                if (_inner.TryGetValue(key, out var existing) && string.Equals(existing, value, StringComparison.Ordinal))
                    return;

                _mutationRequested();
                _inner[key] = value;
            }
        }

        public ICollection<string> Keys => _inner.Keys;
        public ICollection<string> Values => _inner.Values;
        public int Count => _inner.Count;
        public bool IsReadOnly => false;

        public void Add(string key, string value)
        {
            // Preserve Dictionary duplicate/null-key failure semantics without
            // advancing persistence state for an operation that cannot mutate.
            if (_inner.ContainsKey(key))
                _inner.Add(key, value);

            _mutationRequested();
            _inner.Add(key, value);
        }

        public bool ContainsKey(string key) => _inner.ContainsKey(key);

        public bool Remove(string key)
        {
            if (!_inner.ContainsKey(key)) return false;
            _mutationRequested();
            return _inner.Remove(key);
        }

        public bool TryGetValue(string key, out string value) => _inner.TryGetValue(key, out value!);

        public void Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            if (_inner.Count == 0) return;
            _mutationRequested();
            _inner.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
            => ((ICollection<KeyValuePair<string, string>>)_inner).Contains(item);

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<string, string>>)_inner).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, string> item)
        {
            var collection = (ICollection<KeyValuePair<string, string>>)_inner;
            if (!collection.Contains(item)) return false;
            _mutationRequested();
            return collection.Remove(item);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
