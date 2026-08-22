using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;

namespace QS3D.Core.Services
{
    public interface IElementRegenerator
    {
        bool CanRegenerate(ElementCategory category);
        void Regenerate(ProjectState project, ProjectElement element);
    }

    public static class RegeneratorCatalog
    {
        public static IReadOnlyList<IElementRegenerator> CreateDefault() => new IElementRegenerator[]
        {
            new OpeningRegenerator(),
            new WallRegenerator(),
            new StructuralRegenerator(),
            new RoomRegenerator(),
            new GenericTakeoffRegenerator()
        };
    }

    public sealed class RegenerationEngine
    {
        private readonly DependencyGraph _graph;
        private readonly IList<IElementRegenerator> _regenerators;
        private readonly QuantityRuleEngine _ruleEngine;

        public RegenerationEngine(DependencyGraph graph, IEnumerable<IElementRegenerator> regenerators)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            if (regenerators == null) throw new ArgumentNullException(nameof(regenerators));
            var materialized = new List<IElementRegenerator>(regenerators);
            if (materialized.Any(x => x == null))
                throw new ArgumentException("Regenerator collection cannot contain null entries.", nameof(regenerators));
            _regenerators = materialized;
            _ruleEngine = new QuantityRuleEngine();
        }

        public void MarkChanged(ProjectState project, string elementId, ElementDirtyFlags flags)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            _graph.Rebuild(project.Elements);

            var normalizedId = (elementId ?? string.Empty).Trim();
            if (!_graph.TryGetElement(normalizedId, out var source) || source == null)
                throw new KeyNotFoundException("Unknown element: " + elementId);

            var dependents = new List<ProjectElement>();
            foreach (var dependentId in _graph.GetDependentsTransitive(source.Id))
            {
                if (!_graph.TryGetElement(dependentId, out var dependent) || dependent == null)
                    throw new InvalidOperationException("Dependency graph returned missing semantic element: " + dependentId);
                dependents.Add(dependent);
            }

            ProjectSemanticMutationExecutor.Execute(project, "regeneration.mark-changed", () =>
            {
                source.MarkDirty(flags);
                foreach (var dependent in dependents)
                    dependent.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                project.Touch();
                return true;
            });
        }

        public int RegenerateDirty(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ValidateProjectElements(project.Elements);
            return RegenerateTransactional(project, project.Elements, project.Elements.Count);
        }

        public int RegenerateDirtySubset(ProjectState project, IEnumerable<string> elementIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));

            var unresolved = CanonicalTargetIds(elementIds, project.Elements.Count);
            if (unresolved.Count == 0) return 0;

            // Resolve the requested subset in one project-order scan. The previous implementation
            // built a full by-id dictionary and then scanned project.Elements again to recover
            // project order, which doubled O(project-size) work on every targeted regeneration pass.
            var targets = new List<ProjectElement>(unresolved.Count);
            var seenProjectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element entry.");
                if (!seenProjectIds.Add(element.Id))
                    throw new InvalidOperationException("Project contains duplicate element id: " + element.Id);
                if (unresolved.Remove(element.Id)) targets.Add(element);
            }
            if (unresolved.Count > 0)
            {
                var missing = unresolved.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).First();
                throw new KeyNotFoundException("Unknown regeneration target: " + missing);
            }

            return RegenerateTransactional(project, targets, targets.Count);
        }

        private static void ValidateProjectElements(IEnumerable<ProjectElement> elements)
        {
            var seenProjectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in elements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element entry.");
                if (!seenProjectIds.Add(element.Id))
                    throw new InvalidOperationException("Project contains duplicate element id: " + element.Id);
            }
        }

        private static HashSet<string> CanonicalTargetIds(IEnumerable<string> elementIds, int maxCount)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var value in elementIds)
            {
                var raw = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new ArgumentException("Regeneration target id cannot be blank at index " + index.ToString(CultureInfo.InvariantCulture) + ".", nameof(elementIds));
                if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                    throw new ArgumentException("Regeneration target id must be canonical without surrounding whitespace: " + raw + ".", nameof(elementIds));
                if (result.Contains(raw))
                    throw new ArgumentException("Duplicate regeneration target id: " + raw + ".", nameof(elementIds));
                if (result.Count >= maxCount)
                    throw new ArgumentException("Regeneration target set cannot exceed project element count of " + maxCount.ToString(CultureInfo.InvariantCulture) + ".", nameof(elementIds));
                result.Add(raw);
                index++;
            }
            return result;
        }

        private int RegenerateTransactional(ProjectState project, IEnumerable<ProjectElement> candidates, int passBasis)
        {
            var snapshot = ProjectStateSnapshot.Capture(project);
            try
            {
                return Regenerate(project, candidates, passBasis);
            }
            catch (Exception regenerationError)
            {
                try
                {
                    snapshot.Restore(project);
                }
                catch (Exception rollbackError)
                {
                    throw new AggregateException("Semantic regeneration failed and project rollback also failed.", regenerationError, rollbackError);
                }
                throw;
            }
        }

        private int Regenerate(ProjectState project, IEnumerable<ProjectElement> candidates, int passBasis)
        {
            var candidateList = candidates?.ToList() ?? throw new ArgumentNullException(nameof(candidates));
            var total = 0;
            var maxPasses = Math.Max(2, passBasis * 2 + 2);

            for (var pass = 0; pass < maxPasses; pass++)
            {
                // TopologicalDirtyOrder derives ordering directly from each candidate's DependsOn
                // list. Rebuilding the reverse-dependency index here never participates in that
                // ordering and previously caused a redundant full-project scan on every pass.
                var dirty = _graph.TopologicalDirtyOrder(candidateList);
                if (dirty.Count == 0) break;
                var progress = 0;

                foreach (var element in dirty)
                {
                    var semanticDirty = element.Dirty & (ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                    if (semanticDirty == ElementDirtyFlags.None) continue;

                    IElementRegenerator? selected = null;
                    foreach (var regenerator in _regenerators)
                    {
                        if (!regenerator.CanRegenerate(element.Category)) continue;
                        selected = regenerator;
                        break;
                    }

                    var handled = false;
                    if (selected != null)
                    {
                        selected.Regenerate(project, element);
                        handled = true;
                    }
                    if (MeasuredSolidQuantityPolicy.Apply(element)) handled = true;
                    if (_ruleEngine.ApplyMatching(project, element) > 0) handled = true;
                    if (!handled) continue;

                    element.MarkClean(ElementGeometryPolicy.SemanticCleanFlags(element.Category));
                    progress++;
                    total++;
                }

                if (progress == 0) break;
            }

            if (total > 0) project.Touch();
            return total;
        }
    }
}
