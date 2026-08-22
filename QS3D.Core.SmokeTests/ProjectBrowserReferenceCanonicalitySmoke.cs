using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserReferenceCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-BROWSER-REF", "Browser reference smoke");
            project.Floors.Add(new FloorDefinition("F1", "Level 1", 0d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, "F1", "Z1");
            project.Elements.Add(element);

            var floorTree = ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory);
            var zoneTree = ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.ZoneThenCategory);
            Equal(1, floorTree.Children.Count, "canonical floor grouping");
            Equal(1, zoneTree.Children.Count, "canonical zone grouping");

            element.FloorId = "f1";
            element.ZoneId = "z1";
            var caseFloorTree = ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory);
            var caseZoneTree = ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.ZoneThenCategory);
            Equal("Level 1", caseFloorTree.Children[0].DisplayName, "case-insensitive floor grouping");
            Equal("Zone 1", caseZoneTree.Children[0].DisplayName, "case-insensitive zone grouping");

            element.FloorId = " F1 ";
            element.ZoneId = "Z1";
            Equal("F1", element.FloorId, "padded floor setter normalization");
            Equal("Level 1", ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory).Children[0].DisplayName, "padded floor normalized grouping");

            element.FloorId = "F1";
            element.ZoneId = " Z1 ";
            Equal("Z1", element.ZoneId, "padded zone setter normalization");
            Equal("Zone 1", ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.ZoneThenCategory).Children[0].DisplayName, "padded zone normalized grouping");

            element.FloorId = "   ";
            element.ZoneId = "Z1";
            Equal(string.Empty, element.FloorId, "whitespace-only floor setter normalization");
            Equal("(No Floor)", ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory).Children[0].DisplayName, "unassigned floor grouping");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectBrowserReferenceCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
