using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class DependencyGraph
    {
        private readonly Dictionary<string, HashSet<string>> _dependents = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public void Rebuild(IEnumerable<ProjectElement> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            _dependents.Clear();
            foreach (var element in elements)
            {
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
            var list = elements.Where(x => x.Dirty != ElementDirtyFlags.None).ToList();
            var byId = list.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var result = new List<ProjectElement>(list.Count);
            var temporary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var permanent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in list) Visit(element, byId, temporary, permanent, result);
            return result;
        }

        private static void Visit(ProjectElement element, IDictionary<string, ProjectElement> byId, ISet<string> temporary, ISet<string> permanent, IList<ProjectElement> result)
        {
            if (permanent.Contains(element.Id)) return;
            if (!temporary.Add(element.Id)) throw new InvalidOperationException("Dependency cycle detected at " + element.Id + ".");
            foreach (var dependencyId in element.DependsOn)
            {
                if (byId.TryGetValue(dependencyId, out var dependency)) Visit(dependency, byId, temporary, permanent, result);
            }
            temporary.Remove(element.Id);
            permanent.Add(element.Id);
            result.Add(element);
        }
    }
}
