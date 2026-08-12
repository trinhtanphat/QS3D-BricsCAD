using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
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
                _elevationM = value;
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
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Family id is required.", nameof(id)) : id.Trim();
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
        private static string RequireName(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Family name is required.", nameof(value)) : value.Trim();
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
        public const int CurrentSchemaVersion = 3;
        private string _name;

        public ProjectState(string projectId, string name)
        {
            ProjectId = string.IsNullOrWhiteSpace(projectId) ? throw new ArgumentException("Project id is required.", nameof(projectId)) : projectId.Trim();
            _name = string.IsNullOrWhiteSpace(name) ? "QS3D Project" : name.Trim();
            Zones = new List<ZoneDefinition>();
            Floors = new List<FloorDefinition>();
            Families = new List<ProjectFamily>();
            Elements = new List<ProjectElement>();
            QuantityRules = new List<QuantityRule>();
            AuditEvents = new List<AuditEvent>();
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string ProjectId { get; }
        public string Name
        {
            get => _name;
            set => _name = RequireProjectName(value);
        }
        public string DrawingPath { get; set; } = string.Empty;
        public string DrawingFingerprint { get; set; } = string.Empty;
        public string ActiveZoneId { get; set; } = string.Empty;
        public string ActiveFloorId { get; set; } = string.Empty;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
        public long ChangeVersion { get; private set; }
        public IList<ZoneDefinition> Zones { get; }
        public IList<FloorDefinition> Floors { get; }
        public IList<ProjectFamily> Families { get; }
        public IList<ProjectElement> Elements { get; }
        public IList<QuantityRule> QuantityRules { get; }
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
            if (updatedUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Project persistence timestamp must be UTC.", nameof(updatedUtc));
            if (changeVersion < 0L)
                throw new ArgumentOutOfRangeException(nameof(changeVersion), "Project change version cannot be negative.");
            UpdatedUtc = updatedUtc;
            ChangeVersion = changeVersion;
        }

        private static string RequireProjectName(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Project name is required.", nameof(value)) : value.Trim();
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
