using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetScheduleKindBoundarySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ModelAndPlanViewsRemainPlaceable();
            DirectScheduleViewPlacementFailsClosed();
            AutoLayoutScheduleViewPlacementFailsClosed();
        }

        private static void ModelAndPlanViewsRemainPlaceable()
        {
            var views = BuildViews();
            var sheet = SemanticSheetPlanner.Build(
                new SemanticSheetDefinition(
                    "SHEET-VALID",
                    "A-001",
                    "Valid viewport kinds",
                    420d,
                    297d,
                    new[]
                    {
                        new SemanticSheetPlacementDefinition("VIEW-MODEL", 10d, 10d, 180d, 120d),
                        new SemanticSheetPlacementDefinition("VIEW-PLAN", 210d, 10d, 180d, 120d)
                    }),
                views);

            RequireEqual(2, sheet.Placements.Count, "Model and Plan views must remain placeable on a semantic sheet.");
            RequireEqual("VIEW-MODEL", sheet.Placements[0].ViewId, "Model view placement identity changed.");
            RequireEqual("VIEW-PLAN", sheet.Placements[1].ViewId, "Plan view placement identity changed.");
        }

        private static void DirectScheduleViewPlacementFailsClosed()
        {
            RequireScheduleKindFailure(
                () => SemanticSheetPlanner.Build(
                    new SemanticSheetDefinition(
                        "SHEET-DIRECT",
                        "A-002",
                        "Invalid direct schedule placement",
                        420d,
                        297d,
                        new[] { new SemanticSheetPlacementDefinition("VIEW-SCHEDULE", 10d, 10d, 180d, 120d) }),
                    BuildViews()),
                "Semantic sheet cannot place schedule view id as a sheet view: VIEW-SCHEDULE.");
        }

        private static void AutoLayoutScheduleViewPlacementFailsClosed()
        {
            RequireScheduleKindFailure(
                () => SemanticSheetAutoLayoutPlanner.Build(
                    new[] { new SemanticSheetAutoLayoutItem("VIEW-SCHEDULE", 180d, 120d) },
                    BuildViews(),
                    new SemanticSheetAutoLayoutOptions("AUTO", "A-", "Automatic", 420d, 297d)),
                "Semantic sheet cannot place schedule view id as a sheet view: VIEW-SCHEDULE.");
        }

        private static System.Collections.Generic.IReadOnlyList<SemanticViewPlan> BuildViews()
        {
            return SemanticViewPlanner.BuildCatalog(
                new ProjectState("P-SHEET-KIND", "Semantic Sheet kind boundary"),
                new[]
                {
                    new SemanticViewDefinition("VIEW-MODEL", "Model", SemanticViewKind.Model),
                    new SemanticViewDefinition("VIEW-PLAN", "Plan", SemanticViewKind.Plan),
                    new SemanticViewDefinition("VIEW-SCHEDULE", "Schedule", SemanticViewKind.Schedule)
                });
        }

        private static void RequireScheduleKindFailure(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(expectedMessage, ex.Message, StringComparison.Ordinal))
                    throw new InvalidOperationException("Schedule-kind placement returned the wrong diagnostic: " + ex.Message, ex);
                return;
            }

            throw new InvalidOperationException("Schedule-kind placement must fail closed before publishing a sheet plan.");
        }

        private static void RequireEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected " + expected + ", got " + actual + ".");
        }
    }
}
