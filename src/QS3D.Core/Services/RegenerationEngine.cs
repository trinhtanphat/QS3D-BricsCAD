using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

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

        public RegenerationEngine(DependencyGraph graph, IEnumerable<IElementRegenerator> regenerators)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _regenerators = new List<IElementRegenerator>(regenerators ?? throw new ArgumentNullException(nameof(regenerators)));
        }

        public void MarkChanged(ProjectState project, string elementId, ElementDirtyFlags flags)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            _graph.Rebuild(project.Elements);
            var source = project.FindElement(elementId) ?? throw new KeyNotFoundException("Unknown element: " + elementId);
            source.MarkDirty(flags);
            foreach (var dependentId in _graph.GetDependentsTransitive(elementId))
                project.FindElement(dependentId)?.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            project.Touch();
        }

        public int RegenerateDirty(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var total = 0;
            var maxPasses = Math.Max(2, project.Elements.Count * 2 + 2);

            for (var pass = 0; pass < maxPasses; pass++)
            {
                _graph.Rebuild(project.Elements);
                var dirty = _graph.TopologicalDirtyOrder(project.Elements);
                if (dirty.Count == 0) break;
                var progress = 0;

                foreach (var element in dirty)
                {
                    IElementRegenerator? selected = null;
                    foreach (var regenerator in _regenerators)
                    {
                        if (!regenerator.CanRegenerate(element.Category)) continue;
                        selected = regenerator;
                        break;
                    }
                    if (selected == null) continue;
                    selected.Regenerate(project, element);
                    element.MarkClean(ElementDirtyFlags.All);
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
