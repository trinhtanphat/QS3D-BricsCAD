using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class DependencyGraph
    {
        private const int MaxElementInputCount = 10000;

        private sealed class VisitFrame
        {
            public VisitFrame(ProjectElement element) { Element = element; }
            public ProjectElement Element { get; }
            public int NextDependencyIndex { get; set; }
        }

        private readonly Dictionary<string, HashSet<string>> _dependents = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ProjectElement> _elementsById = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
        private long _rebuildVersion;

        public void Rebuild(IEnumerable<ProjectElement> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            RejectKnownOversizedInput(elements, "Dependency graph rebuild");

            var next = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var nextElements = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            var processedDependencies = new List<KeyValuePair<ProjectElement, HashSet<string>>>();
            var enumerationVersion = _rebuildVersion;
            var elementCount = 0;
            foreach (var element in elements)
            {
                elementCount++;
                if (elementCount > MaxElementInputCount)
                    throw new InvalidOperationException("Dependency graph rebuild exceeds the supported " + MaxElementInputCount + " element limit.");
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
            RejectKnownOversizedInput(elements, "Dependency ordering");
            var materialized = new List<ProjectElement>();
            foreach (var element in elements)
            {
                if (materialized.Count >= MaxElementInputCount)
                    throw new InvalidOperationException("Dependency ordering exceeds the supported " + MaxElementInputCount + " element limit.");
                if (element == null) throw new InvalidOperationException("Dependency ordering cannot contain a null semantic element.");
                materialized.Add(element);
            }

            foreach (var element in materialized)
                ValidateDependencies(element);

            var list = new List<ProjectElement>();
            foreach (var element in materialized)
            {
                if (element.Dirty != ElementDirtyFlags.None) list.Add(element);
            }

            var byId = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in list)
            {
                if (byId.ContainsKey(element.Id))
                    throw new InvalidOperationException("Dependency ordering contains duplicate semantic element id: " + element.Id);
                byId[element.Id] = element;
            }

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
                    if (frame.NextDependencyIndex < frame.Element.DependsOn.Count)
                    {
                        var dependencyId = frame.Element.DependsOn[frame.NextDependencyIndex++];
                        if (!byId.TryGetValue(dependencyId, out var dependency) || permanent.Contains(dependency.Id)) continue;
                        if (!temporary.Add(dependency.Id))
                            throw new InvalidOperationException("Dependency cycle detected at " + dependency.Id + ".");
                        stack.Push(new VisitFrame(dependency));
                        continue;
                    }

                    stack.Pop();
                    temporary.Remove(frame.Element.Id);
                    if (permanent.Add(frame.Element.Id)) result.Add(frame.Element);
                }
            }
            return result.AsReadOnly();
        }

        private static void RejectKnownOversizedInput(IEnumerable<ProjectElement> elements, string operation)
        {
            var genericCount = elements is ICollection<ProjectElement> collection ? (int?)collection.Count : null;
            var readOnlyCount = elements is IReadOnlyCollection<ProjectElement> readOnlyCollection ? (int?)readOnlyCollection.Count : null;
            var nonGenericCount = elements is System.Collections.ICollection nonGenericCollection ? (int?)nonGenericCollection.Count : null;

            ValidateKnownCount(genericCount, operation);
            ValidateKnownCount(readOnlyCount, operation);
            ValidateKnownCount(nonGenericCount, operation);

            var expected = genericCount ?? readOnlyCount ?? nonGenericCount;
            if (!expected.HasValue) return;
            if ((genericCount.HasValue && genericCount.Value != expected.Value) ||
                (readOnlyCount.HasValue && readOnlyCount.Value != expected.Value) ||
                (nonGenericCount.HasValue && nonGenericCount.Value != expected.Value))
                throw new InvalidOperationException(operation + " reports conflicting known element counts.");
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
