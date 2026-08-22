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
            ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory);
            ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.ZoneThenCategory);

            element.FloorId = " F1 ";
            element.ZoneId = "Z1";
            Throws<InvalidOperationException>(() => ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory), "padded floor reference");

            element.FloorId = "F1";
            element.ZoneId = " Z1 ";
            Throws<InvalidOperationException>(() => ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.ZoneThenCategory), "padded zone reference");

            element.FloorId = "   ";
            element.ZoneId = "Z1";
            Throws<InvalidOperationException>(() => ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.Category), "whitespace-only floor reference");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectBrowserReferenceCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("ProjectBrowserReferenceCanonicalitySmoke " + label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
