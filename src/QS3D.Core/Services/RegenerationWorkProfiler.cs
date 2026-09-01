using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public enum RegenerationWorkScope
    {
        Project,
        Subset
    }

    internal static class RegenerationWorkIdentityContract
    {
        internal static string Require(string value, string parameterName, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(label + " is required.", parameterName);

            var trimmed = value.Trim();
            for (var i = 0; i < trimmed.Length; i++)
            {
                if (char.IsControl(trimmed[i]))
                    throw new ArgumentException(label + " must not contain control characters.", parameterName);
            }

            try
            {
                XmlConvert.VerifyXmlChars(trimmed);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException(label + " contains malformed UTF-16 or XML-invalid characters.", parameterName, ex);
            }

            return trimmed;
        }
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
            ElementId = RegenerationWorkIdentityContract.Require(
                elementId,
                nameof(elementId),
                "Regeneration work item element id");
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
            ProjectId = RegenerationWorkIdentityContract.Require(projectId, nameof(projectId), "Project id");
            if (sourceChangeVersion < 0L) throw new ArgumentOutOfRangeException(nameof(sourceChangeVersion));
            if (!Enum.IsDefined(typeof(RegenerationWorkScope), scope)) throw new ArgumentOutOfRangeException(nameof(scope));
            if (projectElementCount < 0) throw new ArgumentOutOfRangeException(nameof(projectElementCount));
            if (dirtyProjectElementCount < 0 || dirtyProjectElementCount > projectElementCount)
                throw new ArgumentOutOfRangeException(nameof(dirtyProjectElementCount));
            if (internalDependencyEdgeCount < 0) throw new ArgumentOutOfRangeException(nameof(internalDependencyEdgeCount));
            if (maxDependencyDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDependencyDepth));

            SourceChangeVersion = sourceChangeVersion;
            Scope = scope;
            TargetElementIds = MaterializeBounded(targetElementIds, projectElementCount, nameof(targetElementIds), "target element");
            ProjectElementCount = projectElementCount;
            DirtyProjectElementCount = dirtyProjectElementCount;
            Items = MaterializeBounded(items, projectElementCount, nameof(items), "work item");
            Categories = MaterializeBounded(categories, projectElementCount, nameof(categories), "category");
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

        private static IReadOnlyList<T> MaterializeBounded<T>(IEnumerable<T> values, int maxCount, string parameterName, string label)
        {
            if (values == null) throw new ArgumentNullException(parameterName);
            var knownCount = ValidateKnownCountContract(values, maxCount, parameterName, label);
            var result = new List<T>();
            using (var enumerator = values.GetEnumerator())
            {
                RequireStableKnownCountContract(values, knownCount, maxCount, parameterName, label);
                while (true)
                {
                    if (!enumerator.MoveNext()) break;
                    RequireStableKnownCountContract(values, knownCount, maxCount, parameterName, label);

                    if (knownCount.HasValue && result.Count >= knownCount.Value)
                        throw CountMismatch(knownCount.Value, result.Count + 1, parameterName, label);
                    if (result.Count >= maxCount)
                        throw CollectionTooLarge(maxCount, parameterName, label);

                    var value = enumerator.Current;
                    RequireStableKnownCountContract(values, knownCount, maxCount, parameterName, label);
                    if (ReferenceEquals(value, null))
                        throw new ArgumentException("Regeneration work profile " + label + " collection cannot contain null entries.", parameterName);
                    result.Add(value);
                }
            }
            if (knownCount.HasValue && result.Count != knownCount.Value)
                throw CountMismatch(knownCount.Value, result.Count, parameterName, label);

            RequireStableKnownCountContract(values, knownCount, maxCount, parameterName, label);
            return result.AsReadOnly();
        }

        private static void RequireStableKnownCountContract<T>(
            IEnumerable<T> values,
            int? knownCount,
            int maxCount,
            string parameterName,
            string label)
        {
            var observedKnownCount = ValidateKnownCountContract(values, maxCount, parameterName, label);
            if (observedKnownCount != knownCount)
                throw new ArgumentException(
                    "Regeneration work profile " + label + " collection known Count changed during traversal.",
                    parameterName);
        }

        private static int? ValidateKnownCountContract<T>(IEnumerable<T> values, int maxCount, string parameterName, string label)
        {
            int? knownCount = null;
            MergeKnownCount(values is ICollection<T> collection ? collection.Count : (int?)null, maxCount, parameterName, label, ref knownCount);
            MergeKnownCount(values is IReadOnlyCollection<T> readOnlyCollection ? readOnlyCollection.Count : (int?)null, maxCount, parameterName, label, ref knownCount);
            MergeKnownCount(values is System.Collections.ICollection nonGenericCollection ? nonGenericCollection.Count : (int?)null, maxCount, parameterName, label, ref knownCount);
            return knownCount;
        }

        private static void MergeKnownCount(
            int? candidate,
            int maxCount,
            string parameterName,
            string label,
            ref int? knownCount)
        {
            if (!candidate.HasValue) return;
            if (candidate.Value < 0)
                throw new ArgumentException(
                    "Regeneration work profile " + label + " collection reports an invalid negative known Count.",
                    parameterName);
            if (candidate.Value > maxCount)
                throw CollectionTooLarge(maxCount, parameterName, label);
            if (knownCount.HasValue && knownCount.Value != candidate.Value)
                throw new ArgumentException(
                    "Regeneration work profile " + label + " collection reports conflicting known Counts.",
                    parameterName);
            knownCount = candidate.Value;
        }

        private static ArgumentException CountMismatch(int knownCount, int observedCount, string parameterName, string label)
        {
            return new ArgumentException(
                "Regeneration work profile " + label + " collection known Count reported " +
                knownCount.ToString(CultureInfo.InvariantCulture) + " entries but traversal produced " +
                observedCount.ToString(CultureInfo.InvariantCulture) + ".",
                parameterName);
        }

        private static ArgumentException CollectionTooLarge(int maxCount, string parameterName, string label)
        {
            return new ArgumentException(
                "Regeneration work profile " + label + " collection cannot exceed project element count of " +
                maxCount.ToString(CultureInfo.InvariantCulture) + ".",
                parameterName);
        }
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

            var elementOwnership = SnapshotElementOwnership(project);
            var inputVersion = project.ChangeVersion;
            var sourceElementCount = project.Elements.Count;
            var requested = CanonicalTargetIds(elementIds, sourceElementCount);
            if (project.ChangeVersion != inputVersion)
                throw new InvalidOperationException("Project changed while regeneration profile target ids were being materialized. Re-run the profile against the current semantic state.");
            RequireElementOwnershipUnchanged(project, elementOwnership);
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

        private static IReadOnlyDictionary<string, ProjectElement> SnapshotElementOwnership(ProjectState project)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null semantic element entry.");
                if (result.ContainsKey(element.Id))
                    throw new InvalidOperationException("Project contains duplicate element id: " + element.Id);
                result.Add(element.Id, element);
            }
            return result;
        }

        private static void RequireElementOwnershipUnchanged(
            ProjectState project,
            IReadOnlyDictionary<string, ProjectElement> expected)
        {
            if (project.Elements.Count != expected.Count)
                throw new InvalidOperationException("Project element ownership changed while regeneration profile target ids were being materialized. Re-run the profile against the current semantic state.");

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null || !seen.Add(element.Id) ||
                    !expected.TryGetValue(element.Id, out var original) ||
                    !ReferenceEquals(original, element))
                    throw new InvalidOperationException("Project element ownership changed while regeneration profile target ids were being materialized. Re-run the profile against the current semantic state.");
            }
        }

        private static IReadOnlyList<string> CanonicalTargetIds(IEnumerable<string> elementIds, int maxCount)
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
                if (seen.Contains(raw))
                    throw new ArgumentException("Duplicate regeneration target id: " + raw + ".", nameof(elementIds));
                if (result.Count >= maxCount)
                    throw new ArgumentException("Regeneration profile target set cannot exceed project element count of " + maxCount.ToString(CultureInfo.InvariantCulture) + ".", nameof(elementIds));
                seen.Add(raw);
                result.Add(raw);
                index++;
            }
            return result.AsReadOnly();
        }
    }
}
