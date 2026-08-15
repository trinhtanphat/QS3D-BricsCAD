using System;
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
            set => _name = Require(value, nameof(value));
        }

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
            ElevationM = elevationM;
        }

        public string Id { get; }
        public string Name
        {
            get => _name;
            set => _name = Require(value, nameof(value));
        }
        public double ElevationM
        {
            get => _elevationM;
            set
            {
                if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "Floor elevation must be finite.");
                _elevationM = value == 0d ? 0d : value;
            }
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
                _category = next;
                OnPropertyChanged();
            }
        }

        public IDictionary<string, string> Properties { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
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
            Zones = new List<ZoneDefinition>();
            Floors = new List<FloorDefinition>();
            Families = new List<ProjectFamily>();
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
            if (restoredDrawingFingerprint.Any(char.IsControl))
                throw new ArgumentException("Drawing fingerprint cannot contain control characters.", nameof(drawingFingerprint));
            restoredDrawingFingerprint = PersistedTextXml.Verify(restoredDrawingFingerprint.Trim(), nameof(drawingFingerprint), "Drawing fingerprint");

            var restoredActiveZoneId = (activeZoneId ?? string.Empty).Trim();
            if (restoredActiveZoneId.Any(char.IsControl))
                throw new ArgumentException("Active context id cannot contain control characters.", nameof(activeZoneId));
            restoredActiveZoneId = PersistedTextXml.Verify(restoredActiveZoneId, nameof(activeZoneId), "Active context id");

            var restoredActiveFloorId = (activeFloorId ?? string.Empty).Trim();
            if (restoredActiveFloorId.Any(char.IsControl))
                throw new ArgumentException("Active context id cannot contain control characters.", nameof(activeFloorId));
            restoredActiveFloorId = PersistedTextXml.Verify(restoredActiveFloorId, nameof(activeFloorId), "Active context id");

            _name = restoredName;
            _drawingPath = restoredDrawingPath;
            _drawingFingerprint = restoredDrawingFingerprint;
            _activeZoneId = restoredActiveZoneId;
            _activeFloorId = restoredActiveFloorId;
        }

        private void SetActiveContextId(ref string field, string? value)
        {
            var normalizedValue = (value ?? string.Empty).Trim();
            if (normalizedValue.Any(char.IsControl))
                throw new ArgumentException("Active context id cannot contain control characters.", nameof(value));
            SetPersistedScalar(ref field, PersistedTextXml.Verify(normalizedValue, nameof(value), "Active context id"));
        }

        private void SetCanonicalOptionalIdentity(ref string field, string? value, string label)
        {
            var rawValue = value ?? string.Empty;
            if (rawValue.Any(char.IsControl))
                throw new ArgumentException(label + " cannot contain control characters.", nameof(value));
            var normalizedValue = rawValue.Trim();
            SetPersistedScalar(ref field, PersistedTextXml.Verify(normalizedValue, nameof(value), label));
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
