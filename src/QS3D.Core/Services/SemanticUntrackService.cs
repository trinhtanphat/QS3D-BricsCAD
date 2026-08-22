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

            var resolved = SemanticHandleOwnershipResolver.Resolve(project, selectedHandles);
            List<ProjectElement> targets;
            if (predicate == null)
            {
                targets = resolved.ToList();
            }
            else
            {
                var predicateVersion = project.ChangeVersion;
                var predicateOwnership = SnapshotElementOwnership(project);
                targets = resolved.Where(predicate).ToList();
                if (project.ChangeVersion != predicateVersion)
                    throw new InvalidOperationException("Project state changed while evaluating semantic untrack predicate. Retry against the current project state.");
                RequireElementOwnershipUnchanged(project, predicateOwnership);
            }
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

        private static IReadOnlyDictionary<string, ProjectElement> SnapshotElementOwnership(ProjectState project)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null semantic element entry.");
                if (result.ContainsKey(element.Id))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + element.Id + ".");
                result.Add(element.Id, element);
            }
            return result;
        }

        private static void RequireElementOwnershipUnchanged(
            ProjectState project,
            IReadOnlyDictionary<string, ProjectElement> expected)
        {
            if (project.Elements.Count != expected.Count)
                throw PredicateStructuralFreshnessError();

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null || !seen.Add(element.Id) ||
                    !expected.TryGetValue(element.Id, out var original) ||
                    !ReferenceEquals(original, element))
                    throw PredicateStructuralFreshnessError();
            }
        }

        private static InvalidOperationException PredicateStructuralFreshnessError()
        {
            return new InvalidOperationException(
                "Project element ownership changed while evaluating semantic untrack predicate. Retry against the current project state.");
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
