using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace QS3D.Core.Domain
{
    internal sealed class ProjectMetadataDictionary : IDictionary<string, string>
    {
        private const string ProjectBrowserWorkspaceMetadataKey = "QS3D.ProjectBrowser.WorkspaceState";
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
            if (_items.Keys.Any(IsReservedKey)) ValidateReserved(_items);
            if (_items.Keys.Any(TracksSemanticDirtyState)) TouchProject();
            _items.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)_items).Contains(item);
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, string>>)_items).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, string> item)
        {
            var collection = (ICollection<KeyValuePair<string, string>>)_items;
            if (!collection.Contains(item)) return false;
            if (IsReservedKey(item.Key)) ValidateReserved(_items);
            if (TracksSemanticDirtyState(item.Key)) TouchProject();
            return collection.Remove(item);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal void AddOwned(string key, string value) => Set(key, value, true, false);
        internal void SetOwned(string key, string value) => Set(key, value, false, false);
        internal bool RemoveOwned(string key) => Remove(key, false);

        internal void ClearReservedOwned()
        {
            var keys = _items.Keys.Where(ProjectMeasurementWorkItemMappingCodec.IsReservedKey).ToArray();
            if (keys.Length == 0) return;
            ValidateReserved(_items);
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
            ValidateReserved(next);
            _items.Clear();
            foreach (var item in next) _items.Add(item.Key, item.Value);
        }

        private bool Remove(string key, bool touchMutation)
        {
            if (!_items.ContainsKey(key)) return false;
            if (IsReservedKey(key)) ValidateReserved(_items);
            if (touchMutation && TracksSemanticDirtyState(key)) TouchProject();
            return _items.Remove(key);
        }

        private void SetPublic(string key, string value, bool addOnly)
        {
            var canonicalKey = RequirePublicKey(key);
            var xmlValue = RequireXmlText(value ?? string.Empty, nameof(value), "Project metadata value");
            Set(canonicalKey, xmlValue, addOnly, true);
        }

        private void Set(string key, string value, bool addOnly, bool touchMutation)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (addOnly && _items.ContainsKey(key)) throw new ArgumentException("An item with the same key has already been added.", nameof(key));
            var normalizedValue = value ?? string.Empty;
            if (!addOnly && _items.TryGetValue(key, out var existing) && string.Equals(existing, normalizedValue, StringComparison.Ordinal)) return;

            if (IsReservedKey(key))
            {
                var next = new Dictionary<string, string>(_items, StringComparer.OrdinalIgnoreCase);
                next[key] = normalizedValue;
                ValidateReserved(next);
            }

            if (touchMutation && TracksSemanticDirtyState(key)) TouchProject();
            if (addOnly) _items.Add(key, normalizedValue); else _items[key] = normalizedValue;
        }

        private static bool IsReservedKey(string key)
        {
            return ProjectMeasurementWorkItemMappingCodec.IsReservedKey(key) || ProjectTbqWorkspaceCodec.IsReservedKey(key);
        }

        private static bool TracksSemanticDirtyState(string key)
        {
            return !string.Equals(key, ProjectBrowserWorkspaceMetadataKey, StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateReserved(IEnumerable<KeyValuePair<string, string>> metadata)
        {
            ProjectMeasurementWorkItemMappingCodec.Read(metadata);
            ProjectTbqWorkspaceCodec.Read(metadata);
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

        private void TouchProject()
        {
            var project = _project ?? throw new InvalidOperationException("Project metadata must be bound before metadata can be mutated.");
            project.Touch();
        }
    }
}
