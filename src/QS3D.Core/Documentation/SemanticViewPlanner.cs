using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Documentation
{
    public enum SemanticViewKind
    {
        Model,
        Plan,
        Schedule
    }

    internal static class SemanticViewEnumerableContract
    {
        internal static IReadOnlyList<T> SnapshotBounded<T>(
            IEnumerable<T> values,
            int maximumCount,
            string capacityMessage,
            string countChangedMessage)
        {
            var hasKnownCount = TryGetKnownCount(values, countChangedMessage, out var knownCount);
            if (hasKnownCount && knownCount > maximumCount)
                throw new InvalidOperationException(capacityMessage);

            var result = new List<T>(Math.Min(maximumCount, hasKnownCount ? knownCount : 256));
            var observedCount = 0;
            using (var enumerator = values.GetEnumerator())
            {
                while (true)
                {
                    if (hasKnownCount)
                        RequireStableKnownCount(values, knownCount, countChangedMessage);

                    var moved = enumerator.MoveNext();

                    if (hasKnownCount)
                        RequireStableKnownCount(values, knownCount, countChangedMessage);
                    if (!moved)
                        break;
                    if (hasKnownCount && observedCount >= knownCount)
                        throw new InvalidOperationException(countChangedMessage);
                    if (observedCount >= maximumCount)
                        throw new InvalidOperationException(capacityMessage);

                    var item = enumerator.Current;

                    if (hasKnownCount)
                        RequireStableKnownCount(values, knownCount, countChangedMessage);

                    result.Add(item);
                    observedCount++;
                }
            }

            if (hasKnownCount && observedCount != knownCount)
                throw new InvalidOperationException(countChangedMessage);
            if (hasKnownCount)
                RequireStableKnownCount(values, knownCount, countChangedMessage);

            return result.AsReadOnly();
        }

        private static void RequireStableKnownCount<T>(IEnumerable<T> values, int knownCount, string countChangedMessage)
        {
            if (!TryGetKnownCount(values, countChangedMessage, out var currentKnownCount) || currentKnownCount != knownCount)
                throw new InvalidOperationException(countChangedMessage);
        }

        private static bool TryGetKnownCount<T>(IEnumerable<T> values, string countChangedMessage, out int count)
        {
            var hasKnownCount = false;
            var firstKnownCount = 0;
            var maximumKnownCount = 0;

            void Observe(int candidate)
            {
                if (candidate < 0)
                    throw new InvalidOperationException(countChangedMessage);
                if (!hasKnownCount)
                {
                    hasKnownCount = true;
                    firstKnownCount = candidate;
                    maximumKnownCount = candidate;
                    return;
                }
                if (candidate != firstKnownCount)
                    throw new InvalidOperationException(countChangedMessage);
                if (candidate > maximumKnownCount)
                    maximumKnownCount = candidate;
            }

            if (values is ICollection<T> collection)
                Observe(collection.Count);
            if (values is IReadOnlyCollection<T> readOnlyCollection)
                Observe(readOnlyCollection.Count);
            if (values is ICollection nonGenericCollection)
                Observe(nonGenericCollection.Count);

            count = maximumKnownCount;
            return hasKnownCount;
        }
    }

    public sealed class SemanticViewDefinition
    {
        public SemanticViewDefinition(
            string id,
            string name,
            SemanticViewKind kind = SemanticViewKind.Model,
            string? floorId = null,
            string? zoneId = null,
            IEnumerable<ElementCategory>? categories = null,
            IEnumerable<string>? includeElementIds = null,
            IEnumerable<string>? excludeElementIds = null)
        {
            Id = id;
            Name = name;
            Kind = kind;
            FloorId = floorId;
            ZoneId = zoneId;
            Categories = SnapshotCategories(categories);
            IncludeElementIds = SnapshotFilterIds(includeElementIds, "includeElementIds");
            ExcludeElementIds = SnapshotFilterIds(excludeElementIds, "excludeElementIds");
        }

        public string Id { get; }
        public string Name { get; }
        public SemanticViewKind Kind { get; }
        public string? FloorId { get; }
        public string? ZoneId { get; }
        public IReadOnlyList<ElementCategory> Categories { get; }
        public IReadOnlyList<string> IncludeElementIds { get; }
        public IReadOnlyList<string> ExcludeElementIds { get; }

        private static IReadOnlyList<ElementCategory> SnapshotCategories(IEnumerable<ElementCategory>? values)
        {
            if (values == null) return Array.Empty<ElementCategory>();
            return SemanticViewEnumerableContract.SnapshotBounded(
                values,
                SemanticViewPlanner.MaxFilterIds,
                "Semantic view supports at most " + SemanticViewPlanner.MaxFilterIds + " categories.",
                "Semantic view category source Count changed during snapshot.");
        }

        private static IReadOnlyList<string> SnapshotFilterIds(IEnumerable<string>? values, string label)
        {
            if (values == null) return Array.Empty<string>();
            return SemanticViewEnumerableContract.SnapshotBounded(
                values,
                SemanticViewPlanner.MaxFilterIds,
                "Semantic view supports at most " + SemanticViewPlanner.MaxFilterIds + " " + label + ".",
                "Semantic view " + label + " source Count changed during snapshot.");
        }
    }

    public sealed class SemanticViewPlan
    {
        internal SemanticViewPlan(
            string id,
            string name,
            SemanticViewKind kind,
            string? floorId,
            string? zoneId,
            IReadOnlyList<string> elementIds)
        {
            Id = id;
            Name = name;
            Kind = kind;
            FloorId = floorId;
            ZoneId = zoneId;
            ElementIds = new List<string>(elementIds).AsReadOnly();
        }

        public string Id { get; }
        public string Name { get; }
        public SemanticViewKind Kind { get; }
        public string? FloorId { get; }
        public string? ZoneId { get; }
        public IReadOnlyList<string> ElementIds { get; }
    }

    public static class SemanticViewPlanner
    {
        private const int MaxCatalogViews = 10000;
        internal const int MaxFilterIds = 100000;
        private const int MaxIdLength = 128;
        private const int MaxNameLength = 160;

        public static SemanticViewPlan Build(ProjectState project, SemanticViewDefinition definition)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            var viewId = Required(definition.Id, nameof(definition.Id), MaxIdLength);
            var viewName = Required(definition.Name, nameof(definition.Name), MaxNameLength);
            var viewKind = RequiredKind(definition.Kind);
            var elementIndex = BuildUniqueElementIndex(project);

            var floorId = NormalizeOptional(definition.FloorId, MaxIdLength, nameof(definition.FloorId));
            if (floorId != null) EnsureUniqueReference(project.Floors, x => x.Id, floorId, "floor");

            var zoneId = NormalizeOptional(definition.ZoneId, MaxIdLength, nameof(definition.ZoneId));
            if (zoneId != null) EnsureUniqueReference(project.Zones, x => x.Id, zoneId, "zone");

            var categories = NormalizeCategories(definition.Categories);

            var includeIds = NormalizeIds(definition.IncludeElementIds, "includeElementIds");
            var excludeIds = NormalizeIds(definition.ExcludeElementIds, "excludeElementIds");
            if (includeIds.Overlaps(excludeIds))
                throw new InvalidOperationException("Semantic view cannot both include and exclude the same element id.");

            EnsureFilterIdsExist(includeIds, elementIndex, "included");
            EnsureFilterIdsExist(excludeIds, elementIndex, "excluded");

            IEnumerable<ProjectElement> query = project.Elements;
            if (floorId != null) query = query.Where(x => string.Equals((x.FloorId ?? string.Empty).Trim(), floorId, StringComparison.OrdinalIgnoreCase));
            if (zoneId != null) query = query.Where(x => string.Equals((x.ZoneId ?? string.Empty).Trim(), zoneId, StringComparison.OrdinalIgnoreCase));
            if (categories.Count > 0) query = query.Where(x => categories.Contains(x.Category));
            if (includeIds.Count > 0) query = query.Where(x => includeIds.Contains(x.Id));
            if (excludeIds.Count > 0) query = query.Where(x => !excludeIds.Contains(x.Id));

            var selectedIds = query
                .Select(x => x.Id)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToArray();

            return new SemanticViewPlan(viewId, viewName, viewKind, floorId, zoneId, selectedIds);
        }

        public static IReadOnlyList<SemanticViewPlan> BuildCatalog(ProjectState project, IEnumerable<SemanticViewDefinition> definitions)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            var projectSnapshot = CaptureProjectStructure(project);
            var materialized = MaterializeCatalogBounded(definitions);
            EnsureProjectStructureUnchanged(project, projectSnapshot);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var plans = new List<SemanticViewPlan>(materialized.Count);
            foreach (var definition in materialized)
            {
                if (definition == null) throw new ArgumentException("Semantic view definition cannot be null.", nameof(definitions));
                var plan = Build(project, definition);
                if (!ids.Add(plan.Id)) throw new InvalidOperationException("Semantic view catalog contains duplicate view id: " + plan.Id + ".");
                if (!names.Add(plan.Name)) throw new InvalidOperationException("Semantic view catalog contains duplicate view name: " + plan.Name + ".");
                plans.Add(plan);
            }

            var result = plans
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
            EnsureProjectStructureUnchanged(project, projectSnapshot);
            return result;
        }

        private static ProjectStructureSnapshot CaptureProjectStructure(ProjectState project) =>
            new ProjectStructureSnapshot(
                project.ChangeVersion,
                project.Elements.ToArray(),
                project.Floors.ToArray(),
                project.Zones.ToArray());

        private static void EnsureProjectStructureUnchanged(ProjectState project, ProjectStructureSnapshot snapshot)
        {
            if (project.ChangeVersion != snapshot.ChangeVersion)
                throw new InvalidOperationException("Project changed while the semantic view catalog was being planned.");
            EnsureSameReferences(project.Elements, snapshot.Elements);
            EnsureSameElementPlanningValues(project.Elements, snapshot.ElementPlanningValues);
            EnsureSameReferences(project.Floors, snapshot.Floors);
            EnsureSameReferences(project.Zones, snapshot.Zones);
        }

        private static void EnsureSameReferences<T>(IList<T> current, IReadOnlyList<T> expected) where T : class
        {
            if (current.Count != expected.Count)
                throw new InvalidOperationException("Project structure changed while the semantic view catalog was being planned.");
            for (var i = 0; i < expected.Count; i++)
                if (!ReferenceEquals(current[i], expected[i]))
                    throw new InvalidOperationException("Project structure changed while the semantic view catalog was being planned.");
        }

        private static void EnsureSameElementPlanningValues(
            IList<ProjectElement> current,
            IReadOnlyList<ProjectElementPlanningValues> expected)
        {
            if (current.Count != expected.Count)
                throw new InvalidOperationException("Project structure changed while the semantic view catalog was being planned.");
            for (var i = 0; i < expected.Count; i++)
            {
                var element = current[i];
                var values = expected[i];
                if (element == null)
                {
                    if (!values.IsNull)
                        throw new InvalidOperationException("Project structure changed while the semantic view catalog was being planned.");
                    continue;
                }

                if (values.IsNull ||
                    !string.Equals(element.Id, values.Id, StringComparison.Ordinal) ||
                    element.Category != values.Category ||
                    !string.Equals(element.FloorId, values.FloorId, StringComparison.Ordinal) ||
                    !string.Equals(element.ZoneId, values.ZoneId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Project structure changed while the semantic view catalog was being planned.");
            }
        }

        private static List<SemanticViewDefinition> MaterializeCatalogBounded(IEnumerable<SemanticViewDefinition> definitions)
        {
            var snapshot = SemanticViewEnumerableContract.SnapshotBounded(
                definitions,
                MaxCatalogViews,
                "Semantic view catalog supports at most " + MaxCatalogViews + " views.",
                "Semantic view catalog source Count changed during snapshot.");
            return new List<SemanticViewDefinition>(snapshot);
        }

        private sealed class ProjectStructureSnapshot
        {
            public ProjectStructureSnapshot(
                long changeVersion,
                IReadOnlyList<ProjectElement> elements,
                IReadOnlyList<FloorDefinition> floors,
                IReadOnlyList<ZoneDefinition> zones)
            {
                ChangeVersion = changeVersion;
                Elements = elements;
                ElementPlanningValues = elements.Select(x => new ProjectElementPlanningValues(x)).ToArray();
                Floors = floors;
                Zones = zones;
            }

            public long ChangeVersion { get; }
            public IReadOnlyList<ProjectElement> Elements { get; }
            public IReadOnlyList<ProjectElementPlanningValues> ElementPlanningValues { get; }
            public IReadOnlyList<FloorDefinition> Floors { get; }
            public IReadOnlyList<ZoneDefinition> Zones { get; }
        }

        private sealed class ProjectElementPlanningValues
        {
            public ProjectElementPlanningValues(ProjectElement? element)
            {
                IsNull = element == null;
                Id = element?.Id;
                Category = element?.Category ?? default;
                FloorId = element?.FloorId;
                ZoneId = element?.ZoneId;
            }

            public bool IsNull { get; }
            public string? Id { get; }
            public ElementCategory Category { get; }
            public string? FloorId { get; }
            public string? ZoneId { get; }
        }

        private static Dictionary<string, ProjectElement> BuildUniqueElementIndex(ProjectState project)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element.");
                var id = Required(element.Id, "project.Elements.Id", MaxIdLength);
                if (result.ContainsKey(id)) throw new InvalidOperationException("Project contains duplicate semantic element id: " + id + ".");
                result.Add(id, element);
            }
            return result;
        }

        private static HashSet<ElementCategory> NormalizeCategories(IReadOnlyList<ElementCategory> values)
        {
            var result = new HashSet<ElementCategory>();
            for (var i = 0; i < values.Count; i++)
            {
                var category = values[i];
                if (!Enum.IsDefined(typeof(ElementCategory), category))
                    throw new InvalidOperationException("Unsupported semantic view category filter '" + category + "'.");
                if (!result.Add(category))
                    throw new InvalidOperationException("Semantic view contains duplicate category filters.");
            }
            return result;
        }

        private static HashSet<string> NormalizeIds(IReadOnlyList<string> values, string label)
        {
            if (values.Count > MaxFilterIds) throw new InvalidOperationException("Semantic view supports at most " + MaxFilterIds + " " + label + ".");
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < values.Count; i++)
            {
                var id = Required(values[i], label + "[" + i + "]", MaxIdLength);
                if (!result.Add(id)) throw new InvalidOperationException("Semantic view contains duplicate " + label + " id: " + id + ".");
            }
            return result;
        }

        private static void EnsureFilterIdsExist(HashSet<string> ids, Dictionary<string, ProjectElement> elementIndex, string label)
        {
            foreach (var id in ids)
                if (!elementIndex.ContainsKey(id)) throw new InvalidOperationException("Semantic view references missing " + label + " element id: " + id + ".");
        }

        private static void EnsureUniqueReference<T>(IEnumerable<T> items, Func<T, string> idSelector, string requestedId, string label)
            where T : class
        {
            var count = 0;
            foreach (var item in items)
            {
                if (item == null) throw new InvalidOperationException("Project contains a null " + label + " entry.");
                if (string.Equals(idSelector(item), requestedId, StringComparison.OrdinalIgnoreCase)) count++;
            }

            if (count == 0) throw new InvalidOperationException("Semantic view references missing " + label + " id: " + requestedId + ".");
            if (count > 1) throw new InvalidOperationException("Semantic view references ambiguous " + label + " id: " + requestedId + ".");
        }

        private static SemanticViewKind RequiredKind(SemanticViewKind kind)
        {
            if (!Enum.IsDefined(typeof(SemanticViewKind), kind))
                throw new InvalidOperationException("Unsupported semantic view kind '" + kind + "'.");
            return kind;
        }

        private static string Required(string? value, string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var normalized = value!.Trim();
            if (normalized.Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
            return normalized;
        }

        private static string? NormalizeOptional(string? value, int maxLength, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var normalized = value!.Trim();
            if (normalized.Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
            return normalized;
        }
    }
}
