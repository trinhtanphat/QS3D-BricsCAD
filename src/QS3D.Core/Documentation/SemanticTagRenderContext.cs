using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Documentation
{
    internal sealed class SemanticTagRenderContext
    {
        private readonly ProjectState _project;
        private readonly Dictionary<string, ProjectElement> _elements;
        private readonly HashSet<string> _ambiguousElementIds;
        private Dictionary<string, ProjectFamily>? _families;
        private HashSet<string>? _ambiguousFamilyIds;
        private Dictionary<string, FloorDefinition>? _floors;
        private HashSet<string>? _ambiguousFloorIds;
        private Dictionary<string, ZoneDefinition>? _zones;
        private HashSet<string>? _ambiguousZoneIds;

        public SemanticTagRenderContext(ProjectState project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _elements = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            _ambiguousElementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element.");
                Add(_elements, _ambiguousElementIds, element.Id, element);
            }
        }

        public ProjectState Project => _project;

        public ProjectElement ResolveElement(string id)
        {
            var raw = id ?? string.Empty;
            var normalized = raw.Trim();
            if (normalized.Length == 0) throw new InvalidOperationException("Semantic documentation element id is required.");
            if (!string.Equals(raw, normalized, StringComparison.Ordinal))
                throw new InvalidOperationException("Semantic documentation element id is non-canonical: \"" + id + "\".");
            if (_ambiguousElementIds.Contains(normalized))
                throw new InvalidOperationException("Semantic documentation element id is ambiguous: " + normalized + ".");
            if (!_elements.TryGetValue(normalized, out var element))
                throw new InvalidOperationException("Semantic documentation element does not exist: " + normalized + ".");
            return element;
        }

        public void EnsureElement(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (_ambiguousElementIds.Contains(element.Id))
                throw new InvalidOperationException("Semantic tag element id is ambiguous in project: " + element.Id + ".");
            if (!_elements.TryGetValue(element.Id, out var match) || !ReferenceEquals(match, element))
                throw new InvalidOperationException("Semantic tag element is not part of the supplied project: " + element.Id + ".");
        }

        public string ResolveFamily(ProjectElement element)
        {
            if (string.IsNullOrEmpty(element.FamilyId)) return string.Empty;
            EnsureFamilyIndex();
            return ResolveReference(
                element.FamilyId,
                _families!,
                _ambiguousFamilyIds!,
                "Family",
                element.Id,
                x => x.Name);
        }

        public string ResolveFloor(ProjectElement element)
        {
            if (string.IsNullOrEmpty(element.FloorId)) return string.Empty;
            EnsureFloorIndex();
            return ResolveReference(
                element.FloorId,
                _floors!,
                _ambiguousFloorIds!,
                "Floor",
                element.Id,
                x => x.Name);
        }

        public string ResolveZone(ProjectElement element)
        {
            if (string.IsNullOrEmpty(element.ZoneId)) return string.Empty;
            EnsureZoneIndex();
            return ResolveReference(
                element.ZoneId,
                _zones!,
                _ambiguousZoneIds!,
                "Zone",
                element.Id,
                x => x.Name);
        }

        private void EnsureFamilyIndex()
        {
            if (_families != null) return;
            _families = new Dictionary<string, ProjectFamily>(StringComparer.OrdinalIgnoreCase);
            _ambiguousFamilyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in _project.Families)
            {
                if (family == null) throw new InvalidOperationException("Project contains a null Family entry.");
                Add(_families, _ambiguousFamilyIds, family.Id, family);
            }
        }

        private void EnsureFloorIndex()
        {
            if (_floors != null) return;
            _floors = new Dictionary<string, FloorDefinition>(StringComparer.OrdinalIgnoreCase);
            _ambiguousFloorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var floor in _project.Floors)
            {
                if (floor == null) throw new InvalidOperationException("Project contains a null Floor entry.");
                Add(_floors, _ambiguousFloorIds, floor.Id, floor);
            }
        }

        private void EnsureZoneIndex()
        {
            if (_zones != null) return;
            _zones = new Dictionary<string, ZoneDefinition>(StringComparer.OrdinalIgnoreCase);
            _ambiguousZoneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var zone in _project.Zones)
            {
                if (zone == null) throw new InvalidOperationException("Project contains a null Zone entry.");
                Add(_zones, _ambiguousZoneIds, zone.Id, zone);
            }
        }

        private static void Add<T>(IDictionary<string, T> index, ISet<string> ambiguous, string rawId, T value)
        {
            var id = rawId ?? string.Empty;
            var canonicalId = id.Trim();
            if (canonicalId.Length == 0) throw new InvalidOperationException("Semantic documentation index contains an empty id.");
            if (!string.Equals(id, canonicalId, StringComparison.Ordinal))
                throw new InvalidOperationException("Semantic documentation index contains a non-canonical id: \"" + rawId + "\".");
            if (index.ContainsKey(id))
            {
                ambiguous.Add(id);
                return;
            }
            index[id] = value;
        }

        private static string ResolveReference<T>(
            string rawId,
            IReadOnlyDictionary<string, T> index,
            ISet<string> ambiguous,
            string label,
            string elementId,
            Func<T, string> nameSelector)
        {
            var id = (rawId ?? string.Empty).Trim();
            if (!string.Equals(rawId, id, StringComparison.Ordinal))
                throw new InvalidOperationException("Semantic tag references non-canonical " + label + " \"" + rawId + "\" on element " + elementId + ".");
            if (ambiguous.Contains(id))
                throw new InvalidOperationException("Semantic tag references ambiguous " + label + " " + id + " on element " + elementId + ".");
            if (!index.TryGetValue(id, out var match))
                throw new InvalidOperationException("Semantic tag references missing " + label + " " + id + " on element " + elementId + ".");
            return nameSelector(match) ?? string.Empty;
        }
    }
}
