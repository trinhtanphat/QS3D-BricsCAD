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

            var elementIndex = BuildElementIndex(project);
            var handles = new List<string>();
            var knownHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack = new Stack<string>();

            foreach (var rawId in elementIds.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                stack.Push(rawId.Trim());
                while (stack.Count > 0)
                {
                    var elementId = stack.Pop();
                    if (!visited.Add(elementId)) continue;
                    if (!elementIndex.TryGetValue(elementId, out var element)) continue;

                    AddDirectHandles(element, knownHandles, handles, out var hasDirectReference);
                    var hasBoundaryReference = false;
                    if (!hasDirectReference)
                        AddBoundaryHandles(element, knownHandles, handles, out hasBoundaryReference);
                    if (!hasDirectReference && !hasBoundaryReference)
                        AddGeneratedSolidHandle(element, knownHandles, handles);

                    if (AutoRoomLifecycle.IsRoomFinishCategory(element.Category))
                    {
                        var roomId = AutoRoomLifecycle.ResolveRoomReferenceId(project, element);
                        if (roomId.Length > 0 && !visited.Contains(roomId)) stack.Push(roomId);
                    }

                    for (var index = element.DependsOn.Count - 1; index >= 0; index--)
                    {
                        var dependency = (element.DependsOn[index] ?? string.Empty).Trim();
                        if (dependency.Length > 0 && !visited.Contains(dependency)) stack.Push(dependency);
                    }
                }
            }
            return handles.AsReadOnly();
        }

        private static IReadOnlyDictionary<string, ProjectElement> BuildElementIndex(ProjectState project)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element.");
                if (result.ContainsKey(element.Id))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + element.Id);
                result[element.Id] = element;
            }
            return result;
        }

        private static void AddDirectHandles(ProjectElement element, ISet<string> knownHandles, ICollection<string> handles, out bool hasDirectReference)
        {
            hasDirectReference = false;
            foreach (var raw in element.SourceHandles)
            {
                var handle = (raw ?? string.Empty).Trim();
                if (handle.Length == 0) continue;
                hasDirectReference = true;
                if (knownHandles.Add(handle)) handles.Add(handle);
            }
        }

        private static void AddBoundaryHandles(ProjectElement element, ISet<string> knownHandles, ICollection<string> handles, out bool hasBoundaryReference)
        {
            hasBoundaryReference = false;
            if (!element.Properties.TryGetValue(AutoRoomLifecycle.BoundarySourceHandlesKey, out var boundaryHandles)) return;
            foreach (var raw in (boundaryHandles ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var handle = raw.Trim();
                if (handle.Length == 0) continue;
                hasBoundaryReference = true;
                if (knownHandles.Add(handle)) handles.Add(handle);
            }
        }

        private static void AddGeneratedSolidHandle(ProjectElement element, ISet<string> knownHandles, ICollection<string> handles)
        {
            if (!element.Properties.TryGetValue("GeneratedSolidHandle", out var generatedHandle)) return;
            var normalized = (generatedHandle ?? string.Empty).Trim();
            if (normalized.Length > 0 && knownHandles.Add(normalized)) handles.Add(normalized);
        }
    }
}
