using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using QS3D.Core.Audit;
using QS3D.Core.Mapping;
using QS3D.Core.Rules;

namespace QS3D.Core.Domain
{
    internal static class PersistedTextXml
    {
        internal static string Verify(string value, string parameterName, string label)
        {
            try
            {
                XmlConvert.VerifyXmlChars(value);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException(label + " contains characters that are invalid in XML.", parameterName, ex);
            }

            return value;
        }
    }

    public sealed class ZoneDefinition
    {
        private string _name;

        public ZoneDefinition(string id, string name)
        {
            Id = Require(id, nameof(id));
            _name = Require(name, nameof(name));
        }

        public string Id { get; }
        public string Name
        {
            get => _name;
            set
            {
                var next = Require(value, nameof(value));
                if (string.Equals(_name, next, StringComparison.Ordinal)) return;
                PersistenceMutationRequested?.Invoke();
                _name = next;
            }
        }

        internal event Action? PersistenceMutationRequested;

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var normalized = value.Trim();
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Value cannot contain control characters.", name);
            return PersistedTextXml.Verify(normalized, name, "Value");
        }
    }

    public sealed class FloorDefinition
    {
        private string _name;
        private double _elevationM;

        public FloorDefinition(string id, string name, double elevationM)
        {
            Id = Require(id, nameof(id));
            _name = Require(name, nameof(name));
            _elevationM = RequireElevation(elevationM);
        }

        public string Id { get; }
        public string Name
        {
            get => _name;
            set
            {
                var next = Require(value, nameof(value));
                if (string.Equals(_name, next, StringComparison.Ordinal)) return;
                PersistenceMutationRequested?.Invoke();
                _name = next;
            }
        }
        public double ElevationM
        {
            get => _elevationM;
            set
            {
                var next = RequireElevation(value);
                if (_elevationM.Equals(next)) return;
                PersistenceMutationRequested?.Invoke();
                _elevationM = next;
            }
        }

        internal event Action? PersistenceMutationRequested;

        internal void ApplyPersistedUpdate(string name, bool updateName, double elevationM, bool updateElevation)
        {
            var nextName = updateName ? Require(name, nameof(name)) : _name;
            var nextElevation = updateElevation ? RequireElevation(elevationM) : _elevationM;
            var nameChanged = updateName && !string.Equals(_name, nextName, StringComparison.Ordinal);
            var elevationChanged = updateElevation && !_elevationM.Equals(nextElevation);
            if (!nameChanged && !elevationChanged) return;

            PersistenceMutationRequested?.Invoke();
            if (nameChanged) _name = nextName;
            if (elevationChanged) _elevationM = nextElevation;
        }

        private static double RequireElevation(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Floor elevation must be finite.");
            return value == 0d ? 0d : value;
        }

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var normalized = value.Trim();
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Value cannot contain control characters.", name);
            return PersistedTextXml.Verify(normalized, name, "Value");
        }
    }

    public sealed class ProjectFamily : INotifyPropertyChanged
    {
        private const int MaxPropertyKeyLength = 120;
        private const int MaxPropertyValueLength = 1000;

        private sealed class PersistenceAwarePropertyDictionary : IDictionary<string, string>
        {
            private Dictionary<string, string> _inner = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private readonly Action _beforeMutation;

            internal PersistenceAwarePropertyDictionary(Action beforeMutation)
            {
                _beforeMutation = beforeMutation ?? throw new ArgumentNullException(nameof(beforeMutation));
            }

            public string this[string key]
            {
                get => _inner[key];
                set
                {
                    var canonicalKey = RequirePropertyKey(key);
                    var persistedValue = RequirePropertyValue(value);
                    if (_inner.TryGetValue(canonicalKey, out var current) && string.Equals(current, persistedValue, StringComparison.Ordinal)) return;
                    _beforeMutation();
                    _inner[canonicalKey] = persistedValue;
                }
            }

            public ICollection<string> Keys => _inner.Keys;
            public ICollection<string> Values => _inner.Values;
            public int Count => _inner.Count;
            public bool IsReadOnly => false;

            public void Add(string key, string value)
            {
                var canonicalKey = RequirePropertyKey(key);
                if (_inner.ContainsKey(canonicalKey))
                {
                    _inner.Add(canonicalKey, value);
                    return;
                }
                var persistedValue = RequirePropertyValue(value);
                _beforeMutation();
                _inner.Add(canonicalKey, persistedValue);
            }

            public void Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);

            public void Clear()
            {
                if (_inner.Count == 0) return;
                _beforeMutation();
                _inner.Clear();
            }

            public bool Contains(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)_inner).Contains(item);
            public bool ContainsKey(string key) => _inner.ContainsKey(key);
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, string>>)_inner).CopyTo(array, arrayIndex);
            public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _inner.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool Remove(string key)
            {
                if (!_inner.ContainsKey(key)) return false;
                _beforeMutation();
                return _inner.Remove(key);
            }

            public bool Remove(KeyValuePair<string, string> item)
            {
                var collection = (ICollection<KeyValuePair<string, string>>)_inner;
                if (!collection.Contains(item)) return false;
                _beforeMutation();
                return collection.Remove(item);
            }

            internal void ReplaceSnapshotState(Dictionary<string, string> replacement)
            {
                _inner = replacement ?? throw new ArgumentNullException(nameof(replacement));
            }

            public bool TryGetValue(string key, out string value) => _inner.TryGetValue(key, out value!);
        }

        private readonly PersistenceAwarePropertyDictionary _properties;
        private string _name;
        private ElementCategory _category;

        public ProjectFamily(string id, string name, ElementCategory category)
        {
            Id = RequireId(id);
            _name = RequireName(name);
            _category = RequireCategory(category);
            _properties = new PersistenceAwarePropertyDictionary(() => PersistenceMutationRequested?.Invoke());
            Properties = _properties;
        }

        public string Id { get; }
        public string Name
        {
            get => _name;
            set
            {
                var next = RequireName(value);
                if (string.Equals(_name, next, StringComparison.Ordinal)) return;
                PersistenceMutationRequested?.Invoke();
                _name = next;
                OnPropertyChanged();
            }
        }

        public ElementCategory Category
        {
            get => _category;
            set
            {
                var next = RequireCategory(value);
                if (_category == next) return;
                PersistenceMutationRequested?.Invoke();
                _category = next;
                OnPropertyChanged();
            }
        }

        public IDictionary<string, string> Properties { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        internal event Action? PersistenceMutationRequested;

        internal void RestoreSnapshotState(string name, ElementCategory category, IReadOnlyList<KeyValuePair<string, string>> properties)
        {
            var nextName = RequireName(name);
            var nextCategory = RequireCategory(category);
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            var nextProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in properties)
            {
                var canonicalKey = RequirePropertyKey(property.Key);
                var persistedValue = RequirePropertyValue(property.Value);
                nextProperties.Add(canonicalKey, persistedValue);
            }
            _name = nextName;
            _category = nextCategory;
            _properties.ReplaceSnapshotState(nextProperties);
        }

        private static string RequirePropertyKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Family property key is required.", nameof(value));
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal)) throw new ArgumentException("Family property key must be canonical without surrounding whitespace.", nameof(value));
            if (value.Any(char.IsControl)) throw new ArgumentException("Family property key cannot contain control characters.", nameof(value));
            if (value.Length > MaxPropertyKeyLength) throw new ArgumentException("Family property key must not exceed " + MaxPropertyKeyLength + " characters.", nameof(value));
            return PersistedTextXml.Verify(value, nameof(value), "Family property key");
        }

        private static string RequirePropertyValue(string? value)
        {
            var persistedValue = value ?? string.Empty;
            if (persistedValue.Length > MaxPropertyValueLength) throw new ArgumentException("Family property value must not exceed " + MaxPropertyValueLength + " characters.", nameof(value));
            return PersistedTextXml.Verify(persistedValue, nameof(value), "Family property value");
        }

        private static string RequireId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Family id is required.", nameof(value));
            var normalized = value.Trim();
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Family id cannot contain control characters.", nameof(value));
            return PersistedTextXml.Verify(normalized, nameof(value), "Family id");
        }
        private static string RequireName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Family name is required.", nameof(value));
            var normalized = value.Trim();
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Family name cannot contain control characters.", nameof(value));
            return PersistedTextXml.Verify(normalized, nameof(value), "Family name");
        }
        private static ElementCategory RequireCategory(ElementCategory value)
        {
            if (!Enum.IsDefined(typeof(ElementCategory), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Family category must be a defined ElementCategory.");
            return value;
        }
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    internal interface ICatalogMutationObserver<T> where T : class
    {
        void ValidateAdd(T item, int existingReferenceCount);
        void CommitAdd(T item);
        void ValidateReplace(T previous, T replacement, int previousReferenceCount, int replacementReferenceCount);
        void CommitReplace(T previous, T replacement);
        void ValidateRemove(T item, int referenceCount);
        void CommitRemove(T item);
        void ValidateClear(IReadOnlyList<T> items);
        void CommitClear();
    }

    internal sealed class AuditHistoryBudgetObserver : ICatalogMutationObserver<AuditEvent>
    {
        private sealed class ReferenceComparer : IEqualityComparer<AuditEvent>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();
            public bool Equals(AuditEvent? x, AuditEvent? y) => ReferenceEquals(x, y);
            public int GetHashCode(AuditEvent obj) => RuntimeHelpers.GetHashCode(obj);
        }

        private readonly Dictionary<AuditEvent, int> _referenceCounts = new Dictionary<AuditEvent, int>(ReferenceComparer.Instance);
        private int _storedCount;
        private long _storedTextCharacters;

        internal void ValidateStructuralCount(int actualCount)
        {
            if (actualCount != _storedCount)
                throw new InvalidOperationException("Audit history structural accounting is inconsistent with project history. Repair the existing audit history before modifying it.");
        }

        public void ValidateAdd(AuditEvent item, int existingReferenceCount)
        {
            ValidateReferenceCount(item, existingReferenceCount);
            if (_storedCount >= AuditTrail.MaxStoredEvents)
                throw new InvalidOperationException("Audit trail already contains 10000 events and cannot record another event.");
            RequireTextCapacity(AuditTrail.CountStoredTextCharacters(item));
        }

        public void CommitAdd(AuditEvent item)
        {
            _storedCount++;
            _storedTextCharacters += AuditTrail.CountStoredTextCharacters(item);
            if (_referenceCounts.TryGetValue(item, out var count)) _referenceCounts[item] = checked(count + 1);
            else _referenceCounts.Add(item, 1);
        }

        public void ValidateReplace(AuditEvent previous, AuditEvent replacement, int previousReferenceCount, int replacementReferenceCount)
        {
            ValidateReferenceCount(previous, previousReferenceCount);
            ValidateReferenceCount(replacement, replacementReferenceCount);
            var removed = AuditTrail.CountStoredTextCharacters(previous);
            var added = AuditTrail.CountStoredTextCharacters(replacement);
            var retained = _storedTextCharacters - removed;
            if (retained < 0L || added > AuditTrail.MaxStoredTextCharacters - retained)
                throw new InvalidOperationException("Audit trail text exceeds the supported aggregate text budget. Repair the existing audit history before modifying it.");
        }

        public void CommitReplace(AuditEvent previous, AuditEvent replacement)
        {
            _storedTextCharacters = _storedTextCharacters - AuditTrail.CountStoredTextCharacters(previous) + AuditTrail.CountStoredTextCharacters(replacement);
            DecrementReference(previous);
            IncrementReference(replacement);
        }

        public void ValidateRemove(AuditEvent item, int referenceCount)
        {
            ValidateReferenceCount(item, referenceCount);
            var removed = AuditTrail.CountStoredTextCharacters(item);
            if (_storedCount <= 0 || removed > _storedTextCharacters)
                throw new InvalidOperationException("Audit history accounting is inconsistent.");
        }

        public void CommitRemove(AuditEvent item)
        {
            _storedCount--;
            _storedTextCharacters -= AuditTrail.CountStoredTextCharacters(item);
            DecrementReference(item);
        }

        public void ValidateClear(IReadOnlyList<AuditEvent> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (items.Count != _storedCount)
                throw new InvalidOperationException("Audit history structural accounting is inconsistent with project history. Repair the existing audit history before modifying it.");

            var actualReferences = new Dictionary<AuditEvent, int>(ReferenceComparer.Instance);
            long actualTextCharacters = 0L;
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (item == null) throw new InvalidOperationException("Audit history contains a null entry.");
                actualTextCharacters = checked(actualTextCharacters + AuditTrail.CountStoredTextCharacters(item));
                if (actualReferences.TryGetValue(item, out var count)) actualReferences[item] = checked(count + 1);
                else actualReferences.Add(item, 1);
            }

            if (actualTextCharacters != _storedTextCharacters || actualReferences.Count != _referenceCounts.Count)
                throw new InvalidOperationException("Audit history reference accounting is inconsistent.");
            foreach (var pair in actualReferences)
            {
                if (!_referenceCounts.TryGetValue(pair.Key, out var storedCount) || storedCount != pair.Value)
                    throw new InvalidOperationException("Audit history reference accounting is inconsistent.");
            }
        }

        public void CommitClear()
        {
            _storedCount = 0;
            _storedTextCharacters = 0L;
            _referenceCounts.Clear();
        }

        internal void ValidateOwnedMutation(AuditEvent item, long perOccurrenceDelta)
        {
            if (!_referenceCounts.TryGetValue(item, out var count) || count <= 0)
                throw new InvalidOperationException("Owned audit event is missing from project history accounting.");
            var totalDelta = checked(perOccurrenceDelta * count);
            if (totalDelta > 0L) RequireTextCapacity(totalDelta);
            else if (totalDelta < 0L && _storedTextCharacters + totalDelta < 0L)
                throw new InvalidOperationException("Audit trail text accounting would become negative.");
        }

        internal void CommitOwnedMutation(AuditEvent item, long perOccurrenceDelta)
        {
            if (!_referenceCounts.TryGetValue(item, out var count) || count <= 0)
                throw new InvalidOperationException("Owned audit event is missing from project history accounting.");
            _storedTextCharacters = checked(_storedTextCharacters + checked(perOccurrenceDelta * count));
        }

        private void ValidateReferenceCount(AuditEvent item, int actualCount)
        {
            if (actualCount < 0) throw new InvalidOperationException("Audit history reference accounting is inconsistent.");
            if (actualCount == 0)
            {
                if (_referenceCounts.ContainsKey(item))
                    throw new InvalidOperationException("Audit history reference accounting is inconsistent.");
                return;
            }

            if (!_referenceCounts.TryGetValue(item, out var storedCount) || storedCount != actualCount)
                throw new InvalidOperationException("Audit history reference accounting is inconsistent.");
        }

        private void RequireTextCapacity(long additionalCharacters)
        {
            if (additionalCharacters < 0L || additionalCharacters > AuditTrail.MaxStoredTextCharacters - _storedTextCharacters)
                throw new InvalidOperationException("Audit trail text exceeds the supported aggregate text budget. Repair the existing audit history before modifying it.");
        }

        private void IncrementReference(AuditEvent item)
        {
            if (_referenceCounts.TryGetValue(item, out var count)) _referenceCounts[item] = checked(count + 1);
            else _referenceCounts.Add(item, 1);
        }

        private void DecrementReference(AuditEvent item)
        {
            if (!_referenceCounts.TryGetValue(item, out var count) || count <= 0)
                throw new InvalidOperationException("Audit history reference accounting is inconsistent.");
            if (count == 1) _referenceCounts.Remove(item);
            else _referenceCounts[item] = count - 1;
        }
    }

    internal sealed class CatalogOwnershipList<T> : IList<T> where T : class
    {
        private readonly List<T> _items = new List<T>();
        private readonly Action<T> _attach;
        private readonly Action<T> _detach;
        private readonly Action _beforeMutation;
        private readonly Action<T>? _validateCandidate;
        private readonly ICatalogMutationObserver<T>? _mutationObserver;

        internal CatalogOwnershipList(Action<T> attach, Action<T> detach, Action beforeMutation)
            : this(attach, detach, beforeMutation, null, null)
        {
        }

        internal CatalogOwnershipList(Action<T> attach, Action<T> detach, Action beforeMutation, Action<T>? validateCandidate)
            : this(attach, detach, beforeMutation, validateCandidate, null)
        {
        }

        internal CatalogOwnershipList(Action<T> attach, Action<T> detach, Action beforeMutation, Action<T>? validateCandidate, ICatalogMutationObserver<T>? mutationObserver)
        {
            _attach = attach ?? throw new ArgumentNullException(nameof(attach));
            _detach = detach ?? throw new ArgumentNullException(nameof(detach));
            _beforeMutation = beforeMutation ?? throw new ArgumentNullException(nameof(beforeMutation));
            _validateCandidate = validateCandidate;
            _mutationObserver = mutationObserver;
        }

        public T this[int index]
        {
            get => _items[index];
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                var previous = _items[index];
                if (previous == null) throw new InvalidOperationException("Catalog contains a null entry.");
                if (ReferenceEquals(previous, value)) return;
                _validateCandidate?.Invoke(value);
                var previousReferenceCount = CountReferences(previous);
                var replacementReferenceCount = CountReferences(value);
                _mutationObserver?.ValidateReplace(previous, value, previousReferenceCount, replacementReferenceCount);
                var previousWasLastReference = previousReferenceCount == 1;
                var valueAlreadyOwned = replacementReferenceCount > 0;
                _beforeMutation();
                _items[index] = value;
                if (previousWasLastReference) _detach(previous);
                if (!valueAlreadyOwned) _attach(value);
                _mutationObserver?.CommitReplace(previous, value);
            }
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public void Add(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _validateCandidate?.Invoke(item);
            var existingReferenceCount = CountReferences(item);
            _mutationObserver?.ValidateAdd(item, existingReferenceCount);
            var alreadyOwned = existingReferenceCount > 0;
            _beforeMutation();
            _items.Add(item);
            if (!alreadyOwned) _attach(item);
            _mutationObserver?.CommitAdd(item);
        }

        internal void AddRestoredPersistenceState(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var existingReferenceCount = CountReferences(item);
            _mutationObserver?.ValidateAdd(item, existingReferenceCount);
            var alreadyOwned = existingReferenceCount > 0;
            _items.Add(item);
            if (!alreadyOwned) _attach(item);
            _mutationObserver?.CommitAdd(item);
        }

        internal void ClearRestoredPersistenceState()
        {
            if (_items.Count == 0)
            {
                _mutationObserver?.CommitClear();
                return;
            }
            var owned = new List<T>();
            for (var index = 0; index < _items.Count; index++)
            {
                var item = _items[index];
                if (item == null) continue;
                var seen = false;
                for (var ownedIndex = 0; ownedIndex < owned.Count; ownedIndex++)
                {
                    if (!ReferenceEquals(owned[ownedIndex], item)) continue;
                    seen = true;
                    break;
                }
                if (!seen) owned.Add(item);
            }
            _items.Clear();
            for (var index = 0; index < owned.Count; index++) _detach(owned[index]);
            _mutationObserver?.CommitClear();
        }

        public void Clear()
        {
            if (_items.Count == 0) return;
            _mutationObserver?.ValidateClear(_items);
            _beforeMutation();
            var owned = new List<T>();
            for (var index = 0; index < _items.Count; index++)
            {
                var item = _items[index];
                var seen = false;
                for (var ownedIndex = 0; ownedIndex < owned.Count; ownedIndex++)
                {
                    if (!ReferenceEquals(owned[ownedIndex], item)) continue;
                    seen = true;
                    break;
                }
                if (!seen) owned.Add(item);
            }
            _items.Clear();
            for (var index = 0; index < owned.Count; index++)
            {
                var item = owned[index];
                if (item != null) _detach(item);
            }
            _mutationObserver?.CommitClear();
        }

        public bool Contains(T item) => _items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int IndexOf(T item) => _items.IndexOf(item);

        public void Insert(int index, T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (index < 0 || index > _items.Count) throw new ArgumentOutOfRangeException(nameof(index));
            _validateCandidate?.Invoke(item);
            var existingReferenceCount = CountReferences(item);
            _mutationObserver?.ValidateAdd(item, existingReferenceCount);
            var alreadyOwned = existingReferenceCount > 0;
            _beforeMutation();
            _items.Insert(index, item);
            if (!alreadyOwned) _attach(item);
            _mutationObserver?.CommitAdd(item);
        }

        public bool Remove(T item)
        {
            var index = _items.IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            var item = _items[index];
            if (item == null) throw new InvalidOperationException("Catalog contains a null entry.");
            var referenceCount = CountReferences(item);
            _mutationObserver?.ValidateRemove(item, referenceCount);
            var detach = referenceCount == 1;
            _beforeMutation();
            _items.RemoveAt(index);
            if (detach) _detach(item);
            _mutationObserver?.CommitRemove(item);
        }

        private bool ContainsReference(T item)
        {
            for (var i = 0; i < _items.Count; i++) if (ReferenceEquals(_items[i], item)) return true;
            return false;
        }

        private int CountReferences(T item)
        {
            var count = 0;
            for (var i = 0; i < _items.Count; i++) if (ReferenceEquals(_items[i], item)) count++;
            return count;
        }
    }

    internal sealed class StructuralRevisionList<T> : IList<T> where T : class
    {
        private readonly List<T> _items = new List<T>();
        private readonly Action _beforeMutation;

        internal StructuralRevisionList(Action beforeMutation)
        {
            _beforeMutation = beforeMutation ?? throw new ArgumentNullException(nameof(beforeMutation));
        }

        public T this[int index]
        {
            get => _items[index];
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                var previous = _items[index];
                if (ReferenceEquals(previous, value)) return;
                _beforeMutation();
                _items[index] = value;
            }
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public void Add(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _beforeMutation();
            _items.Add(item);
        }

        public void Clear()
        {
            if (_items.Count == 0) return;
            _beforeMutation();
            _items.Clear();
        }

        public bool Contains(T item) => _items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int IndexOf(T item) => _items.IndexOf(item);

        public void Insert(int index, T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (index < 0 || index > _items.Count) throw new ArgumentOutOfRangeException(nameof(index));
            _beforeMutation();
            _items.Insert(index, item);
        }

        public bool Remove(T item)
        {
            var index = _items.IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _items.Count) throw new ArgumentOutOfRangeException(nameof(index));
            _beforeMutation();
            _items.RemoveAt(index);
        }
    }

    public sealed class ProjectState
    {
        public const int CurrentSchemaVersion = 4;
        private readonly AuditHistoryBudgetObserver _auditHistoryBudget = new AuditHistoryBudgetObserver();
        private string _name;
        private string _drawingPath = string.Empty;
        private string _drawingFingerprint = string.Empty;
        private string _activeZoneId = string.Empty;
        private string _activeFloorId = string.Empty;
        private DateTime _updatedUtc = DateTime.UtcNow;
        private bool _restoringSnapshot;

        public ProjectState(string projectId, string name)
        {
            ProjectId = RequireProjectId(projectId);
            _name = string.IsNullOrWhiteSpace(name) ? "QS3D Project" : RequireProjectName(name);
            Zones = new CatalogOwnershipList<ZoneDefinition>(AttachZone, DetachZone, Touch);
            Floors = new CatalogOwnershipList<FloorDefinition>(AttachFloor, DetachFloor, Touch);
            Families = new CatalogOwnershipList<ProjectFamily>(AttachFamily, DetachFamily, Touch);
            Elements = new StructuralRevisionList<ProjectElement>(Touch);
            QuantityRules = new StructuralRevisionList<QuantityRule>(Touch);
            Metadata = new ProjectMetadataDictionary();
            MeasurementWorkItemMappings = new ProjectMeasurementWorkItemMappingCollection(this, Metadata);
            AuditEvents = new CatalogOwnershipList<AuditEvent>(AttachAuditEvent, DetachAuditEvent, ValidateAuditHistoryAndTouch, ValidateAuditEventCandidate, _auditHistoryBudget);
        }

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string ProjectId { get; }
        public string Name
        {
            get => _name;
            set
            {
                var next = RequireProjectName(value);
                if (string.Equals(_name, next, StringComparison.Ordinal)) return;
                var nextChangeVersion = checked(ChangeVersion + 1L);
                var nextUpdatedUtc = DateTime.UtcNow;
                _name = next;
                UpdatedUtc = nextUpdatedUtc;
                ChangeVersion = nextChangeVersion;
            }
        }
        public string DrawingPath
        {
            get => _drawingPath;
            set
            {
                var rawValue = value ?? string.Empty;
                if (rawValue.Any(char.IsControl)) throw new ArgumentException("Drawing path cannot contain control characters.", nameof(value));
                SetPersistedScalar(ref _drawingPath, PersistedTextXml.Verify(rawValue, nameof(value), "Drawing path"));
            }
        }
        public string DrawingFingerprint { get => _drawingFingerprint; set => SetCanonicalOptionalIdentity(ref _drawingFingerprint, value, "Drawing fingerprint"); }
        public string ActiveZoneId { get => _activeZoneId; set => SetActiveContextId(ref _activeZoneId, value); }
        public string ActiveFloorId { get => _activeFloorId; set => SetActiveContextId(ref _activeFloorId, value); }
        public DateTime UpdatedUtc { get => _updatedUtc; set => _updatedUtc = RequireUtcTimestamp(value, nameof(value)); }
        public long ChangeVersion { get; private set; }
        public IList<ZoneDefinition> Zones { get; }
        public IList<FloorDefinition> Floors { get; }
        public IList<ProjectFamily> Families { get; }
        public IList<ProjectElement> Elements { get; }
        public IList<QuantityRule> QuantityRules { get; }
        public ICollection<MeasurementWorkItemMapping> MeasurementWorkItemMappings { get; }
        public IList<AuditEvent> AuditEvents { get; }
        public IDictionary<string, string> Metadata { get; }

        public ProjectElement? FindElement(string id) => FindUnique(Elements, NormalizeLookupId(id), x => x.Id, "element");
        public ProjectFamily? FindFamily(string id) => FindUnique(Families, NormalizeLookupId(id), x => x.Id, "family");
        public FloorDefinition? FindFloor(string id) => FindUnique(Floors, NormalizeLookupId(id), x => x.Id, "floor");
        public ZoneDefinition? FindZone(string id) => FindUnique(Zones, NormalizeLookupId(id), x => x.Id, "zone");
        public QuantityRule? FindQuantityRule(string id) => FindUnique(QuantityRules, NormalizeLookupId(id), x => x.Id, "quantity rule");

        public void Touch()
        {
            if (_restoringSnapshot) return;
            var nextChangeVersion = checked(ChangeVersion + 1L);
            UpdatedUtc = DateTime.UtcNow;
            ChangeVersion = nextChangeVersion;
        }

        internal void ClearAuditEventsForRestore()
        {
            var auditEvents = AuditEvents as CatalogOwnershipList<AuditEvent>
                ?? throw new InvalidOperationException("Project audit collection does not expose the canonical ownership store.");
            auditEvents.ClearRestoredPersistenceState();
        }

        internal void RestoreAuditEvent(AuditEvent auditEvent)
        {
            if (auditEvent == null) throw new ArgumentNullException(nameof(auditEvent));
            var auditEvents = AuditEvents as CatalogOwnershipList<AuditEvent>
                ?? throw new InvalidOperationException("Project audit collection does not expose the canonical ownership store.");
            auditEvents.AddRestoredPersistenceState(auditEvent);
        }

        internal void RestorePersistenceState(DateTime updatedUtc, long changeVersion)
        {
            var restoredUpdatedUtc = RequireUtcTimestamp(updatedUtc, nameof(updatedUtc));
            if (changeVersion < 0L) throw new ArgumentOutOfRangeException(nameof(changeVersion), "Project change version cannot be negative.");
            _updatedUtc = restoredUpdatedUtc;
            ChangeVersion = changeVersion;
            _restoringSnapshot = false;
        }

        internal void RestoreSnapshotScalars(string name, string? drawingPath, string? drawingFingerprint, string? activeZoneId, string? activeFloorId)
        {
            var restoredName = RequireProjectName(name);
            var restoredDrawingPath = drawingPath ?? string.Empty;
            if (restoredDrawingPath.Any(char.IsControl)) throw new ArgumentException("Drawing path cannot contain control characters.", nameof(drawingPath));
            restoredDrawingPath = PersistedTextXml.Verify(restoredDrawingPath, nameof(drawingPath), "Drawing path");
            var restoredDrawingFingerprint = drawingFingerprint ?? string.Empty;
            if (restoredDrawingFingerprint.Length != 0 && !string.Equals(restoredDrawingFingerprint, restoredDrawingFingerprint.Trim(), StringComparison.Ordinal)) throw new ArgumentException("Drawing fingerprint must be empty or canonical without surrounding whitespace.", nameof(drawingFingerprint));
            if (restoredDrawingFingerprint.Any(char.IsControl)) throw new ArgumentException("Drawing fingerprint cannot contain control characters.", nameof(drawingFingerprint));
            restoredDrawingFingerprint = PersistedTextXml.Verify(restoredDrawingFingerprint, nameof(drawingFingerprint), "Drawing fingerprint");
            var restoredActiveZoneId = activeZoneId ?? string.Empty;
            if (restoredActiveZoneId.Length != 0 && !string.Equals(restoredActiveZoneId, restoredActiveZoneId.Trim(), StringComparison.Ordinal)) throw new ArgumentException("Active context id must be empty or canonical without surrounding whitespace.", nameof(activeZoneId));
            if (restoredActiveZoneId.Any(char.IsControl)) throw new ArgumentException("Active context id cannot contain control characters.", nameof(activeZoneId));
            restoredActiveZoneId = PersistedTextXml.Verify(restoredActiveZoneId, nameof(activeZoneId), "Active context id");
            var restoredActiveFloorId = activeFloorId ?? string.Empty;
            if (restoredActiveFloorId.Length != 0 && !string.Equals(restoredActiveFloorId, restoredActiveFloorId.Trim(), StringComparison.Ordinal)) throw new ArgumentException("Active context id must be empty or canonical without surrounding whitespace.", nameof(activeFloorId));
            if (restoredActiveFloorId.Any(char.IsControl)) throw new ArgumentException("Active context id cannot contain control characters.", nameof(activeFloorId));
            restoredActiveFloorId = PersistedTextXml.Verify(restoredActiveFloorId, nameof(activeFloorId), "Active context id");
            _restoringSnapshot = true;
            _name = restoredName;
            _drawingPath = restoredDrawingPath;
            _drawingFingerprint = restoredDrawingFingerprint;
            _activeZoneId = restoredActiveZoneId;
            _activeFloorId = restoredActiveFloorId;
        }

        private void AttachZone(ZoneDefinition zone) => zone.PersistenceMutationRequested += Touch;
        private void DetachZone(ZoneDefinition zone) => zone.PersistenceMutationRequested -= Touch;
        private void AttachFloor(FloorDefinition floor) => floor.PersistenceMutationRequested += Touch;
        private void DetachFloor(FloorDefinition floor) => floor.PersistenceMutationRequested -= Touch;
        private void AttachFamily(ProjectFamily family) => family.PersistenceMutationRequested += Touch;
        private void DetachFamily(ProjectFamily family) => family.PersistenceMutationRequested -= Touch;

        private void AttachAuditEvent(AuditEvent auditEvent)
        {
            auditEvent.PersistenceTextMutationValidating += ValidateAuditOwnedMutation;
            auditEvent.PersistenceMutationRequested += Touch;
            auditEvent.PersistenceTextMutationCommitted += _auditHistoryBudget.CommitOwnedMutation;
        }

        private void DetachAuditEvent(AuditEvent auditEvent)
        {
            auditEvent.PersistenceTextMutationValidating -= ValidateAuditOwnedMutation;
            auditEvent.PersistenceMutationRequested -= Touch;
            auditEvent.PersistenceTextMutationCommitted -= _auditHistoryBudget.CommitOwnedMutation;
        }

        private void ValidateAuditOwnedMutation(AuditEvent auditEvent, long textDelta)
        {
            _auditHistoryBudget.ValidateStructuralCount(AuditEvents.Count);
            _auditHistoryBudget.ValidateOwnedMutation(auditEvent, textDelta);
            if (_restoringSnapshot) return;
            _ = checked(ChangeVersion + 1L);
        }

        private void ValidateAuditHistoryAndTouch()
        {
            _auditHistoryBudget.ValidateStructuralCount(AuditEvents.Count);
            Touch();
        }

        private static void ValidateAuditEventCandidate(AuditEvent auditEvent)
        {
            AuditTrail.ValidateOwnedUtcMutation(auditEvent.Utc);
            AuditTrail.ValidateOwnedActionMutation(auditEvent.Action);
            AuditTrail.ValidateOwnedOptionalIdentityMutation(auditEvent.ElementId, nameof(AuditEvent.ElementId), "Audit element id");
            AuditTrail.ValidateOwnedXmlTextMutation(auditEvent.Detail, nameof(AuditEvent.Detail), "Audit detail");
            AuditTrail.ValidateOwnedXmlTextMutation(auditEvent.Actor, nameof(AuditEvent.Actor), "Audit actor");
            AuditTrail.ValidateOwnedOptionalIdentityMutation(auditEvent.CorrelationId, nameof(AuditEvent.CorrelationId), "Audit correlation id");
        }

        private void SetActiveContextId(ref string field, string? value)
        {
            var rawValue = value ?? string.Empty;
            if (rawValue.Length != 0 && !string.Equals(rawValue, rawValue.Trim(), StringComparison.Ordinal)) throw new ArgumentException("Active context id must be empty or canonical without surrounding whitespace.", nameof(value));
            if (rawValue.Any(char.IsControl)) throw new ArgumentException("Active context id cannot contain control characters.", nameof(value));
            SetPersistedScalar(ref field, PersistedTextXml.Verify(rawValue, nameof(value), "Active context id"));
        }

        private void SetCanonicalOptionalIdentity(ref string field, string? value, string label)
        {
            var rawValue = value ?? string.Empty;
            if (rawValue.Length != 0 && !string.Equals(rawValue, rawValue.Trim(), StringComparison.Ordinal)) throw new ArgumentException(label + " must be empty or canonical without surrounding whitespace.", nameof(value));
            if (rawValue.Any(char.IsControl)) throw new ArgumentException(label + " cannot contain control characters.", nameof(value));
            SetPersistedScalar(ref field, PersistedTextXml.Verify(rawValue, nameof(value), label));
        }

        private void SetPersistedScalar(ref string field, string value)
        {
            var normalizedValue = value ?? string.Empty;
            if (string.Equals(field, normalizedValue, StringComparison.Ordinal)) return;
            var nextChangeVersion = checked(ChangeVersion + 1L);
            var nextUpdatedUtc = DateTime.UtcNow;
            field = normalizedValue;
            UpdatedUtc = nextUpdatedUtc;
            ChangeVersion = nextChangeVersion;
        }

        private static DateTime RequireUtcTimestamp(DateTime value, string parameterName)
        {
            if (value.Kind != DateTimeKind.Utc) throw new ArgumentException("Project persistence timestamp must be UTC.", parameterName);
            return value;
        }

        private static string RequireProjectId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Project id is required.", nameof(value));
            var normalized = value.Trim();
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Project id cannot contain control characters.", nameof(value));
            return PersistedTextXml.Verify(normalized, nameof(value), "Project id");
        }

        private static string RequireProjectName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Project name is required.", nameof(value));
            var normalized = value.Trim();
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Project name cannot contain control characters.", nameof(value));
            return PersistedTextXml.Verify(normalized, nameof(value), "Project name");
        }

        private static string NormalizeLookupId(string id) => (id ?? string.Empty).Trim();

        private static T? FindUnique<T>(IEnumerable<T> items, string normalizedId, Func<T, string> idSelector, string label) where T : class
        {
            if (normalizedId.Length == 0) return null;
            T? match = null;
            foreach (var item in items)
            {
                if (item == null) throw new InvalidOperationException("Project contains a null " + label + " entry.");
                if (!string.Equals(idSelector(item), normalizedId, StringComparison.OrdinalIgnoreCase)) continue;
                if (match != null) throw new InvalidOperationException("Project contains duplicate " + label + " id: " + normalizedId);
                match = item;
            }
            return match;
        }
    }
}
