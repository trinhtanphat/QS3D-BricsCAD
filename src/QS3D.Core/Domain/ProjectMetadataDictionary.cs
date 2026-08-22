using System;
using System.Collections;
using System.Collections.Generic;

namespace QS3D.Core.Domain
{
    internal sealed class ProjectMetadataDictionary : IDictionary<string, string>
    {
        private readonly Dictionary<string, string> _items = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string this[string key] { get => _items[key]; set => Set(key, value, false); }
        public ICollection<string> Keys => _items.Keys;
        public ICollection<string> Values => _items.Values;
        public int Count => _items.Count;
        public bool IsReadOnly => false;
        public void Add(string key, string value) => Set(key, value, true);
        public bool ContainsKey(string key) => _items.ContainsKey(key);
        public bool Remove(string key) => _items.Remove(key);
        public bool TryGetValue(string key, out string value)
        {
            string found;
            if (_items.TryGetValue(key, out found)) { value = found; return true; }
            value = string.Empty;
            return false;
        }
        public void Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);
        public void Clear() => _items.Clear();
        public bool Contains(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)_items).Contains(item);
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, string>>)_items).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)_items).Remove(item);
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void Set(string key, string value, bool addOnly)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (addOnly && _items.ContainsKey(key)) throw new ArgumentException("An item with the same key has already been added.", nameof(key));
            var normalizedValue = value ?? string.Empty;
            if (ProjectMeasurementWorkItemMappingCodec.IsReservedKey(key))
            {
                var next = new Dictionary<string, string>(_items, StringComparer.OrdinalIgnoreCase);
                next[key] = normalizedValue;
                ProjectMeasurementWorkItemMappingCodec.Read(next);
            }
            if (addOnly) _items.Add(key, normalizedValue); else _items[key] = normalizedValue;
        }
    }
}
