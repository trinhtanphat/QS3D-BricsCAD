using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class DependencyHealthService
    {
        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            var elements = project.Elements.Where(x => x != null).ToList();
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in elements)
            {
                if (!counts.TryGetValue(element.Id, out var count)) count = 0;
                counts[element.Id] = count + 1;
            }
            var duplicateIds = new HashSet<string>(counts.Where(x => x.Value > 1).Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            var uniqueIds = new HashSet<string>(counts.Where(x => x.Value == 1).Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            var graph = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            var selfReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ambiguousTargets = new List<KeyValuePair<string, string>>();
            var missingTargets = new List<KeyValuePair<string, string>>();

            foreach (var element in elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (duplicateIds.Contains(element.Id) || graph.ContainsKey(element.Id)) continue;
                var dependencies = new List<string>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var raw in element.DependsOn)
                {
                    var dependencyId = (raw ?? string.Empty).Trim();
                    if (dependencyId.Length == 0 || !seen.Add(dependencyId)) continue;
                    if (string.Equals(dependencyId, element.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        selfReferences.Add(element.Id);
                        continue;
                    }
                    if (duplicateIds.Contains(dependencyId))
                    {
                        ambiguousTargets.Add(new KeyValuePair<string, string>(element.Id, dependencyId));
                        continue;
                    }
                    if (uniqueIds.Contains(dependencyId))
                    {
                        dependencies.Add(dependencyId);
                        continue;
                    }
                    missingTargets.Add(new KeyValuePair<string, string>(element.Id, dependencyId));
                }
                dependencies.Sort(StringComparer.OrdinalIgnoreCase);
                graph[element.Id] = dependencies.ToArray();
            }

            var cycleMembers = FindCycleMembers(graph);
            var issues = new List<ModelHealthIssue>();
            foreach (var pair in ambiguousTargets
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Value, StringComparer.OrdinalIgnoreCase))
            {
                issues.Add(new ModelHealthIssue(
                    "DEPENDENCY_TARGET_AMBIGUOUS",
                    HealthSeverity.Error,
                    "Dependency trỏ tới mã semantic element bị trùng: " + pair.Value + ". Không thể xác định cạnh graph an toàn.",
                    pair.Key));
            }

            foreach (var pair in missingTargets
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Value, StringComparer.OrdinalIgnoreCase))
            {
                issues.Add(new ModelHealthIssue(
                    "DEPENDENCY_TARGET_MISSING",
                    HealthSeverity.Error,
                    "Dependency trỏ tới semantic element không tồn tại: " + pair.Value + ". Cần sửa dependency trước khi regenerate/release.",
                    pair.Key));
            }

            foreach (var elementId in selfReferences.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                issues.Add(new ModelHealthIssue(
                    "DEPENDENCY_SELF_REFERENCE",
                    HealthSeverity.Error,
                    "Element không được phụ thuộc vào chính nó; regeneration không thể tạo thứ tự hợp lệ.",
                    elementId));

            foreach (var elementId in cycleMembers.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                issues.Add(new ModelHealthIssue(
                    "DEPENDENCY_CYCLE",
                    HealthSeverity.Error,
                    "Element nằm trong vòng phụ thuộc semantic; cần phá vòng trước khi regenerate/release.",
                    elementId));

            return issues.AsReadOnly();
        }

        private static HashSet<string> FindCycleMembers(IReadOnlyDictionary<string, string[]> graph)
        {
            var state = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            var activePath = new List<string>();
            var activeIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var cycleMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var start in graph.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                if (state.TryGetValue(start, out var existing) && existing != 0) continue;

                var stack = new List<Frame>();
                Push(start, graph, state, activePath, activeIndex, stack);
                while (stack.Count > 0)
                {
                    var frameIndex = stack.Count - 1;
                    var frame = stack[frameIndex];
                    if (frame.NextDependency >= frame.Dependencies.Length)
                    {
                        stack.RemoveAt(frameIndex);
                        state[frame.ElementId] = 2;
                        activeIndex.Remove(frame.ElementId);
                        if (activePath.Count == 0 || !string.Equals(activePath[activePath.Count - 1], frame.ElementId, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Dependency health traversal stack became inconsistent.");
                        activePath.RemoveAt(activePath.Count - 1);
                        continue;
                    }

                    var dependencyId = frame.Dependencies[frame.NextDependency];
                    frame.NextDependency++;
                    stack[frameIndex] = frame;

                    state.TryGetValue(dependencyId, out var dependencyState);
                    if (dependencyState == 0)
                    {
                        Push(dependencyId, graph, state, activePath, activeIndex, stack);
                        continue;
                    }
                    if (dependencyState != 1) continue;
                    if (!activeIndex.TryGetValue(dependencyId, out var cycleStart))
                        throw new InvalidOperationException("Dependency health active-path index became inconsistent.");
                    for (var index = cycleStart; index < activePath.Count; index++)
                        cycleMembers.Add(activePath[index]);
                }
            }

            return cycleMembers;
        }

        private static void Push(
            string elementId,
            IReadOnlyDictionary<string, string[]> graph,
            IDictionary<string, byte> state,
            IList<string> activePath,
            IDictionary<string, int> activeIndex,
            IList<Frame> stack)
        {
            state[elementId] = 1;
            activeIndex[elementId] = activePath.Count;
            activePath.Add(elementId);
            stack.Add(new Frame(elementId, graph[elementId]));
        }

        private struct Frame
        {
            public Frame(string elementId, string[] dependencies)
            {
                ElementId = elementId;
                Dependencies = dependencies;
                NextDependency = 0;
            }

            public string ElementId;
            public string[] Dependencies;
            public int NextDependency;
        }
    }
}
