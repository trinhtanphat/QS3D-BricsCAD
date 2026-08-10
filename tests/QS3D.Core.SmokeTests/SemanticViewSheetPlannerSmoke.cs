using System;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewSheetPlannerSmoke
    {
        public static void Run()
        {
            ViewFilteringIsDeterministic();
            ViewReferencesFailClosed();
            DuplicateProjectElementIdsFailClosed();
            SheetCompositionIsDeterministic();
            SheetOverlapFailsClosed();
            SheetBoundsFailClosed();
            DuplicateViewIdentityFailsClosed();
        }

        private static void ViewFilteringIsDeterministic()
        {
            var project = BuildProject();
            var view = new SemanticViewDefinition(
                "VIEW-L02-BEAMS",
                "L02 Beams",
                SemanticViewKind.Plan,
                floorId: "F-02",
                categories: new[] { ElementCategory.Beam });

            var plan = SemanticViewPlanner.Build(project, view);
            Equal(2, plan.ElementIds.Count);
            Equal("B-001", plan.ElementIds[0]);
            Equal("B-002", plan.ElementIds[1]);
            Equal("F-02", plan.FloorId);
        }

        private static void ViewReferencesFailClosed()
        {
            var project = BuildProject();
            MustFail(
                () => SemanticViewPlanner.Build(project, new SemanticViewDefinition("V1", "Missing floor", floorId: "F-404")),
                "Missing floor references must fail closed.");
            MustFail(
                () => SemanticViewPlanner.Build(project, new SemanticViewDefinition("V2", "Missing include", includeElementIds: new[] { "E-404" })),
                "Missing explicit element references must fail closed.");
            MustFail(
                () => SemanticViewPlanner.Build(project, new SemanticViewDefinition("V3", "Conflicting filter", includeElementIds: new[] { "B-001" }, excludeElementIds: new[] { "b-001" })),
                "Conflicting include/exclude filters must fail closed.");
        }

        private static void DuplicateProjectElementIdsFailClosed()
        {
            var project = BuildProject();
            project.Elements.Add(new ProjectElement("b-001", ElementCategory.Column, "", "F-02", "Z-A"));
            MustFail(
                () => SemanticViewPlanner.Build(project, new SemanticViewDefinition("V1", "Ambiguous project")),
                "Duplicate semantic element IDs must fail closed before view planning.");
        }

        private static void SheetCompositionIsDeterministic()
        {
            var project = BuildProject();
            var views = SemanticViewPlanner.BuildCatalog(project, new[]
            {
                new SemanticViewDefinition("VIEW-B", "B View", categories: new[] { ElementCategory.Beam }),
                new SemanticViewDefinition("VIEW-C", "C View", categories: new[] { ElementCategory.Column })
            });
            var sheet = new SemanticSheetDefinition(
                "SHEET-01",
                "A-101",
                "General Arrangement",
                841d,
                594d,
                new[]
                {
                    new SemanticSheetPlacementDefinition("VIEW-C", 430d, 20d, 380d, 250d),
                    new SemanticSheetPlacementDefinition("VIEW-B", 20d, 20d, 380d, 250d)
                },
                "A1 Standard");

            var plan = SemanticSheetPlanner.Build(sheet, views);
            Equal(2, plan.Placements.Count);
            Equal("VIEW-B", plan.Placements[0].ViewId);
            Equal("VIEW-C", plan.Placements[1].ViewId);
            Equal("A-101", plan.Number);
        }

        private static void SheetOverlapFailsClosed()
        {
            var project = BuildProject();
            var views = SemanticViewPlanner.BuildCatalog(project, new[]
            {
                new SemanticViewDefinition("V1", "View 1"),
                new SemanticViewDefinition("V2", "View 2")
            });
            var sheet = new SemanticSheetDefinition(
                "S1", "A-001", "Overlap", 420d, 297d,
                new[]
                {
                    new SemanticSheetPlacementDefinition("V1", 10d, 10d, 200d, 150d),
                    new SemanticSheetPlacementDefinition("V2", 100d, 100d, 200d, 150d)
                });
            MustFail(() => SemanticSheetPlanner.Build(sheet, views), "Overlapping semantic sheet placements must fail closed.");
        }

        private static void SheetBoundsFailClosed()
        {
            var project = BuildProject();
            var views = SemanticViewPlanner.BuildCatalog(project, new[] { new SemanticViewDefinition("V1", "View 1") });
            var sheet = new SemanticSheetDefinition(
                "S1", "A-001", "Out of bounds", 420d, 297d,
                new[] { new SemanticSheetPlacementDefinition("V1", 300d, 10d, 200d, 100d) });
            MustFail(() => SemanticSheetPlanner.Build(sheet, views), "Out-of-bounds semantic sheet placements must fail closed.");
        }

        private static void DuplicateViewIdentityFailsClosed()
        {
            var project = BuildProject();
            MustFail(
                () => SemanticViewPlanner.BuildCatalog(project, new[]
                {
                    new SemanticViewDefinition("VIEW-1", "First"),
                    new SemanticViewDefinition("view-1", "Second")
                }),
                "Duplicate view IDs must fail closed case-insensitively.");
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-DOC", "Documentation Planning");
            project.Floors.Add(new FloorDefinition("F-01", "L01", 0d));
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Elements.Add(new ProjectElement("B-002", ElementCategory.Beam, "", "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("C-001", ElementCategory.Column, "", "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam, "", "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("B-000", ElementCategory.Beam, "", "F-01", "Z-A"));
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
