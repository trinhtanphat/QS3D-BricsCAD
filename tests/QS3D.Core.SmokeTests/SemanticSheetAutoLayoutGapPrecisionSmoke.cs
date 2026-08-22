using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetAutoLayoutGapPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LostHorizontalGapFailsClosed();
            LostVerticalGapFailsClosed();
            OrdinaryPackingRemainsStable();
        }

        private static void LostHorizontalGapFailsClosed()
        {
            const double largeWidth = 1e16d;
            if (largeWidth + 1d != largeWidth)
                throw new InvalidOperationException("Auto-layout horizontal-gap smoke requires a 1 mm gap below the local double ULP.");

            MustFail(
                () => SemanticSheetAutoLayoutPlanner.Build(
                    new[]
                    {
                        new SemanticSheetAutoLayoutItem("A", largeWidth, 20d),
                        new SemanticSheetAutoLayoutItem("B", 20d, 20d)
                    },
                    BuildViews("A", "B"),
                    Options(
                        paperWidth: 3e16d,
                        paperHeight: 100d,
                        horizontalGap: 1d,
                        verticalGap: 0d)),
                "horizontal gap");
        }

        private static void LostVerticalGapFailsClosed()
        {
            const double largeExtent = 1e16d;
            if (largeExtent + 1d != largeExtent)
                throw new InvalidOperationException("Auto-layout vertical-gap smoke requires a 1 mm gap below the local double ULP.");

            MustFail(
                () => SemanticSheetAutoLayoutPlanner.Build(
                    new[]
                    {
                        new SemanticSheetAutoLayoutItem("A", largeExtent, largeExtent),
                        new SemanticSheetAutoLayoutItem("B", largeExtent, largeExtent)
                    },
                    BuildViews("A", "B"),
                    Options(
                        paperWidth: largeExtent,
                        paperHeight: 3e16d,
                        horizontalGap: 0d,
                        verticalGap: 1d)),
                "vertical gap");
        }

        private static void OrdinaryPackingRemainsStable()
        {
            var plans = SemanticSheetAutoLayoutPlanner.Build(
                new[]
                {
                    new SemanticSheetAutoLayoutItem("A", 50d, 20d),
                    new SemanticSheetAutoLayoutItem("B", 40d, 20d)
                },
                BuildViews("A", "B"),
                new SemanticSheetAutoLayoutOptions(
                    "AUTO",
                    "A-",
                    "Auto Sheet",
                    200d,
                    100d,
                    marginLeftMm: 10d,
                    marginTopMm: 10d,
                    marginRightMm: 10d,
                    marginBottomMm: 10d,
                    horizontalGapMm: 8d,
                    verticalGapMm: 7d));

            if (plans.Count != 1 || plans[0].Placements.Count != 2)
                throw new InvalidOperationException("Ordinary auto-layout packing changed sheet or placement count during gap precision hardening.");

            var first = plans[0].Placements.Single(x => string.Equals(x.ViewId, "A", StringComparison.Ordinal));
            var second = plans[0].Placements.Single(x => string.Equals(x.ViewId, "B", StringComparison.Ordinal));
            if (first.Xmm != 10d || first.Ymm != 10d || second.Xmm != 68d || second.Ymm != 10d)
                throw new InvalidOperationException("Ordinary auto-layout gap coordinates changed during precision hardening.");
        }

        private static SemanticSheetAutoLayoutOptions Options(
            double paperWidth,
            double paperHeight,
            double horizontalGap,
            double verticalGap)
        {
            return new SemanticSheetAutoLayoutOptions(
                "AUTO",
                "A-",
                "Auto Sheet",
                paperWidth,
                paperHeight,
                marginLeftMm: 0d,
                marginTopMm: 0d,
                marginRightMm: 0d,
                marginBottomMm: 0d,
                horizontalGapMm: horizontalGap,
                verticalGapMm: verticalGap);
        }

        private static IReadOnlyList<SemanticViewPlan> BuildViews(params string[] ids)
        {
            var project = new ProjectState("P-AUTO-GAP-PRECISION", "Auto Gap Precision Smoke");
            var definitions = new List<SemanticViewDefinition>();
            foreach (var id in ids) definitions.Add(new SemanticViewDefinition(id, "View " + id));
            return SemanticViewPlanner.BuildCatalog(project, definitions);
        }

        private static void MustFail(Action action, string expectedMessageFragment)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException(
                    "Auto-layout failed for an unexpected reason while guarding gap precision: " + ex.Message,
                    ex);
            }

            throw new InvalidOperationException("Auto-layout must fail closed when a configured positive gap is lost to floating-point precision.");
        }
    }
}
