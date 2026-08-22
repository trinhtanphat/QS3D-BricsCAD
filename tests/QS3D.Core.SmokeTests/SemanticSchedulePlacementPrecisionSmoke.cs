using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSchedulePlacementPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CollapsedOccupiedEdgeFailsClosed();
            OrdinaryPlacementRemainsStable();
        }

        private static void CollapsedOccupiedEdgeFailsClosed()
        {
            const double origin = 1e16d;
            if (origin + 1d != origin)
                throw new InvalidOperationException("Precision smoke requires a positive 1 mm extent below the local double ULP.");

            var views = BuildViews("V1");
            var sheet = SemanticSheetPlanner.Build(
                new SemanticSheetDefinition(
                    "S-PRECISION", "A-99", "Precision Sheet", origin + 1024d, 200d,
                    new[] { new SemanticSheetPlacementDefinition("V1", origin, 10d, 1d, 20d) }),
                views);

            MustFail(() => SemanticSchedulePlacementPlanner.Build(
                sheet,
                BuildSchedules("SCH-1"),
                new[] { new SemanticSchedulePlacementItem("SCH-1", 1d, 20d) },
                new SemanticSchedulePlacementOptions(
                    marginLeftMm: origin,
                    marginTopMm: 10d,
                    marginRightMm: 0d,
                    marginBottomMm: 0d,
                    horizontalGapMm: 0d,
                    verticalGapMm: 0d)));
        }

        private static void OrdinaryPlacementRemainsStable()
        {
            var sheet = SemanticSheetPlanner.Build(
                new SemanticSheetDefinition(
                    "S-NORMAL", "A-01", "Normal Sheet", 300d, 200d,
                    Array.Empty<SemanticSheetPlacementDefinition>()),
                Array.Empty<SemanticViewPlan>());
            var plan = SemanticSchedulePlacementPlanner.Build(
                sheet,
                BuildSchedules("SCH-1"),
                new[] { new SemanticSchedulePlacementItem("SCH-1", 50d, 30d) });

            if (plan.Placements.Count != 1 || plan.Placements[0].Xmm != 10d || plan.Placements[0].Ymm != 10d)
                throw new InvalidOperationException("Ordinary schedule placement changed while hardening precision-collapse handling.");
        }

        private static IReadOnlyList<SemanticViewPlan> BuildViews(params string[] ids)
        {
            var project = new ProjectState("P-SCHEDULE-PRECISION", "Schedule Precision Smoke");
            var definitions = new List<SemanticViewDefinition>();
            foreach (var id in ids) definitions.Add(new SemanticViewDefinition(id, "View " + id));
            return SemanticViewPlanner.BuildCatalog(project, definitions);
        }

        private static IReadOnlyList<SemanticScheduleDefinition> BuildSchedules(params string[] ids)
        {
            var result = new List<SemanticScheduleDefinition>();
            foreach (var id in ids)
                result.Add(new SemanticScheduleDefinition(
                    id,
                    "Schedule " + id,
                    "Schedule " + id,
                    Array.Empty<ElementCategory>(),
                    string.Empty,
                    string.Empty,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    new[] { new SemanticDocumentationColumn("ID", "{Id}") }));
            return result.AsReadOnly();
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
            throw new InvalidOperationException("Schedule placement must fail closed when a positive occupied edge collapses at the local floating-point precision.");
        }
    }
}
