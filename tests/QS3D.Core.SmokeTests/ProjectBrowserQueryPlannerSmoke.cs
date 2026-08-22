using System;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserQueryPlannerSmoke
    {
        public static void Run()
        {
            SearchMatchesSemanticCatalogNames();
            DirtyAndCategoryFiltersCompose();
            FloorAndZoneFiltersCompose();
            EmptySearchReturnsWholeTree();
            MissingFamilyReferenceFailsClosed();
            FilteredPathStillValidatesUnmatchedReferences();
            InvalidFilterReferenceFailsClosed();
        }

        private static void SearchMatchesSemanticCatalogNames()
        {
            var project = BuildProject();
            var byFamily = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.FloorThenCategory,
                new ProjectBrowserQueryOptions("Concrete Beam"));
            Equal(2, byFamily.MatchedCount);

            var byFloor = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.FloorThenCategory,
                new ProjectBrowserQueryOptions("Level 02"));
            Equal(3, byFloor.MatchedCount);

            var byZone = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.ZoneThenCategory,
                new ProjectBrowserQueryOptions("East Wing"));
            Equal(3, byZone.MatchedCount);

            var byId = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.Category,
                new ProjectBrowserQueryOptions("B-002"));
            Equal(1, byId.MatchedCount);
            Equal("B-002", byId.Root.ElementIds[0]);
        }

        private static void DirtyAndCategoryFiltersCompose()
        {
            var project = BuildProject();
            project.Elements[0].MarkClean(ElementDirtyFlags.All);
            var result = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.Category,
                new ProjectBrowserQueryOptions(
                    dirtyOnly: true,
                    categories: new[] { ElementCategory.Beam }));
            Equal(1, result.MatchedCount);
            Equal("B-001", result.Root.ElementIds[0]);
        }

        private static void FloorAndZoneFiltersCompose()
        {
            var project = BuildProject();
            var result = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.FloorThenCategory,
                new ProjectBrowserQueryOptions(
                    floorIds: new[] { "F-02" },
                    zoneIds: new[] { "Z-EAST" }));
            Equal(3, result.MatchedCount);
            Equal(1, result.Root.Children.Count);
            Equal("Level 02", result.Root.Children[0].DisplayName);
        }

        private static void EmptySearchReturnsWholeTree()
        {
            var project = BuildProject();
            var result = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.Category,
                new ProjectBrowserQueryOptions("   "));
            Equal(false, result.IsFiltered);
            Equal(project.Elements.Count, result.MatchedCount);
            Equal(project.Elements.Count, result.TotalCount);
        }

        private static void MissingFamilyReferenceFailsClosed()
        {
            var project = BuildProject();
            project.Elements.Add(new ProjectElement("BAD-1", ElementCategory.Beam, "FAM-404", "F-02", "Z-EAST"));
            MustFail(
                () => ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.Category, new ProjectBrowserQueryOptions("beam")),
                "Search must not silently hide an element with a missing family reference.");
        }

        private static void FilteredPathStillValidatesUnmatchedReferences()
        {
            var project = BuildProject();
            var bad = new ProjectElement("BAD-REF", ElementCategory.Column, "FAM-C", "F-404", "Z-EAST");
            bad.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(bad);
            MustFail(
                () => ProjectBrowserQueryPlanner.Build(
                    project,
                    ProjectBrowserGrouping.Category,
                    new ProjectBrowserQueryOptions(dirtyOnly: true, categories: new[] { ElementCategory.Beam })),
                "Filtered browser path must validate corrupt references even when the corrupt element would not match the filter.");
        }

        private static void InvalidFilterReferenceFailsClosed()
        {
            var project = BuildProject();
            MustFail(
                () => ProjectBrowserQueryPlanner.Build(
                    project,
                    ProjectBrowserGrouping.Category,
                    new ProjectBrowserQueryOptions(floorIds: new[] { "F-404" })),
                "Unknown explicit browser filter IDs must fail closed.");
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-BROWSER-QUERY", "Browser Query Smoke");
            project.Floors.Add(new FloorDefinition("F-01", "Level 01", 0d));
            project.Floors.Add(new FloorDefinition("F-02", "Level 02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-EAST", "East Wing"));
            project.Zones.Add(new ZoneDefinition("Z-WEST", "West Wing"));
            project.Families.Add(new ProjectFamily("FAM-B", "Concrete Beam 300x500", ElementCategory.Beam));
            project.Families.Add(new ProjectFamily("FAM-C", "Concrete Column 400x400", ElementCategory.Column));

            project.Elements.Add(new ProjectElement("B-002", ElementCategory.Beam, "FAM-B", "F-02", "Z-EAST"));
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam, "FAM-B", "F-02", "Z-EAST"));
            project.Elements.Add(new ProjectElement("C-001", ElementCategory.Column, "FAM-C", "F-02", "Z-EAST"));
            project.Elements.Add(new ProjectElement("C-000", ElementCategory.Column, "FAM-C", "F-01", "Z-WEST"));
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
