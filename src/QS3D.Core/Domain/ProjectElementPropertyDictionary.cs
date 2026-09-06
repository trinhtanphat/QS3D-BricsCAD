using System;
using System.Collections;
using System.Collections.Generic;

namespace QS3D.Core.Domain
{
    internal sealed class ProjectElementPropertyDictionary : IDictionary<string, string>
    {
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
            set => _owner.SetProperty(key, value);
        }

        public ICollection<string> Keys => _values.Keys;
        public ICollection<string> Values => _values.Values;
        public int Count => _values.Count;
        public bool IsReadOnly => false;

        public void Add(string key, string value) => _owner.AddProperty(key, value);
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
            var collection = (ICollection<KeyValuePair<string, string>>)_values;
            if (!collection.Contains(item)) return false;
            return _owner.RemoveProperty(item.Key);
        }

        public bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value!);

        internal void SetPersistenceValue(string key, string value)
        {
            _values.Add(key, value);
        }
    }
}
