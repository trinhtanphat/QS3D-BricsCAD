using System;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserQueryDefinitionBoundsSmoke
    {
        private const int MaxFamilies = 10000;
        private const int MaxReferenceDefinitions = 2000;

        public static void Run()
        {
            OversizedFamiliesFailBeforeIndexing();
            OversizedFloorsFailBeforeIndexing();
            OversizedZonesFailBeforeIndexing();
            ExactBoundariesRemainSupported();
        }

        private static void OversizedFamiliesFailBeforeIndexing()
        {
            var project = new ProjectState("P-BROWSER-QUERY-FAMILY-BOUND", "Browser query family bound");
            AddFamilies(project, MaxFamilies + 1);

            var error = ThrowsInvalidOperation(() => Filter(project));
            Equal("Project browser query supports at most 10000 family definitions.", error.Message);
        }

        private static void OversizedFloorsFailBeforeIndexing()
        {
            var project = new ProjectState("P-BROWSER-QUERY-FLOOR-BOUND", "Browser query floor bound");
            AddFloors(project, MaxReferenceDefinitions + 1);

            var error = ThrowsInvalidOperation(() => Filter(project));
            Equal("Project browser query supports at most 2000 floor definitions.", error.Message);
        }

        private static void OversizedZonesFailBeforeIndexing()
        {
            var project = new ProjectState("P-BROWSER-QUERY-ZONE-BOUND", "Browser query zone bound");
            AddZones(project, MaxReferenceDefinitions + 1);

            var error = ThrowsInvalidOperation(() => Filter(project));
            Equal("Project browser query supports at most 2000 zone definitions.", error.Message);
        }

        private static void ExactBoundariesRemainSupported()
        {
            var project = new ProjectState("P-BROWSER-QUERY-BOUNDARY", "Browser query definition boundary");
            AddFamilies(project, MaxFamilies);
            AddFloors(project, MaxReferenceDefinitions);
            AddZones(project, MaxReferenceDefinitions);

            var result = Filter(project);
            True(result.IsFiltered);
            Equal(0, result.TotalCount);
            Equal(0, result.MatchedCount);
            Equal(ProjectBrowserNodeKind.Root, result.Root.Kind);
        }

        private static ProjectBrowserQueryResult Filter(ProjectState project)
        {
            return ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.FloorThenCategory,
                new ProjectBrowserQueryOptions(query: "__definition_bound_no_match__"));
        }

        private static void AddFamilies(ProjectState project, int count)
        {
            for (var i = 0; i < count; i++)
                project.Families.Add(new ProjectFamily("FAM-" + i, "Family " + i, ElementCategory.Room));
        }

        private static void AddFloors(ProjectState project, int count)
        {
            for (var i = 0; i < count; i++)
                project.Floors.Add(new FloorDefinition("FLOOR-" + i, "Floor " + i, i));
        }

        private static void AddZones(ProjectState project, int count)
        {
            for (var i = 0; i < count; i++)
                project.Zones.Add(new ZoneDefinition("ZONE-" + i, "Zone " + i));
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

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}