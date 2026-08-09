using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public interface IElementRegenerator
    {
        bool CanRegenerate(ElementCategory category);
        void Regenerate(ProjectState project, ProjectElement element);
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
            _graph.Rebuild(project.Elements);
            var count = 0;
            foreach (var element in _graph.TopologicalDirtyOrder(project.Elements))
            {
                foreach (var regenerator in _regenerators)
                {
                    if (!regenerator.CanRegenerate(element.Category)) continue;
                    regenerator.Regenerate(project, element);
                    count++;
                    break;
                }
                element.MarkClean(ElementDirtyFlags.Geometry | ElementDirtyFlags.Properties | ElementDirtyFlags.Relations);
            }
            if (count > 0) project.Touch();
            return count;
        }
    }
}
