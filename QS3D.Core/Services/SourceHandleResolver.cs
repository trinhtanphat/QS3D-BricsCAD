using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public static class SourceHandleResolver
    {
        public static IReadOnlyList<string> Resolve(ProjectState project, IEnumerable<string> elementIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));

            var handles = new List<string>();
            var knownHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in elementIds.Where(x => !string.IsNullOrWhiteSpace(x)))
                Visit(project, id.Trim(), visited, knownHandles, handles);
            return handles;
        }

        private static void Visit(ProjectState project, string elementId, ISet<string> visited, ISet<string> knownHandles, ICollection<string> handles)
        {
            if (!visited.Add(elementId)) return;
            var element = project.FindElement(elementId);
            if (element == null) return;
            foreach (var handle in element.SourceHandles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
                if (knownHandles.Add(handle)) handles.Add(handle);
            foreach (var dependency in element.DependsOn.Where(x => !string.IsNullOrWhiteSpace(x)))
                Visit(project, dependency.Trim(), visited, knownHandles, handles);
        }
    }
}
