using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class DependencyImpactEntry
    {
        internal DependencyImpactEntry(string elementId, ElementCategory category, int depth, string causeElementId, string rootElementId)
        {
            ElementId = elementId ?? string.Empty;
            Category = category;
            Depth = depth;
            CauseElementId = causeElementId ?? string.Empty;
            RootElementId = rootElementId ?? string.Empty;
        }

        public string ElementId { get; }
        public ElementCategory Category { get; }
        public int Depth { get; }
        public string CauseElementId { get; }
        public string RootElementId { get; }
        public bool IsDirect => Depth == 1;
    }

    public sealed class DependencyImpactPlan
    {
        internal DependencyImpactPlan(string projectId, long sourceChangeVersion, IEnumerable<string> rootElementIds, IEnumerable<DependencyImpactEntry> entries)
        {
            ProjectId = projectId ?? string.Empty;
            SourceChangeVersion = sourceChangeVersion;
            RootElementIds = (rootElementIds ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
            Entries = (entries ?? Enumerable.Empty<DependencyImpactEntry>()).ToList().AsReadOnly();
        }

        public string ProjectId { get; }
        public long SourceChangeVersion { get; }
        public IReadOnlyList<string> RootElementIds { get; }
        public IReadOnlyList<DependencyImpactEntry> Entries { get; }
        public int DirectCount => Entries.Count(x => x.IsDirect);
        public int TotalCount => Entries.Count;
        public int MaxDepth => Entries.Count == 0 ? 0 : Entries.Max(x => x.Depth);
        public bool HasImpact => Entries.Count > 0;
    }

    public sealed class DependencyImpactPlanner
    {
        public DependencyImpactPlan Plan(ProjectState project, IEnumerable<string> sourceElementIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (sourceElementIds == null) throw new ArgumentNullException(nameof(sourceElementIds));
            var sourceChangeVersion = project.ChangeVersion;
            var sourceElementOwnership = SnapshotElementOwnership(project);
            var sourceDependencyTopology = SnapshotDependencyTopology(sourceElementOwnership);
            var requestedRoots = CanonicalRoots(sourceElementIds, sourceElementOwnership.Count);
            RequireProjectFresh(project, sourceChangeVersion, sourceElementOwnership);
            RequireDependencyTopologyFresh(project, sourceDependencyTopology);

            var graph = new DependencyGraph();
            graph.Rebuild(project.Elements);

            var roots = new List<string>(requestedRoots.Count);
            foreach (var requested in requestedRoots)
            {
                if (!graph.TryGetElement(requested, out var resolved))
                    throw new InvalidOperationException("Dependency impact source element does not exist: " + requested + ".");
                if (resolved == null || string.IsNullOrWhiteSpace(resolved.Id))
                    throw new InvalidOperationException("Dependency impact source resolved to an invalid semantic element: " + requested + ".");
                roots.Add(resolved.Id);
            }
            roots.Sort(StringComparer.OrdinalIgnoreCase);

            var rootSet = new HashSet<string>(roots, StringComparer.OrdinalIgnoreCase);
            var visited = new Dictionary<string, WalkState>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<WalkState>();
            foreach (var root in roots)
            {
                var state = new WalkState(root, root, string.Empty, 0);
                visited[root] = state;
                queue.Enqueue(state);
            }

            var entries = new List<DependencyImpactEntry>();
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var dependentId in graph.GetDirectDependents(current.ElementId))
                {
                    if (rootSet.Contains(dependentId) || visited.ContainsKey(dependentId)) continue;
                    if (!graph.TryGetElement(dependentId, out var dependent) || dependent == null)
                        throw new InvalidOperationException("Dependency impact graph contains an unresolved dependent element: " + dependentId + ".");

                    var next = new WalkState(dependent.Id, current.RootElementId, current.ElementId, current.Depth + 1);
                    visited[dependent.Id] = next;
                    queue.Enqueue(next);
                    entries.Add(new DependencyImpactEntry(dependent.Id, dependent.Category, next.Depth, next.CauseElementId, next.RootElementId));
                }
            }

            RequireProjectFresh(project, sourceChangeVersion, sourceElementOwnership);
            RequireDependencyTopologyFresh(project, sourceDependencyTopology);

            var ordered = entries
                .OrderBy(x => x.Depth)
                .ThenBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new DependencyImpactPlan(project.ProjectId, sourceChangeVersion, roots, ordered);
        }

        private static IReadOnlyDictionary<string, ProjectElement> SnapshotElementOwnership(ProjectState project)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null semantic element entry while dependency impact is being planned.");
                if (result.ContainsKey(element.Id))
                    throw new InvalidOperationException("Project contains duplicate semantic element id while dependency impact is being planned: " + element.Id + ".");
                result.Add(element.Id, element);
            }
            return result;
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> SnapshotDependencyTopology(
            IReadOnlyDictionary<string, ProjectElement> ownership)
        {
            var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in ownership)
            {
                var dependencies = pair.Value.DependsOn ?? new List<string>();
                result.Add(pair.Key, dependencies.ToArray());
            }
            return result;
        }

        private static void RequireProjectFresh(
            ProjectState project,
            long expectedChangeVersion,
            IReadOnlyDictionary<string, ProjectElement> expectedOwnership)
        {
            if (project.ChangeVersion != expectedChangeVersion)
                throw new InvalidOperationException("Project changed while dependency impact was being planned; recompute the impact plan.");
            if (project.Elements.Count != expectedOwnership.Count)
                throw StructuralFreshnessError();

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null || !seen.Add(element.Id) ||
                    !expectedOwnership.TryGetValue(element.Id, out var original) ||
                    !ReferenceEquals(original, element))
                    throw StructuralFreshnessError();
            }
        }

        private static void RequireDependencyTopologyFresh(
            ProjectState project,
            IReadOnlyDictionary<string, IReadOnlyList<string>> expectedDependencyTopology)
        {
            foreach (var element in project.Elements)
            {
                if (element == null ||
                    !expectedDependencyTopology.TryGetValue(element.Id, out var expectedDependencies) ||
                    !DependencyTopologyMatches(element.DependsOn, expectedDependencies))
                    throw DependencyTopologyFreshnessError();
            }
        }

        private static bool DependencyTopologyMatches(IList<string> current, IReadOnlyList<string> expected)
        {
            if (current == null) return expected == null || expected.Count == 0;
            if (expected == null || current.Count != expected.Count) return false;
            for (var index = 0; index < current.Count; index++)
            {
                if (!string.Equals(current[index], expected[index], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static InvalidOperationException StructuralFreshnessError()
        {
            return new InvalidOperationException(
                "Project element ownership changed while dependency impact was being planned; recompute the impact plan.");
        }

        private static InvalidOperationException DependencyTopologyFreshnessError()
        {
            return new InvalidOperationException(
                "Project dependency topology changed while dependency impact was being planned; recompute the impact plan.");
        }

        private static IReadOnlyList<string> CanonicalRoots(IEnumerable<string> sourceElementIds, int maxRootCount)
        {
            if (sourceElementIds == null) throw new ArgumentNullException(nameof(sourceElementIds));
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var value in sourceElementIds)
            {
                if (index >= maxRootCount)
                    throw new ArgumentException("Dependency impact source count cannot exceed project semantic element count of " + maxRootCount.ToString(CultureInfo.InvariantCulture) + ".", nameof(sourceElementIds));
                var raw = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new ArgumentException("Dependency impact source id cannot be blank at index " + index.ToString(CultureInfo.InvariantCulture) + ".", nameof(sourceElementIds));
                if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                    throw new ArgumentException("Dependency impact source id must be canonical without surrounding whitespace: " + raw + ".", nameof(sourceElementIds));
                if (!seen.Add(raw))
                    throw new ArgumentException("Duplicate dependency impact source id: " + raw + ".", nameof(sourceElementIds));
                result.Add(raw);
                index++;
            }
            if (result.Count == 0)
                throw new ArgumentException("Dependency impact planning requires at least one source element id.", nameof(sourceElementIds));
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result.AsReadOnly();
        }

        private sealed class WalkState
        {
            public WalkState(string elementId, string rootElementId, string causeElementId, int depth)
            {
                ElementId = elementId;
                RootElementId = rootElementId;
                CauseElementId = causeElementId;
                Depth = depth;
            }

            public string ElementId { get; }
            public string RootElementId { get; }
            public string CauseElementId { get; }
            public int Depth { get; }
        }
    }
}
