using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Navigation
{
    public sealed class ProjectBrowserWorkspacePlan
    {
        internal ProjectBrowserWorkspacePlan(
            ProjectBrowserQueryResult query,
            ProjectBrowserViewport viewport,
            ProjectBrowserSelectionRevealPlan reveal,
            IEnumerable<string> effectiveExpandedPaths)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
            Viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
            Reveal = reveal ?? throw new ArgumentNullException(nameof(reveal));
            EffectiveExpandedPaths = (effectiveExpandedPaths ?? Enumerable.Empty<string>())
                .ToList()
                .AsReadOnly();
        }

        public ProjectBrowserQueryResult Query { get; }
        public ProjectBrowserViewport Viewport { get; }
        public ProjectBrowserSelectionRevealPlan Reveal { get; }
        public IReadOnlyList<string> EffectiveExpandedPaths { get; }
    }

    /// <summary>
    /// Single source-safe coordination seam for a modeless Project Browser UI.
    /// It composes query, virtualization, selection reveal and persisted workspace
    /// contracts without storing native CAD ObjectIds/handles or touching semantic versioning.
    /// </summary>
    public static class ProjectBrowserWorkspaceCoordinator
    {
        public static ProjectBrowserWorkspacePlan Build(
            ProjectState project,
            ProjectBrowserWorkspaceState state,
            int viewportOffset = 0,
            int viewportPageSize = 200)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (state == null) throw new ArgumentNullException(nameof(state));

            var query = ProjectBrowserQueryPlanner.Build(project, state.Grouping, state.ToQueryOptions());
            var reveal = ProjectBrowserSelectionPlanner.PlanReveal(
                query.Root,
                state.SelectedElementIds,
                state.PrimaryElementId);
            var expanded = MergeExpandedPaths(state.ExpandedPaths, reveal.ExpansionPaths);
            var viewport = ProjectBrowserVirtualizationPlanner.BuildViewport(
                query.Root,
                expanded,
                viewportOffset,
                viewportPageSize);
            return new ProjectBrowserWorkspacePlan(query, viewport, reveal, expanded);
        }

        public static ProjectBrowserWorkspaceState ApplySelection(
            ProjectState project,
            ProjectBrowserWorkspaceState current,
            IEnumerable<string> selectedElementIds,
            string? primaryElementId = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (selectedElementIds == null) throw new ArgumentNullException(nameof(selectedElementIds));

            var sourceChangeVersion = project.ChangeVersion;
            var sourceElements = project.Elements.ToArray();
            var query = ProjectBrowserQueryPlanner.Build(project, current.Grouping, current.ToQueryOptions());
            var reveal = ProjectBrowserSelectionPlanner.PlanReveal(query.Root, selectedElementIds, primaryElementId);
            RequireSelectionFreshness(project, sourceChangeVersion, sourceElements);
            var expanded = MergeExpandedPaths(current.ExpandedPaths, reveal.ExpansionPaths);
            return Copy(
                current,
                expandedPaths: expanded,
                selectedElementIds: reveal.SelectedElementIds,
                primaryElementId: reveal.PrimaryElementId);
        }

        public static ProjectBrowserWorkspaceState ApplyNodeSelection(
            ProjectState project,
            ProjectBrowserWorkspaceState current,
            string nodePath,
            int offset = 0,
            int pageSize = 200)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (current == null) throw new ArgumentNullException(nameof(current));

            var query = ProjectBrowserQueryPlanner.Build(project, current.Grouping, current.ToQueryOptions());
            var nodeSelection = ProjectBrowserSelectionPlanner.PlanNodeSelection(query.Root, nodePath, offset, pageSize);
            return ApplySelection(
                project,
                current,
                nodeSelection.ElementIds,
                nodeSelection.PrimaryElementId);
        }

        public static ProjectBrowserWorkspaceState SetExpanded(
            ProjectState project,
            ProjectBrowserWorkspaceState current,
            string nodePath,
            bool expanded)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (string.IsNullOrWhiteSpace(nodePath)) throw new ArgumentException("Project browser node path is required.", nameof(nodePath));
            if (!string.Equals(nodePath, nodePath.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Project browser node path must not contain surrounding whitespace.", nameof(nodePath));

            var query = ProjectBrowserQueryPlanner.Build(project, current.Grouping, current.ToQueryOptions());
            ProjectBrowserVirtualizationPlanner.GetElementPage(query.Root, nodePath, 0, 1);

            var paths = new HashSet<string>(current.ExpandedPaths, StringComparer.Ordinal);
            if (expanded) paths.Add(nodePath);
            else paths.Remove(nodePath);
            return Copy(current, expandedPaths: paths.OrderBy(x => x, StringComparer.Ordinal));
        }

        public static ProjectBrowserWorkspaceState UpdateView(
            ProjectBrowserGrouping grouping,
            string? query = null,
            bool dirtyOnly = false,
            IEnumerable<ElementCategory>? categories = null,
            IEnumerable<string>? floorIds = null,
            IEnumerable<string>? zoneIds = null)
        {
            return new ProjectBrowserWorkspaceState(
                grouping,
                query,
                dirtyOnly,
                categories,
                floorIds,
                zoneIds,
                Array.Empty<string>(),
                Array.Empty<string>(),
                string.Empty);
        }

        private static void RequireSelectionFreshness(
            ProjectState project,
            long expectedChangeVersion,
            IReadOnlyList<ProjectElement> expectedElements)
        {
            if (project.ChangeVersion != expectedChangeVersion)
                throw new InvalidOperationException("Project changed while Project Browser selection ids were being enumerated; recompute the selection against the current project state.");
            if (project.Elements.Count != expectedElements.Count)
                throw StructuralFreshnessError();
            for (var index = 0; index < expectedElements.Count; index++)
                if (!ReferenceEquals(project.Elements[index], expectedElements[index]))
                    throw StructuralFreshnessError();
        }

        private static InvalidOperationException StructuralFreshnessError()
        {
            return new InvalidOperationException(
                "Project element structure changed while Project Browser selection ids were being enumerated; recompute the selection against the current project state.");
        }

        private static ProjectBrowserWorkspaceState Copy(
            ProjectBrowserWorkspaceState current,
            IEnumerable<string>? expandedPaths = null,
            IEnumerable<string>? selectedElementIds = null,
            string? primaryElementId = null)
        {
            return new ProjectBrowserWorkspaceState(
                current.Grouping,
                current.Query,
                current.DirtyOnly,
                current.Categories,
                current.FloorIds,
                current.ZoneIds,
                expandedPaths ?? current.ExpandedPaths,
                selectedElementIds ?? current.SelectedElementIds,
                primaryElementId ?? current.PrimaryElementId);
        }

        private static IReadOnlyList<string> MergeExpandedPaths(
            IEnumerable<string> persisted,
            IEnumerable<string> revealRequired)
        {
            var merged = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var path in persisted ?? Enumerable.Empty<string>()) merged.Add(path);
            foreach (var path in revealRequired ?? Enumerable.Empty<string>()) merged.Add(path);
            return merged.ToList().AsReadOnly();
        }
    }
}
