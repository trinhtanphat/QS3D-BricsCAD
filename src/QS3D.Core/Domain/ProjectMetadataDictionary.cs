using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using QS3D.Core.Coordination;

namespace QS3D.Core.Domain
{
    internal sealed class ProjectMetadataDictionary : IDictionary<string, string>
    {
        private const int MaximumEntries = 10000;
        private const string ProjectBrowserWorkspaceMetadataKey = "QS3D.ProjectBrowser.WorkspaceState";
        private const string WallJunctionSnapPreviewPlanHashMetadataKey = "WallJunctionSnapPreviewPlanHash";
        private const string WallJunctionSnapPreviewSourceFingerprintMetadataKey = "WallJunctionSnapPreviewSourceFingerprint";
        private const string WallJunctionSnapPreviewCountMetadataKey = "WallJunctionSnapPreviewCount";
        private const string WallJunctionSnapPreviewUtcMetadataKey = "WallJunctionSnapPreviewUtc";
        private const string WallJunctionSnapPreviewProjectIdMetadataKey = "WallJunctionSnapPreviewProjectId";
        private const string WallJunctionSnapPreviewChangeVersionMetadataKey = "WallJunctionSnapPreviewChangeVersion";
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

        internal void EnsureCanAddOwned(string key) => EnsureCanSet(key, true);
        internal void EnsureCanSetOwned(string key) => EnsureCanSet(key, false);
        internal void EnsureCanApplyOwned(IEnumerable<string> removeKeys, IEnumerable<string> setKeys)
        {
            if (removeKeys == null) throw new ArgumentNullException(nameof(removeKeys));
            if (setKeys == null) throw new ArgumentNullException(nameof(setKeys));

            var finalKeys = new HashSet<string>(_items.Keys, StringComparer.OrdinalIgnoreCase);
            foreach (var key in removeKeys)
            {
                if (key == null) throw new ArgumentNullException(nameof(removeKeys), "Owned metadata removal contains a null key.");
                finalKeys.Remove(key);
            }
            foreach (var key in setKeys)
            {
                if (key == null) throw new ArgumentNullException(nameof(setKeys), "Owned metadata update contains a null key.");
                if (finalKeys.Contains(key)) continue;
                if (finalKeys.Count >= MaximumEntries) throw MetadataCountError();
                finalKeys.Add(key);
            }
        }
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
            var knownCount = RequireSupportedKnownPersistenceCount(values);
            var next = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var observedCount = 0;
            using (var enumerator = values.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownPersistenceCount(values, knownCount);
                    if (!enumerator.MoveNext()) break;
                    RequireStableKnownPersistenceCount(values, knownCount);

                    if (knownCount.HasValue && observedCount >= knownCount.Value)
                        throw MetadataTraversalCountMismatchError(knownCount.Value, observedCount + 1);
                    if (observedCount >= MaximumEntries)
                        throw MetadataCountError();

                    var item = enumerator.Current;
                    RequireStableKnownPersistenceCount(values, knownCount);
                    observedCount++;
                    if (item.Key == null) throw new ArgumentNullException(nameof(values), "Project metadata contains a null key.");
                    if (next.ContainsKey(item.Key)) throw new ArgumentException("Project metadata contains a duplicate key: " + item.Key + ".", nameof(values));
                    next.Add(item.Key, item.Value ?? string.Empty);
                }
            }
            RequireStableKnownPersistenceCount(values, knownCount);
            if (knownCount.HasValue && observedCount != knownCount.Value)
                throw MetadataTraversalCountMismatchError(knownCount.Value, observedCount);

            var finalKnownCount = RequireSupportedKnownPersistenceCount(values);
            if (knownCount.HasValue != finalKnownCount.HasValue ||
                (knownCount.HasValue && knownCount.Value != finalKnownCount!.Value))
                throw MetadataTraversalCountChangedError();

            ValidateReserved(next);
            _items.Clear();
            foreach (var item in next) _items.Add(item.Key, item.Value);
        }

        private bool Remove(string key, bool touchMutation)
        {
            if (!_items.ContainsKey(key)) return false;
            if (IsReservedKey(key)) ValidateReserved(_items);
            if (TracksSemanticDirtyState(key))
            {
                if (touchMutation) TouchProject();
            }
            return _items.Remove(key);
        }

        private void SetPublic(string key, string value, bool addOnly)
        {
            var canonicalKey = RequirePublicKey(key);
            var xmlValue = RequireXmlText(value ?? string.Empty, nameof(value), "Project metadata value");
            Set(canonicalKey, xmlValue, addOnly, true);
        }

        private void EnsureCanSet(string key, bool addOnly)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var exists = _items.ContainsKey(key);
            if (addOnly && exists) throw new ArgumentException("An item with the same key has already been added.", nameof(key));
            if (!exists && _items.Count >= MaximumEntries) throw MetadataCountError();
        }

        private void Set(string key, string value, bool addOnly, bool touchMutation)
        {
            EnsureCanSet(key, addOnly);
            var exists = _items.ContainsKey(key);
            var normalizedValue = value ?? string.Empty;
            if (!addOnly && exists && _items.TryGetValue(key, out var existing) && string.Equals(existing, normalizedValue, StringComparison.Ordinal)) return;

            if (IsReservedKey(key))
            {
                var next = new Dictionary<string, string>(_items, StringComparer.OrdinalIgnoreCase);
                next[key] = normalizedValue;
                ValidateReserved(next);
            }

            if (TracksSemanticDirtyState(key))
            {
                if (touchMutation) TouchProject();
            }
            if (addOnly) _items.Add(key, normalizedValue); else _items[key] = normalizedValue;
        }

        private static int? RequireSupportedKnownPersistenceCount(IEnumerable<KeyValuePair<string, string>> values)
        {
            var counts = new List<int>(3);
            if (values is ICollection<KeyValuePair<string, string>> collection)
                counts.Add(collection.Count);
            if (values is IReadOnlyCollection<KeyValuePair<string, string>> readOnlyCollection)
                counts.Add(readOnlyCollection.Count);
            if (values is ICollection nonGenericCollection)
                counts.Add(nonGenericCollection.Count);

            if (counts.Count == 0)
                return null;

            var knownCount = counts[0];
            var maximumCount = knownCount;
            var hasNegative = knownCount < 0;
            var hasConflict = false;
            for (var i = 1; i < counts.Count; i++)
            {
                if (counts[i] > maximumCount)
                    maximumCount = counts[i];
                if (counts[i] < 0)
                    hasNegative = true;
                if (counts[i] != knownCount)
                    hasConflict = true;
            }

            if (maximumCount > MaximumEntries)
                throw MetadataCountError();
            if (hasNegative)
                throw new InvalidOperationException("Project metadata persistence input exposes an invalid negative Count.");
            if (hasConflict)
                throw new InvalidOperationException("Project metadata persistence input exposes conflicting Count contracts.");
            return knownCount;
        }

        private static void RequireStableKnownPersistenceCount(
            IEnumerable<KeyValuePair<string, string>> values,
            int? expectedCount)
        {
            if (!expectedCount.HasValue) return;
            var observedCount = RequireSupportedKnownPersistenceCount(values);
            if (!observedCount.HasValue || observedCount.Value != expectedCount.Value)
                throw MetadataTraversalCountChangedError();
        }

        private static InvalidOperationException MetadataTraversalCountMismatchError(int expected, int observed)
        {
            return new InvalidOperationException(
                "Project metadata persistence input Count does not match traversal (expected " + expected + ", observed " + observed + ").");
        }

        private static InvalidOperationException MetadataTraversalCountChangedError()
        {
            return new InvalidOperationException("Project metadata persistence input Count changed during traversal.");
        }

        private static InvalidOperationException MetadataCountError()
        {
            return new InvalidOperationException("Project metadata supports at most " + MaximumEntries + " entries.");
        }

        private static bool IsReservedKey(string key)
        {
            return ProjectMeasurementWorkItemMappingCodec.IsReservedKey(key) ||
                   ProjectTbqWorkspaceCodec.IsReservedKey(key) ||
                   CoordinationIssuePersistenceCodec.IsReservedKey(key);
        }

        private static bool TracksSemanticDirtyState(string key)
        {
            if (string.Equals(key, ProjectBrowserWorkspaceMetadataKey, StringComparison.OrdinalIgnoreCase))
                return false;

            // Only the six production-owned Wall Snap preview keys are one workflow-state
            // batch. Public metadata that merely shares their prefix remains semantic state.
            return !IsWallJunctionSnapPreviewWorkflowKey(key);
        }

        private static bool IsWallJunctionSnapPreviewWorkflowKey(string key)
        {
            return string.Equals(key, WallJunctionSnapPreviewPlanHashMetadataKey, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, WallJunctionSnapPreviewSourceFingerprintMetadataKey, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, WallJunctionSnapPreviewCountMetadataKey, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, WallJunctionSnapPreviewUtcMetadataKey, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, WallJunctionSnapPreviewProjectIdMetadataKey, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, WallJunctionSnapPreviewChangeVersionMetadataKey, StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateReserved(IEnumerable<KeyValuePair<string, string>> metadata)
        {
            ProjectMeasurementWorkItemMappingCodec.Read(metadata);
            ProjectTbqWorkspaceCodec.Read(metadata);
            CoordinationIssuePersistenceCodec.Read(metadata);
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