using System;
using System.Collections.Generic;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetAutoLayoutSmoke
    {
        public static void Run()
        {
            PacksAcrossSheetsDeterministically();
            ReservedTitleBlockAreaIsRespected();
            MissingViewFailsClosed();
            OversizedViewFailsClosed();
            DuplicateRequestedViewFailsClosed();
            BoundedItemsDoNotOverEnumerate();
            BoundedAvailableViewsDoNotOverEnumerate();
        }

        private static void PacksAcrossSheetsDeterministically()
        {
            var views = BuildViews(5);
            var items = new List<SemanticSheetAutoLayoutItem>();
            for (var i = 1; i <= 5; i++) items.Add(new SemanticSheetAutoLayoutItem("V" + i, 130d, 80d));
            var options = new SemanticSheetAutoLayoutOptions("SHEET", "A-", "General Arrangement", 300d, 200d, 10d, 10d, 10d, 10d, 10d, 10d);

            var sheets = SemanticSheetAutoLayoutPlanner.Build(items, views, options);
            Equal(2, sheets.Count);
            Equal("SHEET-01", sheets[0].Id);
            Equal("A-01", sheets[0].Number);
            Equal(4, sheets[0].Placements.Count);
            Equal("V1", sheets[0].Placements[0].ViewId);
            Equal(10d, sheets[0].Placements[0].Xmm);
            Equal(10d, sheets[0].Placements[0].Ymm);
            Equal("V2", sheets[0].Placements[1].ViewId);
            Equal(150d, sheets[0].Placements[1].Xmm);
            Equal(10d, sheets[0].Placements[1].Ymm);
            Equal("V3", sheets[0].Placements[2].ViewId);
            Equal(10d, sheets[0].Placements[2].Xmm);
            Equal(100d, sheets[0].Placements[2].Ymm);
            Equal("V5", sheets[1].Placements[0].ViewId);
        }

        private static void ReservedTitleBlockAreaIsRespected()
        {
            var views = BuildViews(2);
            var items = new[]
            {
                new SemanticSheetAutoLayoutItem("V1", 130d, 80d),
                new SemanticSheetAutoLayoutItem("V2", 130d, 80d)
            };
            var options = new SemanticSheetAutoLayoutOptions(
                "S", "D-", "Detail", 300d, 200d,
                marginLeftMm: 10d,
                marginTopMm: 10d,
                marginRightMm: 10d,
                marginBottomMm: 10d,
                horizontalGapMm: 10d,
                verticalGapMm: 10d,
                reservedBottomMm: 80d,
                titleBlockName: "A3 Title Block");

            var sheets = SemanticSheetAutoLayoutPlanner.Build(items, views, options);
            Equal(1, sheets.Count);
            Equal(2, sheets[0].Placements.Count);
            Equal("A3 Title Block", sheets[0].TitleBlockName);
            foreach (var placement in sheets[0].Placements)
                if (placement.Ymm + placement.HeightMm > 110d)
                    throw new Exception("Automatic layout placed a view inside the reserved title-block area.");
        }

        private static void MissingViewFailsClosed()
        {
            MustFail(
                () => SemanticSheetAutoLayoutPlanner.Build(
                    new[] { new SemanticSheetAutoLayoutItem("V404", 100d, 80d) },
                    BuildViews(1),
                    new SemanticSheetAutoLayoutOptions("S", "A-", "Sheet", 297d, 210d)),
                "Missing semantic views must fail closed.");
        }

        private static void OversizedViewFailsClosed()
        {
            MustFail(
                () => SemanticSheetAutoLayoutPlanner.Build(
                    new[] { new SemanticSheetAutoLayoutItem("V1", 400d, 80d) },
                    BuildViews(1),
                    new SemanticSheetAutoLayoutOptions("S", "A-", "Sheet", 297d, 210d)),
                "Oversized semantic views must fail closed instead of clipping.");
        }

        private static void DuplicateRequestedViewFailsClosed()
        {
            MustFail(
                () => SemanticSheetAutoLayoutPlanner.Build(
                    new[]
                    {
                        new SemanticSheetAutoLayoutItem("V1", 100d, 80d),
                        new SemanticSheetAutoLayoutItem("v1", 100d, 80d)
                    },
                    BuildViews(1),
                    new SemanticSheetAutoLayoutOptions("S", "A-", "Sheet", 297d, 210d)),
                "A semantic view must not be materialized twice by one automatic layout request.");
        }

        private static void BoundedItemsDoNotOverEnumerate()
        {
            MustFail(
                () => SemanticSheetAutoLayoutPlanner.Build(
                    OverBoundedItems(),
                    BuildViews(1),
                    new SemanticSheetAutoLayoutOptions("S", "A-", "Sheet", 297d, 210d)),
                "Automatic sheet layout must stop enumeration as soon as its configured item bound is exceeded.");
        }

        private static void BoundedAvailableViewsDoNotOverEnumerate()
        {
            MustFail(
                () => SemanticSheetAutoLayoutPlanner.Build(
                    Array.Empty<SemanticSheetAutoLayoutItem>(),
                    OverBoundedViews(),
                    new SemanticSheetAutoLayoutOptions("S", "A-", "Sheet", 297d, 210d)),
                "Automatic sheet layout must stop available-view enumeration as soon as its configured catalog bound is exceeded.");
        }

        private static IEnumerable<SemanticSheetAutoLayoutItem> OverBoundedItems()
        {
            for (var i = 0; i <= 10000; i++) yield return new SemanticSheetAutoLayoutItem("V1", 100d, 80d);
            throw new ApplicationException("Automatic sheet layout enumerated beyond the first over-bound item.");
        }

        private static IEnumerable<SemanticViewPlan> OverBoundedViews()
        {
            var project = new ProjectState("P-AUTO-SHEET-BOUND", "Auto Sheet Available View Bound");
            for (var i = 0; i <= 10000; i++)
                yield return SemanticViewPlanner.Build(project, new SemanticViewDefinition("BOUND-V" + i, "Bound View " + i));
            throw new ApplicationException("Automatic sheet layout enumerated beyond the first over-bound available view.");
        }

        private static IReadOnlyList<SemanticViewPlan> BuildViews(int count)
        {
            var project = new ProjectState("P-AUTO-SHEET", "Auto Sheet Smoke");
            var definitions = new List<SemanticViewDefinition>();
            for (var i = 1; i <= count; i++) definitions.Add(new SemanticViewDefinition("V" + i, "View " + i));
            return SemanticViewPlanner.BuildCatalog(project, definitions);
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
