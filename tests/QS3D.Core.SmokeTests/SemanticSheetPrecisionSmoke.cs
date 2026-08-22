using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CollapsedSameOriginPlacementsFailClosed();
            CollapsedRightEdgeFailsClosed();
            OrdinaryPlacementsRemainValid();
        }

        private static void CollapsedSameOriginPlacementsFailClosed()
        {
            const double origin = 1e16d;
            if (origin + 1d != origin)
                throw new InvalidOperationException("Semantic sheet precision smoke requires a positive 1 mm extent below the local double ULP.");

            MustFail(() => SemanticSheetPlanner.Build(
                new SemanticSheetDefinition(
                    "S-OVERLAP", "A-98", "Precision Overlap", origin + 1024d, 200d,
                    new[]
                    {
                        new SemanticSheetPlacementDefinition("V1", origin, 10d, 1d, 20d),
                        new SemanticSheetPlacementDefinition("V2", origin, 10d, 1d, 20d)
                    }),
                BuildViews("V1", "V2")));
        }

        private static void CollapsedRightEdgeFailsClosed()
        {
            const double origin = 1e16d;
            var sheetWidth = origin + 1024d;
            if (sheetWidth + 1d != sheetWidth)
                throw new InvalidOperationException("Semantic sheet right-edge smoke requires a positive 1 mm extent below the local double ULP.");

            MustFail(() => SemanticSheetPlanner.Build(
                new SemanticSheetDefinition(
                    "S-BOUNDS", "A-99", "Precision Bounds", sheetWidth, 200d,
                    new[] { new SemanticSheetPlacementDefinition("V1", sheetWidth, 10d, 1d, 20d) }),
                BuildViews("V1")));
        }

        private static void OrdinaryPlacementsRemainValid()
        {
            var plan = SemanticSheetPlanner.Build(
                new SemanticSheetDefinition(
                    "S-NORMAL", "A-01", "Normal Sheet", 200d, 100d,
                    new[]
                    {
                        new SemanticSheetPlacementDefinition("V2", 70d, 10d, 50d, 30d),
                        new SemanticSheetPlacementDefinition("V1", 10d, 10d, 50d, 30d)
                    }),
                BuildViews("V1", "V2"));

            if (plan.Placements.Count != 2 || plan.Placements[0].ViewId != "V1" || plan.Placements[1].ViewId != "V2")
                throw new InvalidOperationException("Ordinary semantic sheet placement ordering changed during precision hardening.");
        }

        private static IReadOnlyList<SemanticViewPlan> BuildViews(params string[] ids)
        {
            var project = new ProjectState("P-SHEET-PRECISION", "Sheet Precision Smoke");
            var definitions = new List<SemanticViewDefinition>();
            foreach (var id in ids) definitions.Add(new SemanticViewDefinition(id, "View " + id));
            return SemanticViewPlanner.BuildCatalog(project, definitions);
        }

        private static void MustFail(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new InvalidOperationException("Semantic sheet planner must fail closed when positive placement extent collapses at the local floating-point precision.");
        }
    }
}
