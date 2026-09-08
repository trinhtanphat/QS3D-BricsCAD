using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace QS3D.Core.Domain
{
    internal sealed class ProjectElementPropertyDictionary : IDictionary<string, string>
    {
        private const int MaximumPropertyEntries = 10000;

        private readonly ProjectElement _owner;
        private readonly Dictionary<string, string> _values;

        internal ProjectElementPropertyDictionary(ProjectElement owner, Dictionary<string, string> values)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public string this[string key]
        {
            get => _values[key];
            set
            {
                var canonical = ValidateMutationInput(key, value);
                RequireCapacityForNewKey(canonical);
                _owner.SetProperty(key, value);
            }
        }

        public ICollection<string> Keys => _values.Keys;
        public ICollection<string> Values => _values.Values;
        public int Count => _values.Count;
        public bool IsReadOnly => false;

        public void Add(string key, string value)
        {
            var canonical = ValidateMutationInput(key, value);
            RequireCapacityForNewKey(canonical);
            _owner.AddProperty(key, value);
        }

        public void Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);

        public void Clear() => _owner.ClearProperties();

        public bool Contains(KeyValuePair<string, string> item) =>
            ((ICollection<KeyValuePair<string, string>>)_values).Contains(item);

        public bool ContainsKey(string key) => _values.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) =>
            ((ICollection<KeyValuePair<string, string>>)_values).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Remove(string key) => _owner.RemoveProperty(key);

        public bool Remove(KeyValuePair<string, string> item)
        {
            var canonical = ValidateMutationKey(item.Key);
            if (!_values.TryGetValue(canonical, out var existing) ||
                !string.Equals(existing, item.Value, StringComparison.Ordinal))
            {
                return false;
            }

            return _owner.RemoveProperty(canonical);
        }

        public bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value!);

        internal void SetPersistenceValue(string key, string value)
        {
            _values.Add(key, value);
        }

        internal bool RemovePersistenceValue(string key)
        {
            return _values.Remove(key);
        }

        private static string ValidateMutationInput(string key, string value)
        {
            var canonical = ValidateMutationKey(key);

            try
            {
                XmlConvert.VerifyXmlChars(value ?? string.Empty);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException("Property value contains characters that are invalid in XML.", nameof(value), ex);
            }

            return canonical;
        }

        private static string ValidateMutationKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Property name is required.", "name");
            if (key.Any(char.IsControl))
                throw new ArgumentException("Property name cannot contain control characters.", "name");

            var canonical = key.Trim();
            try
            {
                XmlConvert.VerifyXmlChars(canonical);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException("Property name contains characters that are invalid in XML.", "name", ex);
            }

            return canonical;
        }

        private void RequireCapacityForNewKey(string canonicalKey)
        {
            if (_values.ContainsKey(canonicalKey)) return;
            if (_values.Count >= MaximumPropertyEntries)
            {
                throw new InvalidOperationException(
                    "Property collection exceeds the maximum supported cardinality of " + MaximumPropertyEntries + ".");
            }
        }
    }
}
