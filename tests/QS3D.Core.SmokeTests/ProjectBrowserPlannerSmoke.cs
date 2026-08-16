using System;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserPlannerSmoke
    {
        public static void Run()
        {
            FloorGroupingIsDeterministic();
            ZoneGroupingTracksDirtyCounts();
            UnassignedReferencesRemainVisible();
            MissingReferencesFailClosed();
            DuplicateIdentityFailsClosed();
            NonCanonicalElementIdentityFailsClosed();
        }

        private static void FloorGroupingIsDeterministic()
        {
            var project = BuildProject();
            var root = ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory);
            Equal(4, root.Count);
            Equal(2, root.Children.Count);
            Equal("L01", root.Children[0].DisplayName);
            Equal("L02", root.Children[1].DisplayName);
            Equal(3, root.Children[1].Count);
            Equal("Beam", root.Children[1].Children[0].DisplayName);
            Equal("B-001", root.Children[1].Children[0].ElementIds[0]);
            Equal("B-002", root.Children[1].Children[0].ElementIds[1]);
        }

        private static void ZoneGroupingTracksDirtyCounts()
        {
            var project = BuildProject();
            project.Elements[0].MarkClean(ElementDirtyFlags.All);
            var root = ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.ZoneThenCategory);
            Equal(3, root.DirtyCount);
            Equal("Zone A", root.Children[0].DisplayName);
            Equal(3, root.Children[0].DirtyCount);
        }

        private static void UnassignedReferencesRemainVisible()
        {
            var project = BuildProject();
            project.Elements.Add(new ProjectElement("FREE-001", ElementCategory.Room, "", "", ""));
            var floorRoot = ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory);
            Equal("(No Floor)", floorRoot.Children[floorRoot.Children.Count - 1].DisplayName);
            var zoneRoot = ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.ZoneThenCategory);
            Equal("(No Zone)", zoneRoot.Children[zoneRoot.Children.Count - 1].DisplayName);
        }

        private static void MissingReferencesFailClosed()
        {
            var project = BuildProject();
            project.Elements.Add(new ProjectElement("BAD-001", ElementCategory.Beam, "", "F-404", "Z-A"));
            MustFail(
                () => ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory),
                "Project browser must reject missing floor references instead of hiding the element.");
        }

        private static void DuplicateIdentityFailsClosed()
        {
            var project = BuildProject();
            project.Elements.Add(new ProjectElement("b-001", ElementCategory.Column, "", "F-02", "Z-A"));
            MustFail(
                () => ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.Category),
                "Project browser must reject duplicate semantic element IDs case-insensitively.");
        }

        private static void NonCanonicalElementIdentityFailsClosed()
        {
            var project = BuildProject();
            project.Elements.Add(new ProjectElement(" PADDED-001 ", ElementCategory.Room, "", "F-01", "Z-A"));
            MustFail(
                () => ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.Category),
                "Project browser must reject semantic element IDs with surrounding whitespace instead of emitting an ID downstream selection cannot consume.");

            var canonical = BuildProject();
            canonical.Elements.Add(new ProjectElement("PADDED-001", ElementCategory.Room, "", "F-01", "Z-A"));
            var root = ProjectBrowserPlanner.Build(canonical, ProjectBrowserGrouping.Category);
            Equal(5, root.Count);
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-BROWSER", "Browser Smoke");
            project.Floors.Add(new FloorDefinition("F-01", "L01", 0d));
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Elements.Add(new ProjectElement("B-002", ElementCategory.Beam, "", "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("C-001", ElementCategory.Column, "", "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam, "", "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("W-001", ElementCategory.ArchitecturalWall, "", "F-01", "Z-A"));
            return project;
        }

        private static void MustFail(Action action, string message)
        {
            var failed = false;
            try { action(); }
            catch (InvalidOperationException) { failed = true; }
            if (!failed) throw new Exception(message);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}
