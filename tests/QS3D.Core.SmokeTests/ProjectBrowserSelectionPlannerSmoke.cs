using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserSelectionPlannerSmoke
    {
        public static void Run()
        {
            SingleSelectionRevealsAncestors();
            CaseInsensitiveSelectionIdentityReveals();
            MultiSelectionUnionsExpansionPaths();
            InvalidSemanticSelectionFailsClosed();
            NodeSelectionUsesDeterministicPaging();
            ResultCollectionsAreImmutable();
        }

        private static void SingleSelectionRevealsAncestors()
        {
            var root = BuildRoot();
            var plan = ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "B-002" });

            Equal(1, plan.SelectedElementIds.Count);
            Equal("B-002", plan.SelectedElementIds[0]);
            Equal("B-002", plan.PrimaryElementId);
            Equal(1, plan.TargetNodePaths.Count);
            Equal(2, plan.ExpansionPaths.Count);
            True(plan.ExpansionPaths[0] == ProjectBrowserVirtualizationPlanner.GetRootPath(root));
            True(plan.TargetNodePaths[0].EndsWith("/category%3ABeam", StringComparison.Ordinal));
            True(!plan.IsMultiSelection);
        }

        private static void CaseInsensitiveSelectionIdentityReveals()
        {
            var root = BuildRoot();
            var plan = ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "b-001" }, "B-001");

            Equal(1, plan.SelectedElementIds.Count);
            Equal("b-001", plan.SelectedElementIds[0]);
            Equal("b-001", plan.PrimaryElementId);
            Equal(1, plan.TargetNodePaths.Count);
            Equal(2, plan.ExpansionPaths.Count);
            True(plan.TargetNodePaths[0].EndsWith("/category%3ABeam", StringComparison.Ordinal));
        }

        private static void MultiSelectionUnionsExpansionPaths()
        {
            var root = BuildRoot();
            var plan = ProjectBrowserSelectionPlanner.PlanReveal(
                root,
                new[] { "W-001", "C-001" },
                "W-001");

            Equal(2, plan.SelectedElementIds.Count);
            Equal("W-001", plan.PrimaryElementId);
            Equal(2, plan.TargetNodePaths.Count);
            Equal(3, plan.ExpansionPaths.Count);
            Equal(ProjectBrowserVirtualizationPlanner.GetRootPath(root), plan.ExpansionPaths[0]);
            True(plan.ExpansionPaths.Any(x => x.EndsWith("/floor%3AF-01", StringComparison.Ordinal)));
            True(plan.ExpansionPaths.Any(x => x.EndsWith("/floor%3AF-02", StringComparison.Ordinal)));
            True(plan.IsMultiSelection);
        }

        private static void InvalidSemanticSelectionFailsClosed()
        {
            var root = BuildRoot();
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "B-001", "b-001" }));
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "MISSING" }));
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "B-001" }, "C-001"));
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { " B-001" }));
        }

        private static void NodeSelectionUsesDeterministicPaging()
        {
            var root = BuildRoot();
            var reveal = ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "B-001" });
            var beamPath = reveal.TargetNodePaths.Single();

            var first = ProjectBrowserSelectionPlanner.PlanNodeSelection(root, beamPath, 0, 1);
            Equal(2, first.TotalCount);
            Equal(1, first.ElementIds.Count);
            Equal("B-001", first.ElementIds[0]);
            Equal("B-001", first.PrimaryElementId);
            True(!first.HasPrevious);
            True(first.HasNext);

            var second = ProjectBrowserSelectionPlanner.PlanNodeSelection(root, beamPath, 1, 1);
            Equal("B-002", second.ElementIds[0]);
            True(second.HasPrevious);
            True(!second.HasNext);
        }

        private static void ResultCollectionsAreImmutable()
        {
            var root = BuildRoot();
            var plan = ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "B-001", "C-001" });
            Throws<NotSupportedException>(() => ((IList<string>)plan.SelectedElementIds).Clear());
            Throws<NotSupportedException>(() => ((IList<string>)plan.ExpansionPaths).Clear());
            Throws<NotSupportedException>(() => ((IList<string>)plan.TargetNodePaths).Clear());

            var page = ProjectBrowserSelectionPlanner.PlanNodeSelection(root, plan.TargetNodePaths[0], 0, 1);
            Throws<NotSupportedException>(() => ((IList<string>)page.ElementIds).Clear());
        }

        private static ProjectBrowserNode BuildRoot()
        {
            var project = new ProjectState("P-SELECTION", "Selection Browser");
            project.Floors.Add(new FloorDefinition("F-01", "L01", 0d));
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Elements.Add(new ProjectElement("B-002", ElementCategory.Beam, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("C-001", ElementCategory.Column, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("W-001", ElementCategory.ArchitecturalWall, string.Empty, "F-01", "Z-A"));
            return ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectBrowserSelectionPlannerSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("ProjectBrowserSelectionPlannerSmoke assertion failed.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("ProjectBrowserSelectionPlannerSmoke expected exception " + typeof(T).Name + ".");
        }
    }
}
