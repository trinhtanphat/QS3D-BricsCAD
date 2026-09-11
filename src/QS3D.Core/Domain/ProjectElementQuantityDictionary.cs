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
            var candidate = CanonicalReadKey(key);
            if (_values.TryGetValue(candidate, out var existing))
            {
                if (!existing.Equals(item.Value)) return false;

                // Removal validates/canonicalizes the caller-supplied key at the owner boundary, but it
                // must not re-admit the already-persisted value. Otherwise corrupt legacy values such as
                // NaN/negative quantities cannot be removed through ICollection<KeyValuePair<,>>.
                return _owner.RemoveQuantity(key);
            }

            // Validate the caller through the semantic owner boundary before entering the persisted-state
            // repair path. A canonical lookup miss is expected here and therefore must remain a no-op.
            if (_owner.RemoveQuantity(key)) return true;

            string? persistedKey = null;
            double persistedValue = default;
            var matches = 0;
            foreach (var pair in _values)
            {
                if (!string.Equals(CanonicalReadKey(pair.Key), candidate, StringComparison.OrdinalIgnoreCase)) continue;
                persistedKey = pair.Key;
                persistedValue = pair.Value;
                matches++;
                if (matches > 1) return false;
            }

            if (matches != 1 || !persistedValue.Equals(item.Value)) return false;
            if (!_values.Remove(persistedKey!)) return false;

            // The raw key is deliberately removed below the semantic admission facade because it is
            // already malformed persisted state. The effective repair still participates in lifecycle.
            _owner.MarkDirty(ElementDirtyFlags.Quantity);
            return true;
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