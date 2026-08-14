using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Mapping;
using QS3D.Core.Rules;

namespace QS3D.Core.Domain
{
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

        private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", name) : value.Trim();
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

        private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", name) : value.Trim();
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
            return normalized;
        }
        private static string RequireName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Family name is required.", nameof(value));
            var normalized = value.Trim();
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Family name cannot contain control characters.", nameof(value));
            return normalized;
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
            ProjectId = string.IsNullOrWhiteSpace(projectId) ? throw new ArgumentException("Project id is required.", nameof(projectId)) : projectId.Trim();
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
            set => SetPersistedScalar(ref _drawingPath, value);
        }
        public string DrawingFingerprint
        {
            get => _drawingFingerprint;
            set => SetPersistedScalar(ref _drawingFingerprint, value);
        }
        public string ActiveZoneId
        {
            get => _activeZoneId;
            set => SetPersistedScalar(ref _activeZoneId, value);
        }
        public string ActiveFloorId
        {
            get => _activeFloorId;
            set => SetPersistedScalar(ref _activeFloorId, value);
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

        private void SetPersistedScalar(ref string field, string value)
        {
            if (string.Equals(field, value, StringComparison.Ordinal)) return;
            var nextChangeVersion = checked(ChangeVersion + 1L);
            var nextUpdatedUtc = DateTime.UtcNow;
            field = value;
            UpdatedUtc = nextUpdatedUtc;
            ChangeVersion = nextChangeVersion;
        }

        private static DateTime RequireUtcTimestamp(DateTime value, string parameterName)
        {
            if (value.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Project persistence timestamp must be UTC.", parameterName);
            return value;
        }

        private static string RequireProjectName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Project name is required.", nameof(value));
            var normalized = value.Trim();
            if (normalized.Any(char.IsControl))
                throw new ArgumentException("Project name cannot contain control characters.", nameof(value));
            return normalized;
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
