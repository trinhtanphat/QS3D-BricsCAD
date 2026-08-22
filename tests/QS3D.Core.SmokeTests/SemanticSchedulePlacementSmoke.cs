using System;
using System.Collections.Generic;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSchedulePlacementSmoke
    {
        public static void Run()
        {
            AvoidsExistingViewsDeterministically();
            ExistingViewOutsideScheduleMarginRemainsValid();
            ReservedBottomAreaIsRespected();
            MissingScheduleFailsClosed();
            DuplicateRequestedScheduleFailsClosed();
            DuplicateAvailableScheduleFailsClosed();
            NonCanonicalScheduleIdsFailClosed();
            TooManyAvailableSchedulesFailClosed();
            TooManyPlacementItemsFailClosed();
            OversizedScheduleFailsClosed();
            InvalidGeometryFailsClosed();
        }

        private static void AvoidsExistingViewsDeterministically()
        {
            var views = BuildViews("V1");
            var sheet = SemanticSheetPlanner.Build(
                new SemanticSheetDefinition(
                    "S1", "A-01", "Schedule Sheet", 300d, 200d,
                    new[] { new SemanticSheetPlacementDefinition("V1", 10d, 10d, 120d, 80d) }),
                views);
            var schedules = BuildSchedules("SCH-1", "SCH-2");
            var plan = SemanticSchedulePlacementPlanner.Build(
                sheet,
                schedules,
                new[]
                {
                    new SemanticSchedulePlacementItem("SCH-2", 80d, 40d),
                    new SemanticSchedulePlacementItem("SCH-1", 100d, 50d)
                });

            Equal("S1", plan.SheetId);
            Equal(2, plan.Placements.Count);
            Equal("SCH-1", plan.Placements[0].ScheduleId);
            Equal(138d, plan.Placements[0].Xmm);
            Equal(10d, plan.Placements[0].Ymm);
            Equal("SCH-2", plan.Placements[1].ScheduleId);
            Equal(138d, plan.Placements[1].Xmm);
            Equal(68d, plan.Placements[1].Ymm);
        }

        private static void ExistingViewOutsideScheduleMarginRemainsValid()
        {
            var views = BuildViews("V1");
            var sheet = SemanticSheetPlanner.Build(
                new SemanticSheetDefinition(
                    "S1", "A-01", "Schedule Sheet", 210d, 150d,
                    new[] { new SemanticSheetPlacementDefinition("V1", 2d, 2d, 2d, 2d) }),
                views);
            var plan = SemanticSchedulePlacementPlanner.Build(
                sheet,
                BuildSchedules("SCH-1"),
                new[] { new SemanticSchedulePlacementItem("SCH-1", 50d, 30d) },
                new SemanticSchedulePlacementOptions(
                    marginLeftMm: 20d,
                    marginTopMm: 20d,
                    marginRightMm: 20d,
                    marginBottomMm: 20d));

            Equal(1, plan.Placements.Count);
            var placement = plan.Placements[0];
            Equal(20d, placement.Xmm);
            Equal(20d, placement.Ymm);
        }

        private static void ReservedBottomAreaIsRespected()
        {
            var sheet = EmptySheet(210d, 150d);
            var plan = SemanticSchedulePlacementPlanner.Build(
                sheet,
                BuildSchedules("SCH-1"),
                new[] { new SemanticSchedulePlacementItem("SCH-1", 100d, 90d) },
                new SemanticSchedulePlacementOptions(reservedBottomMm: 40d));

            Equal(1, plan.Placements.Count);
            var placement = plan.Placements[0];
            if (placement.Ymm + placement.HeightMm > 100d)
                throw new Exception("Semantic schedule placement entered the reserved bottom/title-block region.");
        }

        private static void MissingScheduleFailsClosed()
        {
            MustFail(
                () => SemanticSchedulePlacementPlanner.Build(
                    EmptySheet(297d, 210d),
                    BuildSchedules("SCH-1"),
                    new[] { new SemanticSchedulePlacementItem("SCH-404", 100d, 60d) }),
                "Missing schedule ids must fail closed.");
        }

        private static void DuplicateRequestedScheduleFailsClosed()
        {
            MustFail(
                () => SemanticSchedulePlacementPlanner.Build(
                    EmptySheet(297d, 210d),
                    BuildSchedules("SCH-1"),
                    new[]
                    {
                        new SemanticSchedulePlacementItem("SCH-1", 100d, 60d),
                        new SemanticSchedulePlacementItem("sch-1", 80d, 40d)
                    }),
                "One schedule must not be placed twice by one request.");
        }

        private static void DuplicateAvailableScheduleFailsClosed()
        {
            MustFail(
                () => SemanticSchedulePlacementPlanner.Build(
                    EmptySheet(297d, 210d),
                    new[] { Schedule("SCH-1"), Schedule("sch-1") },
                    new[] { new SemanticSchedulePlacementItem("SCH-1", 100d, 60d) }),
                "Duplicate available schedule ids must fail closed.");
        }

        private static void NonCanonicalScheduleIdsFailClosed()
        {
            var sheet = EmptySheet(297d, 210d);
            var canonicalSchedules = BuildSchedules("SCH-1");
            foreach (var padded in new[] { " SCH-1", "SCH-1 ", "\tSCH-1", "SCH-1\n" })
            {
                MustFail(
                    () => SemanticSchedulePlacementPlanner.Build(
                        sheet,
                        canonicalSchedules,
                        new[] { new SemanticSchedulePlacementItem(padded, 100d, 60d) }),
                    "Padded requested schedule ids must fail closed: " + Escape(padded));

                MustFail(
                    () => SemanticSchedulePlacementPlanner.Build(
                        sheet,
                        new[] { Schedule(padded) },
                        new[] { new SemanticSchedulePlacementItem("SCH-1", 100d, 60d) }),
                    "Padded available schedule ids must fail closed: " + Escape(padded));
            }

            var caseInsensitive = SemanticSchedulePlacementPlanner.Build(
                sheet,
                canonicalSchedules,
                new[] { new SemanticSchedulePlacementItem("sch-1", 100d, 60d) });
            Equal("sch-1", caseInsensitive.Placements[0].ScheduleId);
        }

        private static void TooManyAvailableSchedulesFailClosed()
        {
            var schedules = new List<SemanticScheduleDefinition>();
            for (var i = 0; i < 129; i++) schedules.Add(Schedule("SCH-" + i));

            MustFail(
                () => SemanticSchedulePlacementPlanner.Build(
                    EmptySheet(297d, 210d),
                    schedules,
                    new[] { new SemanticSchedulePlacementItem("SCH-0", 100d, 60d) }),
                "Available schedule enumeration must fail closed at the 129th definition.");
        }

        private static void TooManyPlacementItemsFailClosed()
        {
            var schedules = new List<SemanticScheduleDefinition>();
            var items = new List<SemanticSchedulePlacementItem>();
            for (var i = 0; i < 128; i++)
            {
                schedules.Add(Schedule("SCH-" + i));
                items.Add(new SemanticSchedulePlacementItem("SCH-" + i, 1d, 1d));
            }
            items.Add(new SemanticSchedulePlacementItem("SCH-0", 1d, 1d));

            MustFail(
                () => SemanticSchedulePlacementPlanner.Build(
                    EmptySheet(297d, 210d),
                    schedules,
                    items),
                "Placement-item enumeration must fail closed at the 129th request.");
        }

        private static void OversizedScheduleFailsClosed()
        {
            MustFail(
                () => SemanticSchedulePlacementPlanner.Build(
                    EmptySheet(297d, 210d),
                    BuildSchedules("SCH-1"),
                    new[] { new SemanticSchedulePlacementItem("SCH-1", 400d, 60d) }),
                "Oversized schedules must fail closed instead of clipping.");
        }

        private static void InvalidGeometryFailsClosed()
        {
            MustFail(
                () => SemanticSchedulePlacementPlanner.Build(
                    EmptySheet(297d, 210d),
                    BuildSchedules("SCH-1"),
                    new[] { new SemanticSchedulePlacementItem("SCH-1", double.NaN, 60d) }),
                "Non-finite schedule geometry must fail closed.");
        }

        private static SemanticSheetPlan EmptySheet(double widthMm, double heightMm)
        {
            return SemanticSheetPlanner.Build(
                new SemanticSheetDefinition("S1", "A-01", "Schedule Sheet", widthMm, heightMm, Array.Empty<SemanticSheetPlacementDefinition>()),
                Array.Empty<SemanticViewPlan>());
        }

        private static IReadOnlyList<SemanticViewPlan> BuildViews(params string[] ids)
        {
            var project = new ProjectState("P-SCHEDULE-PLACEMENT", "Schedule Placement Smoke");
            var definitions = new List<SemanticViewDefinition>();
            foreach (var id in ids) definitions.Add(new SemanticViewDefinition(id, "View " + id));
            return SemanticViewPlanner.BuildCatalog(project, definitions);
        }

        private static IReadOnlyList<SemanticScheduleDefinition> BuildSchedules(params string[] ids)
        {
            var result = new List<SemanticScheduleDefinition>();
            foreach (var id in ids) result.Add(Schedule(id));
            return result.AsReadOnly();
        }

        private static SemanticScheduleDefinition Schedule(string id)
        {
            return new SemanticScheduleDefinition(
                id,
                "Schedule " + id,
                "Schedule " + id,
                Array.Empty<ElementCategory>(),
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { new SemanticDocumentationColumn("ID", "{Id}") });
        }

        private static string Escape(string value)
        {
            return value.Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static void MustFail(Action action, string message)
        {
            var failed = false;
            try { action(); }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                failed = true;
            }
            if (!failed) throw new Exception(message);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}
