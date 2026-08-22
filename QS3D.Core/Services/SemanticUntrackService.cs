using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class SemanticUntrackResult
    {
        internal SemanticUntrackResult(IReadOnlyList<string> removedElementIds)
        {
            RemovedElementIds = removedElementIds ?? throw new ArgumentNullException(nameof(removedElementIds));
        }

        public IReadOnlyList<string> RemovedElementIds { get; }
        public int Count => RemovedElementIds.Count;
    }

    public static class SemanticUntrackService
    {
        public static SemanticUntrackResult Untrack(
            ProjectState project,
            IEnumerable<string> selectedHandles,
            Func<ProjectElement, bool>? predicate = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (selectedHandles == null) throw new ArgumentNullException(nameof(selectedHandles));

            var targets = SemanticHandleOwnershipResolver.Resolve(project, selectedHandles)
                .Where(x => predicate == null || predicate(x))
                .ToList();
            if (targets.Count == 0)
                return new SemanticUntrackResult(Array.Empty<string>());

            var targetIds = new HashSet<string>(targets.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            EnsureNoExternalDependents(project, targets, targetIds);

            return ProjectSemanticMutationExecutor.Execute(project, "semantic.untrack", () =>
            {
                foreach (var target in targets)
                {
                    if (!project.Elements.Remove(target))
                        throw new InvalidOperationException("Semantic untrack target is no longer owned by this project: " + target.Id);
                }

                project.Touch();
                return new SemanticUntrackResult(targets.Select(x => x.Id).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly());
            });
        }

        private static void EnsureNoExternalDependents(
            ProjectState project,
            IEnumerable<ProjectElement> targets,
            ISet<string> targetIds)
        {
            var graph = new DependencyGraph();
            graph.Rebuild(project.Elements);
            var blockers = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var target in targets)
            {
                foreach (var dependentId in graph.GetDependentsTransitive(target.Id))
                {
                    if (targetIds.Contains(dependentId)) continue;
                    blockers.Add(target.Id + " ← " + dependentId);
                }
            }

            if (blockers.Count == 0) return;
            var visible = blockers.Take(8).ToArray();
            var suffix = blockers.Count > visible.Length ? " … +" + (blockers.Count - visible.Length) + " more" : string.Empty;
            throw new InvalidOperationException(
                "Cannot untrack semantic element(s) while dependents remain: " + string.Join(", ", visible) + suffix +
                ". Untrack the dependent batch too, or unlink/repair the semantic relation first.");
        }
    }
}
