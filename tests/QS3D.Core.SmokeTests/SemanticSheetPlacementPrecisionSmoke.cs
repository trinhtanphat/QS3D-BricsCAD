using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetPlacementPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPrecisionLostStartAtPaperBoundary();
            PreservesExactBoundaryPlacement();
            PreservesOrdinaryOutOfBoundsRejection();
        }

        private static void RejectsPrecisionLostStartAtPaperBoundary()
        {
            var views = BuildViews();
            try
            {
                _ = SemanticSheetPlanner.Build(
                    Sheet(
                        paperWidthMm: 1e16,
                        placementXmm: 1d,
                        placementWidthMm: 1e16),
                    views);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("lost a non-zero start coordinate", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    throw new Exception(
                        "Semantic sheet precision regression failed for the wrong reason: " + ex.Message);
                }
                return;
            }

            throw new Exception(
                "Semantic sheet bounds must reject a finite positive start that binary64 subtraction completely loses.");
        }

        private static void PreservesExactBoundaryPlacement()
        {
            var plan = SemanticSheetPlanner.Build(
                Sheet(
                    paperWidthMm: 100d,
                    placementXmm: 1d,
                    placementWidthMm: 99d),
                BuildViews());

            if (plan.Placements.Count != 1 ||
                plan.Placements[0].Xmm != 1d ||
                plan.Placements[0].WidthMm != 99d)
            {
                throw new Exception("Exact-bound semantic sheet placement must remain accepted unchanged.");
            }
        }

        private static void PreservesOrdinaryOutOfBoundsRejection()
        {
            try
            {
                _ = SemanticSheetPlanner.Build(
                    Sheet(
                        paperWidthMm: 100d,
                        placementXmm: 2d,
                        placementWidthMm: 99d),
                    BuildViews());
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception("Ordinary out-of-bounds semantic sheet placement must remain rejected.");
        }

        private static SemanticSheetDefinition Sheet(
            double paperWidthMm,
            double placementXmm,
            double placementWidthMm)
        {
            return new SemanticSheetDefinition(
                "SHEET-PRECISION",
                "A-001",
                "Precision",
                paperWidthMm,
                100d,
                new[]
                {
                    new SemanticSheetPlacementDefinition(
                        "VIEW-1",
                        placementXmm,
                        10d,
                        placementWidthMm,
                        20d)
                });
        }

        private static IReadOnlyList<SemanticViewPlan> BuildViews()
        {
            var project = new ProjectState("P-SHEET-PRECISION", "Sheet precision smoke");
            return SemanticViewPlanner.BuildCatalog(
                project,
                new[] { new SemanticViewDefinition("VIEW-1", "View 1") });
        }
    }
}
