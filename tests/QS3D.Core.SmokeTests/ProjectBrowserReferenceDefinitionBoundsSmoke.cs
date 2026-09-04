using System;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserReferenceDefinitionBoundsSmoke
    {
        private const int MaxReferenceDefinitions = 2000;

        public static void Run()
        {
            OversizedFloorsFailBeforeIndexing();
            OversizedZonesFailBeforeIndexing();
            ExactBoundaryRemainsSupported();
        }

        private static void OversizedFloorsFailBeforeIndexing()
        {
            var project = new ProjectState("P-BROWSER-FLOOR-BOUND", "Browser floor bound");
            AddFloors(project, MaxReferenceDefinitions + 1);

            var error = ThrowsInvalidOperation(() => ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory));
            Equal("Project browser supports at most 2000 floor definitions.", error.Message);
        }

        private static void OversizedZonesFailBeforeIndexing()
        {
            var project = new ProjectState("P-BROWSER-ZONE-BOUND", "Browser zone bound");
            AddZones(project, MaxReferenceDefinitions + 1);

            var error = ThrowsInvalidOperation(() => ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.ZoneThenCategory));
            Equal("Project browser supports at most 2000 zone definitions.", error.Message);
        }

        private static void ExactBoundaryRemainsSupported()
        {
            var project = new ProjectState("P-BROWSER-BOUNDARY", "Browser reference boundary");
            AddFloors(project, MaxReferenceDefinitions);
            AddZones(project, MaxReferenceDefinitions);

            foreach (ProjectBrowserGrouping grouping in Enum.GetValues(typeof(ProjectBrowserGrouping)))
            {
                var root = ProjectBrowserPlanner.Build(project, grouping);
                Equal(ProjectBrowserNodeKind.Root, root.Kind);
                Equal(0, root.Count);
            }
        }

        private static void AddFloors(ProjectState project, int count)
        {
            for (var i = 0; i < count; i++)
                project.Floors.Add(new FloorDefinition("F-" + i, "Floor " + i, i));
        }

        private static void AddZones(ProjectState project, int count)
        {
            for (var i = 0; i < count; i++)
                project.Zones.Add(new ZoneDefinition("Z-" + i, "Zone " + i));
        }

        private static InvalidOperationException ThrowsInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException error)
            {
                return error;
            }

            throw new Exception("Expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
