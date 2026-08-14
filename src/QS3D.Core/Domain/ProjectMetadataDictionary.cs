using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace QS3D.Core.Domain
{
    internal sealed class ProjectMetadataDictionary : IDictionary<string, string>
    {
        private readonly Dictionary<string, string> _items = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private ProjectState? _project;

        public string this[string key] { get => _items[key]; set => SetPublic(key, value, false); }
        public ICollection<string> Keys => _items.Keys;
        public ICollection<string> Values => _items.Values;
        public int Count => _items.Count;
        public bool IsReadOnly => false;

        internal void BindProject(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (_project != null && !ReferenceEquals(_project, project))
                throw new InvalidOperationException("Project metadata is already bound to a different project.");
            _project = project;
        }

        public void Add(string key, string value) => SetPublic(key, value, true);
        public bool ContainsKey(string key) => _items.ContainsKey(key);
        public bool Remove(string key) => Remove(key, true);
        public bool TryGetValue(string key, out string value)
        {
            string found;
            if (_items.TryGetValue(key, out found)) { value = found; return true; }
            value = string.Empty;
            return false;
        }
        public void Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            if (_items.Count == 0) return;
            var hasReserved = _items.Keys.Any(ProjectMeasurementWorkItemMappingCodec.IsReservedKey);
            if (hasReserved)
            {
                ProjectMeasurementWorkItemMappingCodec.Read(_items);
                TouchReserved();
            }
            _items.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)_items).Contains(item);
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, string>>)_items).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, string> item)
        {
            var collection = (ICollection<KeyValuePair<string, string>>)_items;
            if (!collection.Contains(item)) return false;
            if (ProjectMeasurementWorkItemMappingCodec.IsReservedKey(item.Key))
            {
                ProjectMeasurementWorkItemMappingCodec.Read(_items);
                TouchReserved();
            }
            return collection.Remove(item);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal void AddOwned(string key, string value) => Set(key, value, true, false);
        internal bool RemoveOwned(string key) => Remove(key, false);

        internal void ClearReservedOwned()
        {
            var keys = _items.Keys.Where(ProjectMeasurementWorkItemMappingCodec.IsReservedKey).ToArray();
            if (keys.Length == 0) return;
            ProjectMeasurementWorkItemMappingCodec.Read(_items);
            foreach (var key in keys) _items.Remove(key);
        }

        internal void SetPersistenceValue(string key, string value) => Set(key, value, false, false);

        internal void ReplacePersistenceState(IEnumerable<KeyValuePair<string, string>> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var next = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in values)
            {
                if (item.Key == null) throw new ArgumentNullException(nameof(values), "Project metadata contains a null key.");
                if (next.ContainsKey(item.Key)) throw new ArgumentException("Project metadata contains a duplicate key: " + item.Key + ".", nameof(values));
                next.Add(item.Key, item.Value ?? string.Empty);
            }
            ProjectMeasurementWorkItemMappingCodec.Read(next);
            _items.Clear();
            foreach (var item in next) _items.Add(item.Key, item.Value);
        }

        private bool Remove(string key, bool touchReserved)
        {
            if (!_items.ContainsKey(key)) return false;
            if (ProjectMeasurementWorkItemMappingCodec.IsReservedKey(key))
            {
                ProjectMeasurementWorkItemMappingCodec.Read(_items);
                if (touchReserved) TouchReserved();
            }
            return _items.Remove(key);
        }

        private void SetPublic(string key, string value, bool addOnly)
        {
            var canonicalKey = RequirePublicKey(key);
            var xmlValue = RequireXmlText(value ?? string.Empty, nameof(value), "Project metadata value");
            Set(canonicalKey, xmlValue, addOnly, true);
        }

        private void Set(string key, string value, bool addOnly, bool touchReserved)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (addOnly && _items.ContainsKey(key)) throw new ArgumentException("An item with the same key has already been added.", nameof(key));
            var normalizedValue = value ?? string.Empty;
            var reserved = ProjectMeasurementWorkItemMappingCodec.IsReservedKey(key);
            if (reserved)
            {
                var next = new Dictionary<string, string>(_items, StringComparer.OrdinalIgnoreCase);
                next[key] = normalizedValue;
                ProjectMeasurementWorkItemMappingCodec.Read(next);
                if (!addOnly && _items.TryGetValue(key, out var existing) && string.Equals(existing, normalizedValue, StringComparison.Ordinal)) return;
                if (touchReserved) TouchReserved();
            }
            if (addOnly) _items.Add(key, normalizedValue); else _items[key] = normalizedValue;
        }

        private static string RequirePublicKey(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Project metadata key is required.", nameof(key));
            if (!string.Equals(key, key.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Project metadata key must not contain leading or trailing whitespace.", nameof(key));
            return RequireXmlText(key, nameof(key), "Project metadata key");
        }

        private static string RequireXmlText(string value, string parameterName, string label)
        {
            try
            {
                XmlConvert.VerifyXmlChars(value);
                return value;
            }
            catch (XmlException)
            {
                throw new ArgumentException(label + " contains characters that are invalid in XML.", parameterName);
            }
        }

        private void TouchReserved()
        {
            var project = _project ?? throw new InvalidOperationException("Project metadata must be bound before reserved mapping metadata can be mutated.");
            project.Touch();
        }
    }
}
