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
            string primaryTargetNodePath,
            IEnumerable<string> effectiveExpandedPaths)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
            Viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
            Reveal = reveal ?? throw new ArgumentNullException(nameof(reveal));
            PrimaryTargetNodePath = primaryTargetNodePath ?? string.Empty;
            EffectiveExpandedPaths = (effectiveExpandedPaths ?? Enumerable.Empty<string>())
                .ToList()
                .AsReadOnly();
        }

        public ProjectBrowserQueryResult Query { get; }
        public ProjectBrowserViewport Viewport { get; }
        public ProjectBrowserSelectionRevealPlan Reveal { get; }
        public string PrimaryTargetNodePath { get; }
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
            int viewportPageSize = 200,
            bool revealPrimarySelection = false)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (state == null) throw new ArgumentNullException(nameof(state));

            var query = ProjectBrowserQueryPlanner.Build(project, state.Grouping, state.ToQueryOptions());
            var reveal = ProjectBrowserSelectionPlanner.PlanReveal(
                query.Root,
                state.SelectedElementIds,
                state.PrimaryElementId);
            var expanded = MergeExpandedPaths(state.ExpandedPaths, reveal.ExpansionPaths);
            var primaryTargetNodePath = revealPrimarySelection
                ? ResolvePrimaryTargetNodePath(query.Root, reveal)
                : string.Empty;
            var effectiveViewportOffset = primaryTargetNodePath.Length == 0
                ? viewportOffset
                : ProjectBrowserVirtualizationPlanner.ResolveContainingPageOffset(
                    query.Root,
                    expanded,
                    primaryTargetNodePath,
                    viewportPageSize);
            var viewport = ProjectBrowserVirtualizationPlanner.BuildViewport(
                query.Root,
                expanded,
                effectiveViewportOffset,
                viewportPageSize);
            return new ProjectBrowserWorkspacePlan(query, viewport, reveal, primaryTargetNodePath, expanded);
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
            var sourceQueryState = SelectionQueryState.Capture(project);
            var reveal = ProjectBrowserSelectionPlanner.PlanReveal(query.Root, selectedElementIds, primaryElementId);
            RequireSelectionFreshness(project, sourceChangeVersion, sourceElements, sourceQueryState);
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
            IReadOnlyList<ProjectElement> expectedElements,
            SelectionQueryState expectedQueryState)
        {
            if (project.ChangeVersion != expectedChangeVersion)
                throw new InvalidOperationException("Project changed while Project Browser selection ids were being enumerated; recompute the selection against the current project state.");
            if (project.Elements.Count != expectedElements.Count)
                throw StructuralFreshnessError();
            for (var index = 0; index < expectedElements.Count; index++)
                if (!ReferenceEquals(project.Elements[index], expectedElements[index]))
                    throw StructuralFreshnessError();
            if (!expectedQueryState.Matches(project))
                throw new InvalidOperationException(
                    "Project Browser query inputs changed while Project Browser selection ids were being enumerated; recompute the selection against the current project state.");
        }

        private static InvalidOperationException StructuralFreshnessError()
        {
            return new InvalidOperationException(
                "Project element structure changed while Project Browser selection ids were being enumerated; recompute the selection against the current project state.");
        }

        private sealed class SelectionQueryState
        {
            private readonly IReadOnlyList<ElementQueryState> _elements;
            private readonly IReadOnlyList<FamilyQueryState> _families;
            private readonly IReadOnlyList<FloorQueryState> _floors;
            private readonly IReadOnlyList<ZoneQueryState> _zones;

            private SelectionQueryState(
                IReadOnlyList<ElementQueryState> elements,
                IReadOnlyList<FamilyQueryState> families,
                IReadOnlyList<FloorQueryState> floors,
                IReadOnlyList<ZoneQueryState> zones)
            {
                _elements = elements;
                _families = families;
                _floors = floors;
                _zones = zones;
            }

            internal static SelectionQueryState Capture(ProjectState project)
            {
                return new SelectionQueryState(
                    project.Elements.Select(ElementQueryState.Capture).ToArray(),
                    project.Families
                        .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x.Id, StringComparer.Ordinal)
                        .Select(FamilyQueryState.Capture)
                        .ToArray(),
                    project.Floors
                        .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x.Id, StringComparer.Ordinal)
                        .Select(FloorQueryState.Capture)
                        .ToArray(),
                    project.Zones
                        .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x.Id, StringComparer.Ordinal)
                        .Select(ZoneQueryState.Capture)
                        .ToArray());
            }

            internal bool Matches(ProjectState project)
            {
                if (project.Elements.Count != _elements.Count) return false;
                for (var index = 0; index < _elements.Count; index++)
                    if (!_elements[index].Matches(project.Elements[index])) return false;

                return FamiliesMatch(project) && FloorsMatch(project) && ZonesMatch(project);
            }

            private bool FamiliesMatch(ProjectState project)
            {
                if (project.Families.Count != _families.Count || project.Families.Any(x => x == null)) return false;
                var current = project.Families
                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Id, StringComparer.Ordinal)
                    .ToArray();
                for (var index = 0; index < _families.Count; index++)
                    if (!_families[index].Matches(current[index])) return false;
                return true;
            }

            private bool FloorsMatch(ProjectState project)
            {
                if (project.Floors.Count != _floors.Count || project.Floors.Any(x => x == null)) return false;
                var current = project.Floors
                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Id, StringComparer.Ordinal)
                    .ToArray();
                for (var index = 0; index < _floors.Count; index++)
                    if (!_floors[index].Matches(current[index])) return false;
                return true;
            }

            private bool ZonesMatch(ProjectState project)
            {
                if (project.Zones.Count != _zones.Count || project.Zones.Any(x => x == null)) return false;
                var current = project.Zones
                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Id, StringComparer.Ordinal)
                    .ToArray();
                for (var index = 0; index < _zones.Count; index++)
                    if (!_zones[index].Matches(current[index])) return false;
                return true;
            }
        }

        private sealed class ElementQueryState
        {
            private readonly ElementCategory _category;
            private readonly string _familyId;
            private readonly string _floorId;
            private readonly string _zoneId;
            private readonly ElementDirtyFlags _dirty;

            private ElementQueryState(ProjectElement element)
            {
                _category = element.Category;
                _familyId = element.FamilyId;
                _floorId = element.FloorId;
                _zoneId = element.ZoneId;
                _dirty = element.Dirty;
            }

            internal static ElementQueryState Capture(ProjectElement element) => new ElementQueryState(element);

            internal bool Matches(ProjectElement element)
            {
                return element != null &&
                       element.Category == _category &&
                       string.Equals(element.FamilyId, _familyId, StringComparison.Ordinal) &&
                       string.Equals(element.FloorId, _floorId, StringComparison.Ordinal) &&
                       string.Equals(element.ZoneId, _zoneId, StringComparison.Ordinal) &&
                       element.Dirty == _dirty;
            }
        }

        private sealed class FamilyQueryState
        {
            private readonly string _id;
            private readonly string _name;
            private readonly ElementCategory _category;

            private FamilyQueryState(ProjectFamily family)
            {
                _id = family.Id;
                _name = family.Name;
                _category = family.Category;
            }

            internal static FamilyQueryState Capture(ProjectFamily family) => new FamilyQueryState(family);

            internal bool Matches(ProjectFamily family)
            {
                return family != null &&
                       string.Equals(family.Id, _id, StringComparison.Ordinal) &&
                       string.Equals(family.Name, _name, StringComparison.Ordinal) &&
                       family.Category == _category;
            }
        }

        private sealed class FloorQueryState
        {
            private readonly string _id;
            private readonly string _name;
            private readonly double _elevationM;

            private FloorQueryState(FloorDefinition floor)
            {
                _id = floor.Id;
                _name = floor.Name;
                _elevationM = floor.ElevationM;
            }

            internal static FloorQueryState Capture(FloorDefinition floor) => new FloorQueryState(floor);

            internal bool Matches(FloorDefinition floor)
            {
                return floor != null &&
                       string.Equals(floor.Id, _id, StringComparison.Ordinal) &&
                       string.Equals(floor.Name, _name, StringComparison.Ordinal) &&
                       floor.ElevationM.Equals(_elevationM);
            }
        }

        private sealed class ZoneQueryState
        {
            private readonly string _id;
            private readonly string _name;

            private ZoneQueryState(ZoneDefinition zone)
            {
                _id = zone.Id;
                _name = zone.Name;
            }

            internal static ZoneQueryState Capture(ZoneDefinition zone) => new ZoneQueryState(zone);

            internal bool Matches(ZoneDefinition zone)
            {
                return zone != null &&
                       string.Equals(zone.Id, _id, StringComparison.Ordinal) &&
                       string.Equals(zone.Name, _name, StringComparison.Ordinal);
            }
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

        private static string ResolvePrimaryTargetNodePath(
            ProjectBrowserNode root,
            ProjectBrowserSelectionRevealPlan reveal)
        {
            if (!reveal.HasSelection || string.IsNullOrEmpty(reveal.PrimaryElementId)) return string.Empty;
            var primaryReveal = ProjectBrowserSelectionPlanner.PlanReveal(
                root,
                new[] { reveal.PrimaryElementId },
                reveal.PrimaryElementId);
            if (primaryReveal.TargetNodePaths.Count != 1)
                throw new InvalidOperationException("Project browser primary selection must resolve to exactly one reveal target node.");
            return primaryReveal.TargetNodePaths[0];
        }
    }
}
