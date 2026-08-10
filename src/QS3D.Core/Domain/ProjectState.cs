using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace QS3D.Core.Domain
{
    public sealed class ZoneDefinition
    {
        public ZoneDefinition(string id, string name) { Id = Require(id, nameof(id)); Name = Require(name, nameof(name)); }
        public string Id { get; }
        public string Name { get; set; }
        private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", name) : value.Trim();
    }

    public sealed class FloorDefinition
    {
        public FloorDefinition(string id, string name, double elevationM) { Id = Require(id, nameof(id)); Name = Require(name, nameof(name)); ElevationM = elevationM; }
        public string Id { get; }
        public string Name { get; set; }
        public double ElevationM { get; set; }
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
            _category = category;
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
                if (_category == value) return;
                _category = value;
                OnPropertyChanged();
            }
        }
        public IDictionary<string, string> Properties { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private static string RequireName(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Family name is required.", nameof(value)) : value.Trim();
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class ProjectState
    {
        public const int CurrentSchemaVersion = 2;

        public ProjectState(string projectId, string name)
        {
            ProjectId = string.IsNullOrWhiteSpace(projectId) ? throw new ArgumentException("Project id is required.", nameof(projectId)) : projectId.Trim();
            Name = string.IsNullOrWhiteSpace(name) ? "QS3D Project" : name.Trim();
            Zones = new List<ZoneDefinition>();
            Floors = new List<FloorDefinition>();
            Families = new List<ProjectFamily>();
            Elements = new List<ProjectElement>();
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string ProjectId { get; }
        public string Name { get; set; }
        public string DrawingPath { get; set; } = string.Empty;
        public string DrawingFingerprint { get; set; } = string.Empty;
        public string ActiveZoneId { get; set; } = string.Empty;
        public string ActiveFloorId { get; set; } = string.Empty;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
        public IList<ZoneDefinition> Zones { get; }
        public IList<FloorDefinition> Floors { get; }
        public IList<ProjectFamily> Families { get; }
        public IList<ProjectElement> Elements { get; }
        public IDictionary<string, string> Metadata { get; }

        public ProjectElement? FindElement(string id) => Elements.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        public ProjectFamily? FindFamily(string id) => Families.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));

        public void Touch() => UpdatedUtc = DateTime.UtcNow;
    }
}
