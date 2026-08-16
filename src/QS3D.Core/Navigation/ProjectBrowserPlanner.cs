using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Navigation
{
    public enum ProjectBrowserGrouping
    {
        FloorThenCategory,
        ZoneThenCategory,
        Category
    }

    public enum ProjectBrowserNodeKind
    {
        Root,
        Floor,
        Zone,
        Category
    }

    public sealed class ProjectBrowserNode
    {
        internal ProjectBrowserNode(
            string key,
            string displayName,
            ProjectBrowserNodeKind kind,
            IReadOnlyList<string> elementIds,
            int dirtyCount,
            IReadOnlyList<ProjectBrowserNode> children)
        {
            Key = key;
            DisplayName = displayName;
            Kind = kind;
            ElementIds = new List<string>(elementIds).AsReadOnly();
            DirtyCount = dirtyCount;
            Children = new List<ProjectBrowserNode>(children).AsReadOnly();
        }

        public string Key { get; }
        public string DisplayName { get; }
        public ProjectBrowserNodeKind Kind { get; }
        public int Count => ElementIds.Count;
        public int DirtyCount { get; }
        public IReadOnlyList<string> ElementIds { get; }
        public IReadOnlyList<ProjectBrowserNode> Children { get; }
    }

    public static class ProjectBrowserPlanner
    {
        private const int MaxElements = 250000;
        private const int MaxReferenceDefinitions = 2000;
        private const string UnassignedFloorKey = "@unassigned-floor";
        private const string UnassignedZoneKey = "@unassigned-zone";

        public static ProjectBrowserNode Build(ProjectState project, ProjectBrowserGrouping grouping)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!Enum.IsDefined(typeof(ProjectBrowserGrouping), grouping)) throw new ArgumentOutOfRangeException(nameof(grouping));
            if (project.Elements.Count > MaxElements) throw new InvalidOperationException("Project browser supports at most " + MaxElements + " semantic elements.");
            if (project.Floors.Count > MaxReferenceDefinitions) throw new InvalidOperationException("Project browser supports at most " + MaxReferenceDefinitions + " floor definitions.");
            if (project.Zones.Count > MaxReferenceDefinitions) throw new InvalidOperationException("Project browser supports at most " + MaxReferenceDefinitions + " zone definitions.");

            var elements = ValidateAndOrderElements(project);
            var floors = BuildFloorIndex(project);
            var zones = BuildZoneIndex(project);
            ValidateReferences(elements, floors, zones);

            IReadOnlyList<ProjectBrowserNode> children;
            switch (grouping)
            {
                case ProjectBrowserGrouping.FloorThenCategory:
                    children = BuildFloorNodes(elements, floors);
                    break;
                case ProjectBrowserGrouping.ZoneThenCategory:
                    children = BuildZoneNodes(elements, zones);
                    break;
                case ProjectBrowserGrouping.Category:
                    children = BuildCategoryNodes(elements);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(grouping));
            }

            return CreateNode("project:" + project.ProjectId, project.Name, ProjectBrowserNodeKind.Root, elements, children);
        }

        private static List<ProjectElement> ValidateAndOrderElements(ProjectState project)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var elements = new List<ProjectElement>(project.Elements.Count);
            foreach (var element in project.Elements)
            {
                if (element == null) throw new InvalidOperationException("Project browser cannot index a null semantic element.");
                if (string.IsNullOrWhiteSpace(element.Id)) throw new InvalidOperationException("Project browser requires non-empty semantic element IDs.");
                if (!string.Equals(element.Id, element.Id.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Project browser requires canonical semantic element IDs without surrounding whitespace: " + element.Id + ".");
                if (!ids.Add(element.Id)) throw new InvalidOperationException("Project browser found duplicate semantic element id: " + element.Id + ".");
                if (!Enum.IsDefined(typeof(ElementCategory), element.Category)) throw new InvalidOperationException("Project browser found undefined element category on: " + element.Id + ".");
                elements.Add(element);
            }
            return elements
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Id, StringComparer.Ordinal)
                .ToList();
        }

        private static Dictionary<string, FloorDefinition> BuildFloorIndex(ProjectState project)
        {
            var result = new Dictionary<string, FloorDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var floor in project.Floors)
            {
                if (floor == null || string.IsNullOrWhiteSpace(floor.Id) || string.IsNullOrWhiteSpace(floor.Name))
                    throw new InvalidOperationException("Project browser requires valid floor definitions.");
                var id = floor.Id.Trim();
                if (result.ContainsKey(id)) throw new InvalidOperationException("Project browser found duplicate floor id: " + id + ".");
                result.Add(id, floor);
            }
            return result;
        }

        private static Dictionary<string, ZoneDefinition> BuildZoneIndex(ProjectState project)
        {
            var result = new Dictionary<string, ZoneDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var zone in project.Zones)
            {
                if (zone == null || string.IsNullOrWhiteSpace(zone.Id) || string.IsNullOrWhiteSpace(zone.Name))
                    throw new InvalidOperationException("Project browser requires valid zone definitions.");
                var id = zone.Id.Trim();
                if (result.ContainsKey(id)) throw new InvalidOperationException("Project browser found duplicate zone id: " + id + ".");
                result.Add(id, zone);
            }
            return result;
        }

        private static void ValidateReferences(
            IEnumerable<ProjectElement> elements,
            Dictionary<string, FloorDefinition> floors,
            Dictionary<string, ZoneDefinition> zones)
        {
            foreach (var element in elements)
            {
                var floorId = CanonicalOptionalReference(element.FloorId, "floor", element.Id);
                if (floorId.Length > 0 && !floors.ContainsKey(floorId))
                    throw new InvalidOperationException("Project browser found missing floor reference " + floorId + " on element " + element.Id + ".");
                var zoneId = CanonicalOptionalReference(element.ZoneId, "zone", element.Id);
                if (zoneId.Length > 0 && !zones.ContainsKey(zoneId))
                    throw new InvalidOperationException("Project browser found missing zone reference " + zoneId + " on element " + element.Id + ".");
            }
        }

        private static string CanonicalOptionalReference(string value, string label, string elementId)
        {
            var raw = value ?? string.Empty;
            if (raw.Length == 0) return string.Empty;
            if (string.IsNullOrWhiteSpace(raw) || !string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Project browser requires canonical " + label + " references without surrounding whitespace on element " + elementId + ".");
            return raw;
        }

        private static IReadOnlyList<ProjectBrowserNode> BuildFloorNodes(
            IReadOnlyList<ProjectElement> elements,
            Dictionary<string, FloorDefinition> floors)
        {
            var assigned = elements
                .Where(x => !string.IsNullOrWhiteSpace(x.FloorId))
                .GroupBy(x => x.FloorId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => new
                {
                    Floor = floors[group.Key],
                    Elements = group.ToList()
                })
                .OrderBy(x => x.Floor.ElevationM)
                .ThenBy(x => x.Floor.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Floor.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => CreateNode(
                    "floor:" + x.Floor.Id,
                    x.Floor.Name,
                    ProjectBrowserNodeKind.Floor,
                    x.Elements,
                    BuildCategoryNodes(x.Elements)))
                .ToList();

            var unassigned = elements.Where(x => string.IsNullOrWhiteSpace(x.FloorId)).ToList();
            if (unassigned.Count > 0)
                assigned.Add(CreateNode(UnassignedFloorKey, "(No Floor)", ProjectBrowserNodeKind.Floor, unassigned, BuildCategoryNodes(unassigned)));
            return assigned;
        }

        private static IReadOnlyList<ProjectBrowserNode> BuildZoneNodes(
            IReadOnlyList<ProjectElement> elements,
            Dictionary<string, ZoneDefinition> zones)
        {
            var assigned = elements
                .Where(x => !string.IsNullOrWhiteSpace(x.ZoneId))
                .GroupBy(x => x.ZoneId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => new
                {
                    Zone = zones[group.Key],
                    Elements = group.ToList()
                })
                .OrderBy(x => x.Zone.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Zone.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => CreateNode(
                    "zone:" + x.Zone.Id,
                    x.Zone.Name,
                    ProjectBrowserNodeKind.Zone,
                    x.Elements,
                    BuildCategoryNodes(x.Elements)))
                .ToList();

            var unassigned = elements.Where(x => string.IsNullOrWhiteSpace(x.ZoneId)).ToList();
            if (unassigned.Count > 0)
                assigned.Add(CreateNode(UnassignedZoneKey, "(No Zone)", ProjectBrowserNodeKind.Zone, unassigned, BuildCategoryNodes(unassigned)));
            return assigned;
        }

        private static IReadOnlyList<ProjectBrowserNode> BuildCategoryNodes(IEnumerable<ProjectElement> elements)
        {
            return elements
                .GroupBy(x => x.Category)
                .OrderBy(x => x.Key.ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(group => CreateNode(
                    "category:" + group.Key,
                    group.Key.ToString(),
                    ProjectBrowserNodeKind.Category,
                    group.ToList(),
                    Array.Empty<ProjectBrowserNode>()))
                .ToArray();
        }

        private static ProjectBrowserNode CreateNode(
            string key,
            string displayName,
            ProjectBrowserNodeKind kind,
            IEnumerable<ProjectElement> elements,
            IReadOnlyList<ProjectBrowserNode> children)
        {
            var ordered = elements
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Id, StringComparer.Ordinal)
                .ToArray();
            return new ProjectBrowserNode(
                key,
                displayName,
                kind,
                ordered.Select(x => x.Id).ToArray(),
                ordered.Count(x => x.Dirty != ElementDirtyFlags.None),
                children);
        }
    }
}
