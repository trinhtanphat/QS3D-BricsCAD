using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Navigation
{
    public sealed class ProjectBrowserQueryOptions
    {
        public ProjectBrowserQueryOptions(
            string? query = null,
            bool dirtyOnly = false,
            IEnumerable<ElementCategory>? categories = null,
            IEnumerable<string>? floorIds = null,
            IEnumerable<string>? zoneIds = null)
        {
            Query = query;
            DirtyOnly = dirtyOnly;
            Categories = CopyBounded(categories, nameof(categories));
            FloorIds = CopyBounded(floorIds, nameof(floorIds));
            ZoneIds = CopyBounded(zoneIds, nameof(zoneIds));
        }

        public string? Query { get; }
        public bool DirtyOnly { get; }
        public IReadOnlyList<ElementCategory> Categories { get; }
        public IReadOnlyList<string> FloorIds { get; }
        public IReadOnlyList<string> ZoneIds { get; }

        private static IReadOnlyList<T> CopyBounded<T>(IEnumerable<T>? values, string parameterName)
        {
            if (values == null) return new List<T>().AsReadOnly();
            RejectOversizedKnownCount(values, parameterName);
            var result = new List<T>();
            foreach (var value in values)
            {
                if (result.Count >= ProjectBrowserQueryPlanner.MaxFilterIds)
                    throw TooManyFilterValues(parameterName);
                result.Add(value);
            }
            return result.AsReadOnly();
        }

        private static void RejectOversizedKnownCount<T>(IEnumerable<T> values, string parameterName)
        {
            if (values is ICollection<T> collection && collection.Count > ProjectBrowserQueryPlanner.MaxFilterIds)
                throw TooManyFilterValues(parameterName);
            if (values is IReadOnlyCollection<T> readOnlyCollection && readOnlyCollection.Count > ProjectBrowserQueryPlanner.MaxFilterIds)
                throw TooManyFilterValues(parameterName);
            if (values is System.Collections.ICollection nonGenericCollection && nonGenericCollection.Count > ProjectBrowserQueryPlanner.MaxFilterIds)
                throw TooManyFilterValues(parameterName);
        }

        private static InvalidOperationException TooManyFilterValues(string parameterName)
        {
            return new InvalidOperationException(
                "Project browser query option " + parameterName + " supports at most " +
                ProjectBrowserQueryPlanner.MaxFilterIds + " values.");
        }
    }

    public sealed class ProjectBrowserQueryResult
    {
        internal ProjectBrowserQueryResult(ProjectBrowserNode root, int totalCount, bool isFiltered)
        {
            Root = root;
            TotalCount = totalCount;
            IsFiltered = isFiltered;
        }

        public ProjectBrowserNode Root { get; }
        public int TotalCount { get; }
        public int MatchedCount => Root.Count;
        public bool IsFiltered { get; }
    }

    public static class ProjectBrowserQueryPlanner
    {
        private const int MaxElements = 250000;
        private const int MaxFamilies = 10000;
        private const int MaxReferenceDefinitions = 2000;
        private const int MaxQueryLength = 160;
        internal const int MaxFilterIds = 10000;

        public static ProjectBrowserQueryResult Build(
            ProjectState project,
            ProjectBrowserGrouping grouping,
            ProjectBrowserQueryOptions? options = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!Enum.IsDefined(typeof(ProjectBrowserGrouping), grouping)) throw new ArgumentOutOfRangeException(nameof(grouping));
            options = options ?? new ProjectBrowserQueryOptions();

            var query = NormalizeQuery(options.Query);
            var categories = NormalizeCategories(options.Categories);
            var isFiltered = query.Length > 0 || options.DirtyOnly || categories.Count > 0 || options.FloorIds.Count > 0 || options.ZoneIds.Count > 0;

            if (project.Elements.Count > MaxElements)
                throw new InvalidOperationException("Project browser supports at most " + MaxElements + " semantic elements.");
            if (project.Families.Count > MaxFamilies)
                throw new InvalidOperationException("Project browser query supports at most " + MaxFamilies + " family definitions.");
            if (project.Floors.Count > MaxReferenceDefinitions)
                throw new InvalidOperationException("Project browser query supports at most " + MaxReferenceDefinitions + " floor definitions.");
            if (project.Zones.Count > MaxReferenceDefinitions)
                throw new InvalidOperationException("Project browser query supports at most " + MaxReferenceDefinitions + " zone definitions.");

            var familyIndex = BuildUniqueFamilyIndex(project);
            var floorIndex = BuildUniqueFloorIndex(project);
            var zoneIndex = BuildUniqueZoneIndex(project);
            ValidateElementReferences(project, familyIndex, floorIndex, zoneIndex);

            if (!isFiltered)
                return new ProjectBrowserQueryResult(ProjectBrowserPlanner.Build(project, grouping), project.Elements.Count, false);

            var floorIds = NormalizeReferenceIds(options.FloorIds, floorIndex, "floor");
            var zoneIds = NormalizeReferenceIds(options.ZoneIds, zoneIndex, "zone");
            var matched = new List<ProjectElement>();
            foreach (var element in project.Elements)
            {
                if (options.DirtyOnly && element.Dirty == ElementDirtyFlags.None) continue;
                if (categories.Count > 0 && !categories.Contains(element.Category)) continue;
                if (floorIds.Count > 0 && !floorIds.Contains((element.FloorId ?? string.Empty).Trim())) continue;
                if (zoneIds.Count > 0 && !zoneIds.Contains((element.ZoneId ?? string.Empty).Trim())) continue;
                if (query.Length > 0 && !MatchesQuery(element, query, familyIndex, floorIndex, zoneIndex)) continue;
                matched.Add(element);
            }

            var filtered = new ProjectState(project.ProjectId, project.Name);
            foreach (var floor in project.Floors) filtered.Floors.Add(floor);
            foreach (var zone in project.Zones) filtered.Zones.Add(zone);
            foreach (var element in matched) filtered.Elements.Add(element);
            var root = ProjectBrowserPlanner.Build(filtered, grouping);
            return new ProjectBrowserQueryResult(root, project.Elements.Count, true);
        }

        private static string NormalizeQuery(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var query = raw!.Trim();
            if (query.Length > MaxQueryLength)
                throw new ArgumentException("Project browser search text exceeds " + MaxQueryLength + " characters.", nameof(raw));
            return query;
        }

        private static HashSet<ElementCategory> NormalizeCategories(IReadOnlyList<ElementCategory> values)
        {
            var result = new HashSet<ElementCategory>();
            foreach (var value in values)
            {
                if (!Enum.IsDefined(typeof(ElementCategory), value))
                    throw new ArgumentOutOfRangeException(nameof(values), "Project browser category filter contains an undefined category.");
                result.Add(value);
            }
            return result;
        }

        private static HashSet<string> NormalizeReferenceIds<T>(
            IReadOnlyList<string> values,
            Dictionary<string, T> index,
            string label)
        {
            if (values.Count > MaxFilterIds)
                throw new InvalidOperationException("Project browser supports at most " + MaxFilterIds + " " + label + " filters.");
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < values.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(values[i]))
                    throw new ArgumentException("Project browser " + label + " filter id is required at index " + i + ".", nameof(values));
                var id = values[i].Trim();
                if (!result.Add(id))
                    throw new InvalidOperationException("Project browser contains duplicate " + label + " filter id: " + id + ".");
                if (!index.ContainsKey(id))
                    throw new InvalidOperationException("Project browser references missing " + label + " filter id: " + id + ".");
            }
            return result;
        }

        private static bool MatchesQuery(
            ProjectElement element,
            string query,
            Dictionary<string, ProjectFamily> families,
            Dictionary<string, FloorDefinition> floors,
            Dictionary<string, ZoneDefinition> zones)
        {
            if (Contains(element.Id, query) || Contains(element.Category.ToString(), query)) return true;

            var familyId = (element.FamilyId ?? string.Empty).Trim();
            if (familyId.Length > 0)
            {
                var family = families[familyId];
                if (Contains(family.Id, query) || Contains(family.Name, query)) return true;
            }

            var floorId = (element.FloorId ?? string.Empty).Trim();
            if (floorId.Length > 0)
            {
                var floor = floors[floorId];
                if (Contains(floor.Id, query) || Contains(floor.Name, query)) return true;
            }

            var zoneId = (element.ZoneId ?? string.Empty).Trim();
            if (zoneId.Length > 0)
            {
                var zone = zones[zoneId];
                if (Contains(zone.Id, query) || Contains(zone.Name, query)) return true;
            }
            return false;
        }

        private static bool Contains(string? value, string query)
        {
            return value != null && value.Length > 0 && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Dictionary<string, ProjectFamily> BuildUniqueFamilyIndex(ProjectState project)
        {
            var result = new Dictionary<string, ProjectFamily>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in project.Families)
            {
                if (family == null) throw new InvalidOperationException("Project browser found a null family definition.");
                var id = (family.Id ?? string.Empty).Trim();
                if (id.Length == 0 || string.IsNullOrWhiteSpace(family.Name)) throw new InvalidOperationException("Project browser found an invalid family definition.");
                if (result.ContainsKey(id)) throw new InvalidOperationException("Project browser found duplicate family id: " + id + ".");
                result.Add(id, family);
            }
            return result;
        }

        private static Dictionary<string, FloorDefinition> BuildUniqueFloorIndex(ProjectState project)
        {
            var result = new Dictionary<string, FloorDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var floor in project.Floors)
            {
                if (floor == null) throw new InvalidOperationException("Project browser found a null floor definition.");
                var id = (floor.Id ?? string.Empty).Trim();
                if (id.Length == 0 || string.IsNullOrWhiteSpace(floor.Name)) throw new InvalidOperationException("Project browser found an invalid floor definition.");
                if (result.ContainsKey(id)) throw new InvalidOperationException("Project browser found duplicate floor id: " + id + ".");
                result.Add(id, floor);
            }
            return result;
        }

        private static Dictionary<string, ZoneDefinition> BuildUniqueZoneIndex(ProjectState project)
        {
            var result = new Dictionary<string, ZoneDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var zone in project.Zones)
            {
                if (zone == null) throw new InvalidOperationException("Project browser found a null zone definition.");
                var id = (zone.Id ?? string.Empty).Trim();
                if (id.Length == 0 || string.IsNullOrWhiteSpace(zone.Name)) throw new InvalidOperationException("Project browser found an invalid zone definition.");
                if (result.ContainsKey(id)) throw new InvalidOperationException("Project browser found duplicate zone id: " + id + ".");
                result.Add(id, zone);
            }
            return result;
        }

        private static void ValidateElementReferences(
            ProjectState project,
            Dictionary<string, ProjectFamily> families,
            Dictionary<string, FloorDefinition> floors,
            Dictionary<string, ZoneDefinition> zones)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null) throw new InvalidOperationException("Project browser found a null semantic element.");
                var elementId = (element.Id ?? string.Empty).Trim();
                if (elementId.Length == 0) throw new InvalidOperationException("Project browser found a blank semantic element id.");
                if (!ids.Add(elementId)) throw new InvalidOperationException("Project browser found duplicate semantic element id: " + elementId + ".");
                if (!Enum.IsDefined(typeof(ElementCategory), element.Category)) throw new InvalidOperationException("Project browser found undefined element category on: " + elementId + ".");

                var familyId = CanonicalOptionalReference(element.FamilyId, "family", elementId);
                if (familyId.Length > 0)
                {
                    if (!families.TryGetValue(familyId, out var family))
                        throw new InvalidOperationException("Project browser found missing family reference " + familyId + " on element " + elementId + ".");
                    if (family.Category != element.Category)
                        throw new InvalidOperationException("Project browser found family/category mismatch on element " + elementId + ": family " + family.Id + " is " + family.Category + " while element is " + element.Category + ".");
                }
                var floorId = CanonicalOptionalReference(element.FloorId, "floor", elementId);
                if (floorId.Length > 0 && !floors.ContainsKey(floorId))
                    throw new InvalidOperationException("Project browser found missing floor reference " + floorId + " on element " + elementId + ".");
                var zoneId = CanonicalOptionalReference(element.ZoneId, "zone", elementId);
                if (zoneId.Length > 0 && !zones.ContainsKey(zoneId))
                    throw new InvalidOperationException("Project browser found missing zone reference " + zoneId + " on element " + elementId + ".");
            }
        }

        private static string CanonicalOptionalReference(string? value, string label, string elementId)
        {
            var raw = value ?? string.Empty;
            if (raw.Length == 0) return string.Empty;
            if (string.IsNullOrWhiteSpace(raw) || !string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Project browser query requires canonical " + label + " references without surrounding whitespace on element " + elementId + ".");
            return raw;
        }
    }
}