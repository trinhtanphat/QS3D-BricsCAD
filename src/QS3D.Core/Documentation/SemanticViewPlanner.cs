using System;
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
            Categories = categories == null ? new List<ElementCategory>().AsReadOnly() : new List<ElementCategory>(categories).AsReadOnly();
            IncludeElementIds = includeElementIds == null ? new List<string>().AsReadOnly() : new List<string>(includeElementIds).AsReadOnly();
            ExcludeElementIds = excludeElementIds == null ? new List<string>().AsReadOnly() : new List<string>(excludeElementIds).AsReadOnly();
        }

        public string Id { get; }
        public string Name { get; }
        public SemanticViewKind Kind { get; }
        public string? FloorId { get; }
        public string? ZoneId { get; }
        public IReadOnlyList<ElementCategory> Categories { get; }
        public IReadOnlyList<string> IncludeElementIds { get; }
        public IReadOnlyList<string> ExcludeElementIds { get; }
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
        private const int MaxFilterIds = 100000;
        private const int MaxIdLength = 128;
        private const int MaxNameLength = 160;

        public static SemanticViewPlan Build(ProjectState project, SemanticViewDefinition definition)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            var viewId = Required(definition.Id, nameof(definition.Id), MaxIdLength);
            var viewName = Required(definition.Name, nameof(definition.Name), MaxNameLength);
            var elementIndex = BuildUniqueElementIndex(project);

            var floorId = NormalizeOptional(definition.FloorId, MaxIdLength, nameof(definition.FloorId));
            if (floorId != null) EnsureUniqueReference(project.Floors, x => x.Id, floorId, "floor");

            var zoneId = NormalizeOptional(definition.ZoneId, MaxIdLength, nameof(definition.ZoneId));
            if (zoneId != null) EnsureUniqueReference(project.Zones, x => x.Id, zoneId, "zone");

            var categories = new HashSet<ElementCategory>(definition.Categories);
            if (categories.Count != definition.Categories.Count)
                throw new InvalidOperationException("Semantic view contains duplicate category filters.");

            var includeIds = NormalizeIds(definition.IncludeElementIds, "includeElementIds");
            var excludeIds = NormalizeIds(definition.ExcludeElementIds, "excludeElementIds");
            if (includeIds.Overlaps(excludeIds))
                throw new InvalidOperationException("Semantic view cannot both include and exclude the same element id.");

            EnsureFilterIdsExist(includeIds, elementIndex, "included");
            EnsureFilterIdsExist(excludeIds, elementIndex, "excluded");

            IEnumerable<ProjectElement> query = project.Elements;
            if (floorId != null) query = query.Where(x => string.Equals(x.FloorId, floorId, StringComparison.OrdinalIgnoreCase));
            if (zoneId != null) query = query.Where(x => string.Equals(x.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase));
            if (categories.Count > 0) query = query.Where(x => categories.Contains(x.Category));
            if (includeIds.Count > 0) query = query.Where(x => includeIds.Contains(x.Id));
            if (excludeIds.Count > 0) query = query.Where(x => !excludeIds.Contains(x.Id));

            var selectedIds = query
                .Select(x => x.Id)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToArray();

            return new SemanticViewPlan(viewId, viewName, definition.Kind, floorId, zoneId, selectedIds);
        }

        public static IReadOnlyList<SemanticViewPlan> BuildCatalog(ProjectState project, IEnumerable<SemanticViewDefinition> definitions)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            var materialized = definitions.ToList();
            if (materialized.Count > 10000) throw new InvalidOperationException("Semantic view catalog supports at most 10000 views.");

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

            return plans
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
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
