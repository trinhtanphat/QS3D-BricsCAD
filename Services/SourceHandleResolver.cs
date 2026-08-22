using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public static class SourceHandleResolver
    {
        private const int MaxRootElementIdInputCount = 10000;

        public static IReadOnlyList<string> Resolve(ProjectState project, IEnumerable<string> elementIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));

            var elementIndex = BuildElementIndex(project);
            var rootElementIds = MaterializeRootElementIds(elementIds);
            var handles = new List<string>();
            var knownHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack = new Stack<string>();

            foreach (var rootElementId in rootElementIds)
            {
                stack.Push(rootElementId);
                while (stack.Count > 0)
                {
                    var elementId = stack.Pop();
                    if (!visited.Add(elementId)) continue;
                    if (!elementIndex.TryGetValue(elementId, out var element)) continue;

                    ValidateDependencies(element);
                    AddDirectHandles(element, knownHandles, handles, out var hasDirectReference);
                    var hasBoundaryReference = false;
                    if (!hasDirectReference)
                        AddBoundaryHandles(element, knownHandles, handles, out hasBoundaryReference);
                    if (!hasDirectReference && !hasBoundaryReference)
                        AddGeneratedOwnerHandles(element, knownHandles, handles);

                    if (AutoRoomLifecycle.IsRoomFinishCategory(element.Category))
                    {
                        var roomId = AutoRoomLifecycle.ResolveRoomReferenceId(project, element);
                        if (roomId.Length > 0 && !visited.Contains(roomId)) stack.Push(roomId);
                    }

                    for (var index = element.DependsOn.Count - 1; index >= 0; index--)
                    {
                        var dependency = element.DependsOn[index];
                        if (!visited.Contains(dependency)) stack.Push(dependency);
                    }
                }
            }
            return handles.AsReadOnly();
        }

        private static IReadOnlyList<string> MaterializeRootElementIds(IEnumerable<string> elementIds)
        {
            if (elementIds is ICollection<string> collection && collection.Count > MaxRootElementIdInputCount)
                throw new InvalidOperationException("Locate root selection cannot exceed " + MaxRootElementIdInputCount + " input entries.");
            if (elementIds is IReadOnlyCollection<string> readOnlyCollection && readOnlyCollection.Count > MaxRootElementIdInputCount)
                throw new InvalidOperationException("Locate root selection cannot exceed " + MaxRootElementIdInputCount + " input entries.");

            var roots = new List<string>();
            var inputCount = 0;
            foreach (var rawId in elementIds)
            {
                if (inputCount >= MaxRootElementIdInputCount)
                    throw new InvalidOperationException("Locate root selection cannot exceed " + MaxRootElementIdInputCount + " input entries.");
                inputCount++;
                if (string.IsNullOrWhiteSpace(rawId)) continue;
                roots.Add(rawId.Trim());
            }
            return roots.AsReadOnly();
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

        private static void ValidateDependencies(ProjectElement element)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < element.DependsOn.Count; index++)
            {
                var dependency = element.DependsOn[index] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(dependency))
                    throw new InvalidOperationException(
                        "Semantic element " + element.Id + " contains a blank dependency at index " + index + ". Repair semantic relations before Locate.");
                if (!string.Equals(dependency, dependency.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Semantic element " + element.Id + " contains a non-canonical dependency at index " + index + ". Repair semantic relations before Locate.");
                if (!seen.Add(dependency))
                    throw new InvalidOperationException(
                        "Semantic element " + element.Id + " contains duplicate dependency id: " + dependency + ". Repair semantic relations before Locate.");
            }
        }

        private static void AddDirectHandles(ProjectElement element, ISet<string> knownHandles, ICollection<string> handles, out bool hasDirectReference)
        {
            hasDirectReference = false;
            for (var index = 0; index < element.SourceHandles.Count; index++)
            {
                var raw = element.SourceHandles[index] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException(
                        "Semantic element " + element.Id + " contains an empty SourceHandles entry at index " + index + ". Repair source ownership before Locate.");
                if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Semantic element " + element.Id + " contains a non-canonical SourceHandles entry at index " + index + ". Repair source ownership before Locate.");

                hasDirectReference = true;
                if (knownHandles.Add(raw)) handles.Add(raw);
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

        private static void AddGeneratedOwnerHandles(ProjectElement element, ISet<string> knownHandles, ICollection<string> handles)
        {
            foreach (var entry in GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(element))
            {
                var normalized = (entry.Key ?? string.Empty).Trim();
                if (normalized.Length > 0 && knownHandles.Add(normalized)) handles.Add(normalized);
            }
        }
    }
}
