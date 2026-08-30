using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class DependencyGraph
    {
        private const int MaxElementInputCount = 10000;

        private sealed class OrderingSnapshot
        {
            public OrderingSnapshot(ProjectElement element, string id, ElementDirtyFlags dirty, string[] dependencies)
            {
                Element = element;
                Id = id;
                Dirty = dirty;
                Dependencies = dependencies;
            }

            public ProjectElement Element { get; }
            public string Id { get; }
            public ElementDirtyFlags Dirty { get; }
            public IReadOnlyList<string> Dependencies { get; }
        }

        private sealed class VisitFrame
        {
            public VisitFrame(OrderingSnapshot snapshot) { Snapshot = snapshot; }
            public OrderingSnapshot Snapshot { get; }
            public int NextDependencyIndex { get; set; }
        }

        private readonly Dictionary<string, HashSet<string>> _dependents = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ProjectElement> _elementsById = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
        private long _rebuildVersion;

        public void Rebuild(IEnumerable<ProjectElement> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var knownCount = RejectKnownOversizedInput(elements, "Dependency graph rebuild", out var knownCountSources);

            var next = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var nextElements = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            var processedDependencies = new List<KeyValuePair<ProjectElement, HashSet<string>>>();
            var enumerationVersion = _rebuildVersion;
            var elementCount = 0;
            using (var enumerator = elements.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency graph rebuild");
                    if (!enumerator.MoveNext())
                    {
                        RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency graph rebuild");
                        break;
                    }
                    RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency graph rebuild");
                    RequireTraversalCapacity(knownCount, elementCount, "Dependency graph rebuild");
                    if (elementCount >= MaxElementInputCount)
                        throw new InvalidOperationException("Dependency graph rebuild exceeds the supported " + MaxElementInputCount + " element limit.");
                    var element = enumerator.Current;
                    elementCount++;
                    if (element == null) throw new InvalidOperationException("Dependency graph cannot contain a null semantic element.");
                    if (nextElements.ContainsKey(element.Id))
                        throw new InvalidOperationException("Dependency graph contains duplicate semantic element id: " + element.Id);
                    nextElements.Add(element.Id, element);

                    ValidateDependencies(element);
                    var dependencySnapshot = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var source in element.DependsOn)
                    {
                        dependencySnapshot.Add(source);
                        if (!next.TryGetValue(source, out var set))
                        {
                            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            next[source] = set;
                        }
                        set.Add(element.Id);
                    }
                    processedDependencies.Add(new KeyValuePair<ProjectElement, HashSet<string>>(element, dependencySnapshot));
                }
            }

            RequireObservedCount(knownCount, elementCount, "Dependency graph rebuild");
            RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency graph rebuild");

            foreach (var processed in processedDependencies)
            {
                ValidateDependencies(processed.Key);
                if (processed.Key.DependsOn.Count != processed.Value.Count ||
                    processed.Key.DependsOn.Any(dependency => !processed.Value.Contains(dependency)))
                    throw new InvalidOperationException(
                        "Dependency graph input changed after semantic element " + processed.Key.Id + " was processed. Retry rebuild against stable dependency input.");
            }

            foreach (var entry in next)
            {
                if (nextElements.ContainsKey(entry.Key)) continue;
                var dependent = entry.Value.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).First();
                throw new InvalidOperationException(
                    "Semantic element " + dependent + " depends on missing semantic element: " + entry.Key + ". Repair semantic relations before graph evaluation.");
            }

            if (_rebuildVersion != enumerationVersion)
                throw new InvalidOperationException("Dependency graph changed while rebuild elements were being enumerated. Retry rebuild against the current graph state.");

            var nextVersion = checked(_rebuildVersion + 1L);
            _dependents.Clear();
            foreach (var entry in next)
                _dependents[entry.Key] = entry.Value;
            _elementsById.Clear();
            foreach (var entry in nextElements)
                _elementsById[entry.Key] = entry.Value;
            _rebuildVersion = nextVersion;
        }

        public bool TryGetElement(string elementId, out ProjectElement? element)
        {
            var normalized = (elementId ?? string.Empty).Trim();
            if (normalized.Length == 0)
            {
                element = null;
                return false;
            }
            return _elementsById.TryGetValue(normalized, out element);
        }

        public IReadOnlyList<string> GetDirectDependents(string sourceId)
        {
            var normalized = (sourceId ?? string.Empty).Trim();
            if (normalized.Length == 0 || !_dependents.TryGetValue(normalized, out var dependents))
                return Array.Empty<string>();
            return dependents.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        public IReadOnlyList<string> GetDependentsTransitive(string sourceId)
        {
            var normalizedSourceId = (sourceId ?? string.Empty).Trim();
            if (normalizedSourceId.Length == 0) return Array.Empty<string>();

            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>();
            queue.Enqueue(normalizedSourceId);
            seen.Add(normalizedSourceId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!_dependents.TryGetValue(current, out var next)) continue;
                foreach (var id in next.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    if (!seen.Add(id)) continue;
                    result.Add(id);
                    queue.Enqueue(id);
                }
            }
            return result.AsReadOnly();
        }

        public IReadOnlyList<ProjectElement> TopologicalDirtyOrder(IEnumerable<ProjectElement> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var knownCount = RejectKnownOversizedInput(elements, "Dependency ordering", out var knownCountSources);
            var materialized = new List<ProjectElement>();
            var snapshots = new List<OrderingSnapshot>();
            using (var enumerator = elements.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency ordering");
                    if (!enumerator.MoveNext())
                    {
                        RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency ordering");
                        break;
                    }
                    RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency ordering");
                    RequireTraversalCapacity(knownCount, materialized.Count, "Dependency ordering");
                    if (materialized.Count >= MaxElementInputCount)
                        throw new InvalidOperationException("Dependency ordering exceeds the supported " + MaxElementInputCount + " element limit.");
                    var element = enumerator.Current;
                    if (element == null) throw new InvalidOperationException("Dependency ordering cannot contain a null semantic element.");
                    ValidateDependencies(element);
                    materialized.Add(element);
                    snapshots.Add(CaptureOrderingSnapshot(element));
                }
            }

            RequireObservedCount(knownCount, materialized.Count, "Dependency ordering");
            RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency ordering");
            foreach (var snapshot in snapshots)
                RequireStableOrderingSnapshot(snapshot);

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<OrderingSnapshot>();
            foreach (var snapshot in snapshots)
            {
                if (!seenIds.Add(snapshot.Id))
                    throw new InvalidOperationException("Dependency ordering contains duplicate semantic element id: " + snapshot.Id);
                if (snapshot.Dirty != ElementDirtyFlags.None) list.Add(snapshot);
            }

            var byId = new Dictionary<string, OrderingSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (var snapshot in list)
                byId[snapshot.Id] = snapshot;

            var result = new List<ProjectElement>(list.Count);
            var temporary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var permanent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack = new Stack<VisitFrame>();

            foreach (var root in list)
            {
                if (permanent.Contains(root.Id)) continue;
                temporary.Add(root.Id);
                stack.Push(new VisitFrame(root));

                while (stack.Count > 0)
                {
                    var frame = stack.Peek();
                    if (frame.NextDependencyIndex < frame.Snapshot.Dependencies.Count)
                    {
                        var dependencyId = frame.Snapshot.Dependencies[frame.NextDependencyIndex++];
                        if (!byId.TryGetValue(dependencyId, out var dependency) || permanent.Contains(dependency.Id)) continue;
                        if (!temporary.Add(dependency.Id))
                            throw new InvalidOperationException("Dependency cycle detected at " + dependency.Id + ".");
                        stack.Push(new VisitFrame(dependency));
                        continue;
                    }

                    stack.Pop();
                    temporary.Remove(frame.Snapshot.Id);
                    if (permanent.Add(frame.Snapshot.Id)) result.Add(frame.Snapshot.Element);
                }
            }

            foreach (var snapshot in snapshots)
                RequireStableOrderingSnapshot(snapshot);
            return result.AsReadOnly();
        }

        private static OrderingSnapshot CaptureOrderingSnapshot(ProjectElement element)
        {
            return new OrderingSnapshot(
                element,
                element.Id,
                element.Dirty,
                element.DependsOn.ToArray());
        }

        private static void RequireStableOrderingSnapshot(OrderingSnapshot snapshot)
        {
            var element = snapshot.Element;
            if (!string.Equals(element.Id, snapshot.Id, StringComparison.Ordinal) ||
                element.Dirty != snapshot.Dirty ||
                element.DependsOn.Count != snapshot.Dependencies.Count)
                throw OrderingSnapshotChanged(snapshot.Id);

            for (var index = 0; index < snapshot.Dependencies.Count; index++)
            {
                if (!string.Equals(element.DependsOn[index], snapshot.Dependencies[index], StringComparison.Ordinal))
                    throw OrderingSnapshotChanged(snapshot.Id);
            }
        }

        private static InvalidOperationException OrderingSnapshotChanged(string elementId)
        {
            return new InvalidOperationException(
                "Dependency ordering input changed after semantic element " + elementId + " was admitted. Retry ordering against stable semantic input.");
        }

        private static int? RejectKnownOversizedInput(IEnumerable<ProjectElement> elements, string operation, out int knownCountSources)
        {
            var genericCount = elements is ICollection<ProjectElement> collection ? (int?)collection.Count : null;
            var readOnlyCount = elements is IReadOnlyCollection<ProjectElement> readOnlyCollection ? (int?)readOnlyCollection.Count : null;
            var nonGenericCount = elements is System.Collections.ICollection nonGenericCollection ? (int?)nonGenericCollection.Count : null;

            ValidateKnownCount(genericCount, operation);
            ValidateKnownCount(readOnlyCount, operation);
            ValidateKnownCount(nonGenericCount, operation);

            var sources = 0;
            if (genericCount.HasValue) sources |= 1;
            if (readOnlyCount.HasValue) sources |= 2;
            if (nonGenericCount.HasValue) sources |= 4;
            knownCountSources = sources;

            var expected = genericCount ?? readOnlyCount ?? nonGenericCount;
            if (!expected.HasValue) return null;
            if ((genericCount.HasValue && genericCount.Value != expected.Value) ||
                (readOnlyCount.HasValue && readOnlyCount.Value != expected.Value) ||
                (nonGenericCount.HasValue && nonGenericCount.Value != expected.Value))
                throw new InvalidOperationException(operation + " reports conflicting known element counts.");
            return expected;
        }

        private static void RequireStableKnownCount(
            IEnumerable<ProjectElement> elements,
            int? initialKnownCount,
            int initialKnownCountSources,
            string operation)
        {
            var currentKnownCount = RejectKnownOversizedInput(elements, operation, out var currentKnownCountSources);
            if (currentKnownCount != initialKnownCount || currentKnownCountSources != initialKnownCountSources)
                throw TraversalCountError(operation);
        }

        private static void RequireTraversalCapacity(int? knownCount, int observedCount, string operation)
        {
            if (knownCount.HasValue && observedCount >= knownCount.Value)
                throw TraversalCountError(operation);
        }

        private static void RequireObservedCount(int? knownCount, int observedCount, string operation)
        {
            if (knownCount.HasValue && knownCount.Value != observedCount)
                throw TraversalCountError(operation);
        }

        private static InvalidOperationException TraversalCountError(string operation)
        {
            return new InvalidOperationException(operation + " element count changed during enumeration.");
        }

        private static void ValidateKnownCount(int? count, string operation)
        {
            if (!count.HasValue) return;
            if (count.Value < 0)
                throw new InvalidOperationException(operation + " reports an invalid negative element count.");
            if (count.Value > MaxElementInputCount)
                throw new InvalidOperationException(operation + " exceeds the supported " + MaxElementInputCount + " element limit.");
        }

        private static void ValidateDependencies(ProjectElement element)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < element.DependsOn.Count; index++)
            {
                var dependency = element.DependsOn[index] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(dependency))
                    throw new InvalidOperationException(
                        "Semantic element " + element.Id + " contains a blank dependency at index " + index + ". Repair semantic relations before graph evaluation.");
                if (!string.Equals(dependency, dependency.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Semantic element " + element.Id + " contains a non-canonical dependency at index " + index + ". Repair semantic relations before graph evaluation.");
                if (string.Equals(dependency, element.Id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Semantic element " + element.Id + " depends on itself. Repair semantic relations before graph evaluation.");
                if (!seen.Add(dependency))
                    throw new InvalidOperationException(
                        "Semantic element " + element.Id + " contains duplicate dependency id: " + dependency + ". Repair semantic relations before graph evaluation.");
            }
        }
    }
}
