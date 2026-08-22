using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceCoordinatorSmoke
    {
        public static void Run()
        {
            SelectionRevealExpandsRequiredAncestors();
            NodeSelectionPagesDeterministically();
            ExpansionAndViewChangesStayPresentationOnly();
        }

        private static void SelectionRevealExpandsRequiredAncestors()
        {
            var project = BuildProject();
            var state = new ProjectBrowserWorkspaceState(
                ProjectBrowserGrouping.FloorThenCategory,
                selectedElementIds: new[] { "B-001" },
                primaryElementId: "B-001");

            var plan = ProjectBrowserWorkspaceCoordinator.Build(project, state, 0, 20);
            True(plan.Reveal.HasSelection);
            True(plan.EffectiveExpandedPaths.Count >= 2);
            True(plan.Viewport.Rows.Any(x => x.DisplayName == "L02"));
            True(plan.Viewport.Rows.Any(x => x.DisplayName == "Beam"));
        }

        private static void NodeSelectionPagesDeterministically()
        {
            var project = BuildProject();
            var query = ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory);
            var root = ProjectBrowserVirtualizationPlanner.GetRootPath(query.Root);
            var levelViewport = ProjectBrowserVirtualizationPlanner.BuildViewport(query.Root, new[] { root }, 0, 20);
            var floor = levelViewport.Rows.Single(x => x.DisplayName == "L02").Path;
            var categoryViewport = ProjectBrowserVirtualizationPlanner.BuildViewport(query.Root, new[] { root, floor }, 0, 20);
            var beam = categoryViewport.Rows.Single(x => x.DisplayName == "Beam").Path;

            var state = new ProjectBrowserWorkspaceState(
                ProjectBrowserGrouping.FloorThenCategory,
                expandedPaths: new[] { root, floor });
            var selected = ProjectBrowserWorkspaceCoordinator.ApplyNodeSelection(project, state, beam, 0, 1);
            Equal(1, selected.SelectedElementIds.Count);
            Equal("B-001", selected.SelectedElementIds[0]);
            Equal("B-001", selected.PrimaryElementId);
        }

        private static void ExpansionAndViewChangesStayPresentationOnly()
        {
            var project = BuildProject();
            project.Touch();
            var version = project.ChangeVersion;
            var query = ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory);
            var root = ProjectBrowserVirtualizationPlanner.GetRootPath(query.Root);
            var state = new ProjectBrowserWorkspaceState(ProjectBrowserGrouping.FloorThenCategory);

            var expanded = ProjectBrowserWorkspaceCoordinator.SetExpanded(project, state, root, true);
            Equal(version, project.ChangeVersion);
            True(expanded.ExpandedPaths.Contains(root));

            var updated = ProjectBrowserWorkspaceCoordinator.UpdateView(
                ProjectBrowserGrouping.Category,
                "beam",
                true,
                new[] { ElementCategory.Beam });
            Equal(ProjectBrowserGrouping.Category, updated.Grouping);
            Equal("beam", updated.Query);
            True(updated.DirtyOnly);
            Equal(0, updated.SelectedElementIds.Count);
            Equal(0, updated.ExpandedPaths.Count);
            Equal(version, project.ChangeVersion);
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-BROWSER-COORD", "Browser Coordinator");
            project.Floors.Add(new FloorDefinition("F-01", "L01", 0d));
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Elements.Add(new ProjectElement("B-002", ElementCategory.Beam, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("C-001", ElementCategory.Column, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("W-001", ElementCategory.ArchitecturalWall, string.Empty, "F-01", "Z-A"));
            return project;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectBrowserWorkspaceCoordinatorSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("ProjectBrowserWorkspaceCoordinatorSmoke assertion failed.");
        }
    }
}
