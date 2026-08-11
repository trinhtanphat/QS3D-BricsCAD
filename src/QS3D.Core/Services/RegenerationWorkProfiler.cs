using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public enum RegenerationWorkScope
    {
        Project,
        Subset
    }

    public sealed class RegenerationWorkItem
    {
        public RegenerationWorkItem(
            int orderIndex,
            string elementId,
            ElementCategory category,
            ElementDirtyFlags dirtyFlags,
            int dependencyDepth,
            int directPlannedDependencyCount,
            int directPlannedDependentCount)
        {
            if (orderIndex < 0) throw new ArgumentOutOfRangeException(nameof(orderIndex));
            if (!Enum.IsDefined(typeof(ElementCategory), category)) throw new ArgumentOutOfRangeException(nameof(category));
            if ((dirtyFlags & ~ElementDirtyFlags.All) != ElementDirtyFlags.None) throw new ArgumentOutOfRangeException(nameof(dirtyFlags));
            if (dependencyDepth < 0) throw new ArgumentOutOfRangeException(nameof(dependencyDepth));
            if (directPlannedDependencyCount < 0) throw new ArgumentOutOfRangeException(nameof(directPlannedDependencyCount));
            if (directPlannedDependentCount < 0) throw new ArgumentOutOfRangeException(nameof(directPlannedDependentCount));
            OrderIndex = orderIndex;
            ElementId = string.IsNullOrWhiteSpace(elementId)
                ? throw new ArgumentException("Regeneration work item element id is required.", nameof(elementId))
                : elementId;
            Category = category;
            DirtyFlags = dirtyFlags;
            DependencyDepth = dependencyDepth;
            DirectPlannedDependencyCount = directPlannedDependencyCount;
            DirectPlannedDependentCount = directPlannedDependentCount;
        }

        public int OrderIndex { get; }
        public string ElementId { get; }
        public ElementCategory Category { get; }
        public ElementDirtyFlags DirtyFlags { get; }
        public int DependencyDepth { get; }
        public int DirectPlannedDependencyCount { get; }
        public int DirectPlannedDependentCount { get; }
        public bool HasSemanticDirtyWork =>
            (DirtyFlags & (ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity)) != ElementDirtyFlags.None;
    }

    public sealed class RegenerationCategoryWork
    {
        public RegenerationCategoryWork(ElementCategory category, int plannedElementCount, int semanticDirtyElementCount)
        {
            if (!Enum.IsDefined(typeof(ElementCategory), category)) throw new ArgumentOutOfRangeException(nameof(category));
            if (plannedElementCount < 0) throw new ArgumentOutOfRangeException(nameof(plannedElementCount));
            if (semanticDirtyElementCount < 0 || semanticDirtyElementCount > plannedElementCount)
                throw new ArgumentOutOfRangeException(nameof(semanticDirtyElementCount));
            Category = category;
            PlannedElementCount = plannedElementCount;
            SemanticDirtyElementCount = semanticDirtyElementCount;
        }

        public ElementCategory Category { get; }
        public int PlannedElementCount { get; }
        public int SemanticDirtyElementCount { get; }
    }

    public sealed class RegenerationWorkProfile
    {
        public RegenerationWorkProfile(
            string projectId,
            long sourceChangeVersion,
            RegenerationWorkScope scope,
            IEnumerable<string> targetElementIds,
            int projectElementCount,
            int dirtyProjectElementCount,
            IEnumerable<RegenerationWorkItem> items,
            IEnumerable<RegenerationCategoryWork> categories,
            int internalDependencyEdgeCount,
            int maxDependencyDepth)
        {
            ProjectId = string.IsNullOrWhiteSpace(projectId)
                ? throw new ArgumentException("Project id is required.", nameof(projectId))
                : projectId;
            if (sourceChangeVersion < 0L) throw new ArgumentOutOfRangeException(nameof(sourceChangeVersion));
            if (!Enum.IsDefined(typeof(RegenerationWorkScope), scope)) throw new ArgumentOutOfRangeException(nameof(scope));
            if (projectElementCount < 0) throw new ArgumentOutOfRangeException(nameof(projectElementCount));
            if (dirtyProjectElementCount < 0 || dirtyProjectElementCount > projectElementCount)
                throw new ArgumentOutOfRangeException(nameof(dirtyProjectElementCount));
            if (internalDependencyEdgeCount < 0) throw new ArgumentOutOfRangeException(nameof(internalDependencyEdgeCount));
            if (maxDependencyDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDependencyDepth));

            ProjectId = projectId;
            SourceChangeVersion = sourceChangeVersion;
            Scope = scope;
            TargetElementIds = (targetElementIds ?? throw new ArgumentNullException(nameof(targetElementIds))).ToList().AsReadOnly();
            ProjectElementCount = projectElementCount;
            DirtyProjectElementCount = dirtyProjectElementCount;
            Items = (items ?? throw new ArgumentNullException(nameof(items))).ToList().AsReadOnly();
            Categories = (categories ?? throw new ArgumentNullException(nameof(categories))).ToList().AsReadOnly();
            InternalDependencyEdgeCount = internalDependencyEdgeCount;
            MaxDependencyDepth = maxDependencyDepth;
        }

        public string ProjectId { get; }
        public long SourceChangeVersion { get; }
        public RegenerationWorkScope Scope { get; }
        public IReadOnlyList<string> TargetElementIds { get; }
        public int ProjectElementCount { get; }
        public int DirtyProjectElementCount { get; }
        public IReadOnlyList<RegenerationWorkItem> Items { get; }
        public IReadOnlyList<RegenerationCategoryWork> Categories { get; }
        public int InternalDependencyEdgeCount { get; }
        public int MaxDependencyDepth { get; }
        public int PlannedElementCount => Items.Count;
        public int SemanticDirtyElementCount => Items.Count(x => x.HasSemanticDirtyWork);
        public int GeometryOnlyDirtyElementCount => PlannedElementCount - SemanticDirtyElementCount;
        public bool HasWork => PlannedElementCount > 0;
    }

    public sealed class RegenerationWorkProfiler
    {
        private readonly DependencyGraph _graph;

        public RegenerationWorkProfiler() : this(new DependencyGraph()) { }

        public RegenerationWorkProfiler(DependencyGraph graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public RegenerationWorkProfile Profile(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return Build(project, RegenerationWorkScope.Project, Array.Empty<string>(), project.Elements);
        }

        public RegenerationWorkProfile ProfileSubset(ProjectState project, IEnumerable<string> elementIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));

            var requested = CanonicalTargetIds(elementIds);
            if (requested.Count == 0)
                return Build(project, RegenerationWorkScope.Subset, Array.Empty<string>(), Array.Empty<ProjectElement>());

            var unresolved = new HashSet<string>(requested, StringComparer.OrdinalIgnoreCase);
            var candidates = new List<ProjectElement>(unresolved.Count);
            var seenProjectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element entry.");
                if (!seenProjectIds.Add(element.Id))
                    throw new InvalidOperationException("Project contains duplicate element id: " + element.Id);
                if (unresolved.Remove(element.Id)) candidates.Add(element);
            }
            if (unresolved.Count > 0)
            {
                var missing = unresolved.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).First();
                throw new KeyNotFoundException("Unknown regeneration target: " + missing);
            }

            return Build(
                project,
                RegenerationWorkScope.Subset,
                requested.OrderBy(x => x, StringComparer.OrdinalIgnoreCase),
                candidates);
        }

        private RegenerationWorkProfile Build(
            ProjectState project,
            RegenerationWorkScope scope,
            IEnumerable<string> targetElementIds,
            IEnumerable<ProjectElement> candidates)
        {
            var sourceVersion = project.ChangeVersion;
            _graph.Rebuild(project.Elements);
            var order = _graph.TopologicalDirtyOrder(candidates);
            var plannedIds = new HashSet<string>(order.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var depthById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var dependencyCountById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var dependentCountById = order.ToDictionary(x => x.Id, _ => 0, StringComparer.OrdinalIgnoreCase);
            var edges = 0;
            var maxDepth = 0;

            foreach (var element in order)
            {
                var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var rawDependencyId in element.DependsOn)
                {
                    var dependencyId = (rawDependencyId ?? string.Empty).Trim();
                    if (dependencyId.Length == 0 || !plannedIds.Contains(dependencyId)) continue;
                    dependencies.Add(dependencyId);
                }

                var depth = 0;
                foreach (var dependencyId in dependencies)
                {
                    if (!depthById.TryGetValue(dependencyId, out var dependencyDepth))
                        throw new InvalidOperationException("Regeneration work order contains a dependency after its dependent: " + dependencyId + " -> " + element.Id + ".");
                    depth = Math.Max(depth, checked(dependencyDepth + 1));
                    dependentCountById[dependencyId] = checked(dependentCountById[dependencyId] + 1);
                }

                depthById[element.Id] = depth;
                dependencyCountById[element.Id] = dependencies.Count;
                edges = checked(edges + dependencies.Count);
                maxDepth = Math.Max(maxDepth, depth);
            }

            var items = new List<RegenerationWorkItem>(order.Count);
            for (var index = 0; index < order.Count; index++)
            {
                var element = order[index];
                items.Add(new RegenerationWorkItem(
                    index,
                    element.Id,
                    element.Category,
                    element.Dirty,
                    depthById[element.Id],
                    dependencyCountById[element.Id],
                    dependentCountById[element.Id]));
            }

            var categories = items
                .GroupBy(x => x.Category)
                .OrderBy(x => x.Key)
                .Select(x => new RegenerationCategoryWork(x.Key, x.Count(), x.Count(y => y.HasSemanticDirtyWork)))
                .ToList();

            var projectElementCount = 0;
            var dirtyProjectElementCount = 0;
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element entry.");
                if (!seenIds.Add(element.Id)) throw new InvalidOperationException("Project contains duplicate element id: " + element.Id);
                projectElementCount++;
                if (element.Dirty != ElementDirtyFlags.None) dirtyProjectElementCount++;
            }

            if (project.ChangeVersion != sourceVersion)
                throw new InvalidOperationException("Project changed while regeneration work was being profiled. Re-run the profile against the current semantic state.");

            return new RegenerationWorkProfile(
                project.ProjectId,
                sourceVersion,
                scope,
                targetElementIds,
                projectElementCount,
                dirtyProjectElementCount,
                items,
                categories,
                edges,
                maxDepth);
        }

        private static IReadOnlyList<string> CanonicalTargetIds(IEnumerable<string> elementIds)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var value in elementIds)
            {
                var raw = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new ArgumentException("Regeneration target id cannot be blank at index " + index.ToString(CultureInfo.InvariantCulture) + ".", nameof(elementIds));
                if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                    throw new ArgumentException("Regeneration target id must be canonical without surrounding whitespace: " + raw + ".", nameof(elementIds));
                if (!seen.Add(raw))
                    throw new ArgumentException("Duplicate regeneration target id: " + raw + ".", nameof(elementIds));
                result.Add(raw);
                index++;
            }
            return result.AsReadOnly();
        }
    }
}
