using System;
using System.Collections;
using System.Collections.Generic;

namespace QS3D.Core.Domain
{
    internal sealed class ProjectElementQuantityDictionary : IDictionary<string, double>
    {
        private readonly ProjectElement _owner;
        private readonly Dictionary<string, double> _values;

        internal ProjectElementQuantityDictionary(ProjectElement owner, Dictionary<string, double> values)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public double this[string key]
        {
            get => _values[CanonicalReadKey(key)];
            set => _owner.SetQuantity(key, value);
        }

        public ICollection<string> Keys => _values.Keys;
        public ICollection<double> Values => _values.Values;
        public int Count => _values.Count;
        public bool IsReadOnly => false;

        public void Add(string key, double value) => _owner.AddQuantity(key, value);
        public void Add(KeyValuePair<string, double> item) => Add(item.Key, item.Value);
        public void Clear() => _owner.ClearQuantities();

        public bool Contains(KeyValuePair<string, double> item)
        {
            var candidate = new KeyValuePair<string, double>(CanonicalReadKey(item.Key), item.Value);
            return ((ICollection<KeyValuePair<string, double>>)_values).Contains(candidate);
        }

        public bool ContainsKey(string key) => _values.ContainsKey(CanonicalReadKey(key));
        public void CopyTo(KeyValuePair<string, double>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, double>>)_values).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, double>> GetEnumerator() => _values.GetEnumerator();
        public bool Remove(string key) => _owner.RemoveQuantity(key);

        public bool Remove(KeyValuePair<string, double> item)
        {
            var key = item.Key ?? string.Empty;
            var candidate = key.Trim();
            if (!_values.TryGetValue(candidate, out var existing))
                return _owner.RemoveQuantity(key);

            // Reuse the owner boundary to validate/canonicalize the caller-supplied key without
            // mutating state: assigning the already-stored value is an intentional semantic no-op.
            _owner.SetQuantity(key, existing);
            if (!existing.Equals(item.Value)) return false;
            return _owner.RemoveQuantity(key);
        }

        public bool TryGetValue(string key, out double value) => _values.TryGetValue(CanonicalReadKey(key), out value);

        internal void SetPersistenceValue(string key, double value)
        {
            _values.Add(key, value);
        }

        private static string CanonicalReadKey(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return key.Trim();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
