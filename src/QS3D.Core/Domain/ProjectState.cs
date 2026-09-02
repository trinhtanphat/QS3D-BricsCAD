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
        private string _name;
        private ElementCategory _category;

        public ProjectFamily(string id, string name, ElementCategory category)
        {
            Id = RequireId(id);
            _name = RequireName(name);
            _category = RequireCategory(category);
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
            if (!Enum.IsDefined(typeof(ElementCategory), value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Family category must be a defined ElementCategory.");
            return value;
        }
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    internal sealed class CatalogOwnershipList<T> : IList<T> where T : class
    {
        private readonly List<T> _items = new List<T>();
        private readonly Action<T> _attach;
        private readonly Action<T> _detach;

        internal CatalogOwnershipList(Action<T> attach, Action<T> detach)
        {
            _attach = attach ?? throw new ArgumentNullException(nameof(attach));
            _detach = detach ?? throw new ArgumentNullException(nameof(detach));
        }

        public T this[int index]
        {
            get => _items[index];
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                var previous = _items[index];
                if (ReferenceEquals(previous, value)) return;

                var previousWasLastReference = CountReferences(previous) == 1;
                var valueAlreadyOwned = ContainsReference(value);
                _items[index] = value;

                if (previousWasLastReference) _detach(previous);
                if (!valueAlreadyOwned) _attach(value);
            }
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public void Add(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var alreadyOwned = ContainsReference(item);
            _items.Add(item);
            if (!alreadyOwned) _attach(item);
        }

        public void Clear()
        {
            while (_items.Count > 0)
                RemoveAt(_items.Count - 1);
        }

        public bool Contains(T item) => _items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int IndexOf(T item) => _items.IndexOf(item);

        public void Insert(int index, T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var alreadyOwned = ContainsReference(item);
            _items.Insert(index, item);
            if (!alreadyOwned) _attach(item);
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
            var detach = CountReferences(item) == 1;
            _items.RemoveAt(index);
            if (detach) _detach(item);
        }

        private bool ContainsReference(T item)
        {
            for (var i = 0; i < _items.Count; i++)
                if (ReferenceEquals(_items[i], item)) return true;
            return false;
        }

        private int CountReferences(T item)
        {
            var count = 0;
            for (var i = 0; i < _items.Count; i++)
                if (ReferenceEquals(_items[i], item)) count++;
            return count;
        }
    }

    public sealed class ProjectState
    {
        public const int CurrentSchemaVersion = 4;
        private string _name;
        private string _drawingPath = string.Empty;
        private string _drawingFingerprint = string.Empty;
        private string _activeZoneId = string.Empty;
        private string _activeFloorId = string.Empty;
        private DateTime _updatedUtc = DateTime.UtcNow;

        public ProjectState(string projectId, string name)
        {
            ProjectId = RequireProjectId(projectId);
            _name = string.IsNullOrWhiteSpace(name) ? "QS3D Project" : RequireProjectName(name);
            Zones = new CatalogOwnershipList<ZoneDefinition>(AttachZone, DetachZone);
            Floors = new CatalogOwnershipList<FloorDefinition>(AttachFloor, DetachFloor);
            Families = new CatalogOwnershipList<ProjectFamily>(AttachFamily, DetachFamily);
            Elements = new List<ProjectElement>();
            QuantityRules = new List<QuantityRule>();
            Metadata = new ProjectMetadataDictionary();
            MeasurementWorkItemMappings = new ProjectMeasurementWorkItemMappingCollection(this, Metadata);
            AuditEvents = new List<AuditEvent>();
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
                if (rawValue.Any(char.IsControl))
                    throw new ArgumentException("Drawing path cannot contain control characters.", nameof(value));
                SetPersistedScalar(ref _drawingPath, PersistedTextXml.Verify(rawValue, nameof(value), "Drawing path"));
            }
        }
        public string DrawingFingerprint
        {
            get => _drawingFingerprint;
            set => SetCanonicalOptionalIdentity(ref _drawingFingerprint, value, "Drawing fingerprint");
        }
        public string ActiveZoneId
        {
            get => _activeZoneId;
            set => SetActiveContextId(ref _activeZoneId, value);
        }
        public string ActiveFloorId
        {
            get => _activeFloorId;
            set => SetActiveContextId(ref _activeFloorId, value);
        }
        public DateTime UpdatedUtc
        {
            get => _updatedUtc;
            set => _updatedUtc = RequireUtcTimestamp(value, nameof(value));
        }
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
            var nextChangeVersion = checked(ChangeVersion + 1L);
            UpdatedUtc = DateTime.UtcNow;
            ChangeVersion = nextChangeVersion;
        }

        internal void RestorePersistenceState(DateTime updatedUtc, long changeVersion)
        {
            var restoredUpdatedUtc = RequireUtcTimestamp(updatedUtc, nameof(updatedUtc));
            if (changeVersion < 0L)
                throw new ArgumentOutOfRangeException(nameof(changeVersion), "Project change version cannot be negative.");
            _updatedUtc = restoredUpdatedUtc;
            ChangeVersion = changeVersion;
        }

        internal void RestoreSnapshotScalars(
            string name,
            string? drawingPath,
            string? drawingFingerprint,
            string? activeZoneId,
            string? activeFloorId)
        {
            var restoredName = RequireProjectName(name);

            var restoredDrawingPath = drawingPath ?? string.Empty;
            if (restoredDrawingPath.Any(char.IsControl))
                throw new ArgumentException("Drawing path cannot contain control characters.", nameof(drawingPath));
            restoredDrawingPath = PersistedTextXml.Verify(restoredDrawingPath, nameof(drawingPath), "Drawing path");

            var restoredDrawingFingerprint = drawingFingerprint ?? string.Empty;
            if (restoredDrawingFingerprint.Length != 0 && !string.Equals(restoredDrawingFingerprint, restoredDrawingFingerprint.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Drawing fingerprint must be empty or canonical without surrounding whitespace.", nameof(drawingFingerprint));
            if (restoredDrawingFingerprint.Any(char.IsControl))
                throw new ArgumentException("Drawing fingerprint cannot contain control characters.", nameof(drawingFingerprint));
            restoredDrawingFingerprint = PersistedTextXml.Verify(restoredDrawingFingerprint, nameof(drawingFingerprint), "Drawing fingerprint");

            var restoredActiveZoneId = activeZoneId ?? string.Empty;
            if (restoredActiveZoneId.Length != 0 && !string.Equals(restoredActiveZoneId, restoredActiveZoneId.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Active context id must be empty or canonical without surrounding whitespace.", nameof(activeZoneId));
            if (restoredActiveZoneId.Any(char.IsControl))
                throw new ArgumentException("Active context id cannot contain control characters.", nameof(activeZoneId));
            restoredActiveZoneId = PersistedTextXml.Verify(restoredActiveZoneId, nameof(activeZoneId), "Active context id");

            var restoredActiveFloorId = activeFloorId ?? string.Empty;
            if (restoredActiveFloorId.Length != 0 && !string.Equals(restoredActiveFloorId, restoredActiveFloorId.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Active context id must be empty or canonical without surrounding whitespace.", nameof(activeFloorId));
            if (restoredActiveFloorId.Any(char.IsControl))
                throw new ArgumentException("Active context id cannot contain control characters.", nameof(activeFloorId));
            restoredActiveFloorId = PersistedTextXml.Verify(restoredActiveFloorId, nameof(activeFloorId), "Active context id");

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

        private void SetActiveContextId(ref string field, string? value)
        {
            var rawValue = value ?? string.Empty;
            if (rawValue.Length != 0 && !string.Equals(rawValue, rawValue.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Active context id must be empty or canonical without surrounding whitespace.", nameof(value));
            if (rawValue.Any(char.IsControl))
                throw new ArgumentException("Active context id cannot contain control characters.", nameof(value));
            SetPersistedScalar(ref field, PersistedTextXml.Verify(rawValue, nameof(value), "Active context id"));
        }

        private void SetCanonicalOptionalIdentity(ref string field, string? value, string label)
        {
            var rawValue = value ?? string.Empty;
            if (rawValue.Length != 0 && !string.Equals(rawValue, rawValue.Trim(), StringComparison.Ordinal))
                throw new ArgumentException(label + " must be empty or canonical without surrounding whitespace.", nameof(value));
            if (rawValue.Any(char.IsControl))
                throw new ArgumentException(label + " cannot contain control characters.", nameof(value));
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
            if (value.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Project persistence timestamp must be UTC.", parameterName);
            return value;
        }

        private static string RequireProjectId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Project id is required.", nameof(value));
            var normalized = value.Trim();
            if (normalized.Any(char.IsControl))
                throw new ArgumentException("Project id cannot contain control characters.", nameof(value));
            return PersistedTextXml.Verify(normalized, nameof(value), "Project id");
        }

        private static string RequireProjectName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Project name is required.", nameof(value));
            var normalized = value.Trim();
            if (normalized.Any(char.IsControl))
                throw new ArgumentException("Project name cannot contain control characters.", nameof(value));
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
