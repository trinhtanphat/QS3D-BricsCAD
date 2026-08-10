using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class DependencyGraph
    {
        private sealed class VisitFrame
        {
            public VisitFrame(ProjectElement element) { Element = element; }
            public ProjectElement Element { get; }
            public int NextDependencyIndex { get; set; }
        }

        private readonly Dictionary<string, HashSet<string>> _dependents = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public void Rebuild(IEnumerable<ProjectElement> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            _dependents.Clear();
            foreach (var element in elements)
            {
                if (element == null) throw new InvalidOperationException("Dependency graph cannot contain a null semantic element.");
                foreach (var source in element.DependsOn.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    if (!_dependents.TryGetValue(source, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        _dependents[source] = set;
                    }
                    set.Add(element.Id);
                }
            }
        }

        public IReadOnlyList<string> GetDependentsTransitive(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) return Array.Empty<string>();
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>();
            queue.Enqueue(sourceId);
            seen.Add(sourceId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!_dependents.TryGetValue(current, out var next)) continue;
                foreach (var id in next)
                {
                    if (!seen.Add(id)) continue;
                    result.Add(id);
                    queue.Enqueue(id);
                }
            }
            return result;
        }

        public IReadOnlyList<ProjectElement> TopologicalDirtyOrder(IEnumerable<ProjectElement> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var list = new List<ProjectElement>();
            foreach (var element in elements)
            {
                if (element == null) throw new InvalidOperationException("Dependency ordering cannot contain a null semantic element.");
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
                        var dependencyId = (frame.Element.DependsOn[frame.NextDependencyIndex++] ?? string.Empty).Trim();
                        if (dependencyId.Length == 0 || !byId.TryGetValue(dependencyId, out var dependency) || permanent.Contains(dependency.Id)) continue;
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
    }
}
