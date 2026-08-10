using System;
using System.Collections.Generic;
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
            _regenerators = new List<IElementRegenerator>(regenerators ?? throw new ArgumentNullException(nameof(regenerators)));
            _ruleEngine = new QuantityRuleEngine();
        }

        public void MarkChanged(ProjectState project, string elementId, ElementDirtyFlags flags)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            _graph.Rebuild(project.Elements);

            var byId = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
                byId[element.Id] = element;

            var normalizedId = (elementId ?? string.Empty).Trim();
            if (!byId.TryGetValue(normalizedId, out var source))
                throw new KeyNotFoundException("Unknown element: " + elementId);

            var dependents = new List<ProjectElement>();
            foreach (var dependentId in _graph.GetDependentsTransitive(source.Id))
                if (byId.TryGetValue(dependentId, out var dependent)) dependents.Add(dependent);

            source.MarkDirty(flags);
            foreach (var dependent in dependents)
                dependent.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            project.Touch();
        }

        public int RegenerateDirty(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return RegenerateTransactional(project, project.Elements, project.Elements.Count);
        }

        public int RegenerateDirtySubset(ProjectState project, IEnumerable<string> elementIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));

            var ids = new HashSet<string>(
                elementIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
            if (ids.Count == 0) return 0;

            var byId = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements) byId[element.Id] = element;
            foreach (var id in ids)
                if (!byId.ContainsKey(id)) throw new KeyNotFoundException("Unknown regeneration target: " + id);

            var targets = project.Elements.Where(x => ids.Contains(x.Id)).ToList();
            return RegenerateTransactional(project, targets, targets.Count);
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
                _graph.Rebuild(project.Elements);
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
