using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetAutoLayoutMarginPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LostRightMarginFailsClosed();
            LostReservedBottomFailsClosed();
            OrdinaryMarginsRemainStable();
        }

        private static void LostRightMarginFailsClosed()
        {
            const double paperWidth = 1e16d;
            if (paperWidth - 1d != paperWidth)
                throw new InvalidOperationException("Auto-layout right-margin smoke requires a 1 mm subtraction below the local double ULP.");

            MustFail(() => SemanticSheetAutoLayoutPlanner.Build(
                new[] { new SemanticSheetAutoLayoutItem("V1", paperWidth, 20d) },
                BuildViews("V1"),
                Options(
                    paperWidth: paperWidth,
                    paperHeight: 100d,
                    marginLeft: 0d,
                    marginTop: 0d,
                    marginRight: 1d,
                    marginBottom: 0d,
                    reservedBottom: 0d)));
        }

        private static void LostReservedBottomFailsClosed()
        {
            const double paperHeight = 1e16d;
            if (paperHeight - 1d != paperHeight)
                throw new InvalidOperationException("Auto-layout reserved-bottom smoke requires a 1 mm subtraction below the local double ULP.");

            MustFail(() => SemanticSheetAutoLayoutPlanner.Build(
                new[] { new SemanticSheetAutoLayoutItem("V1", 20d, paperHeight) },
                BuildViews("V1"),
                Options(
                    paperWidth: 100d,
                    paperHeight: paperHeight,
                    marginLeft: 0d,
                    marginTop: 0d,
                    marginRight: 0d,
                    marginBottom: 0d,
                    reservedBottom: 1d)));
        }

        private static void OrdinaryMarginsRemainStable()
        {
            var plans = SemanticSheetAutoLayoutPlanner.Build(
                new[] { new SemanticSheetAutoLayoutItem("V1", 50d, 30d) },
                BuildViews("V1"),
                Options(
                    paperWidth: 200d,
                    paperHeight: 100d,
                    marginLeft: 10d,
                    marginTop: 10d,
                    marginRight: 10d,
                    marginBottom: 10d,
                    reservedBottom: 0d));

            if (plans.Count != 1 || plans[0].Placements.Count != 1 ||
                plans[0].Placements[0].Xmm != 10d || plans[0].Placements[0].Ymm != 10d)
                throw new InvalidOperationException("Ordinary auto-layout margins changed during precision hardening.");
        }

        private static SemanticSheetAutoLayoutOptions Options(
            double paperWidth,
            double paperHeight,
            double marginLeft,
            double marginTop,
            double marginRight,
            double marginBottom,
            double reservedBottom)
        {
            return new SemanticSheetAutoLayoutOptions(
                "AUTO",
                "A-",
                "Auto Sheet",
                paperWidth,
                paperHeight,
                marginLeft,
                marginTop,
                marginRight,
                marginBottom,
                horizontalGapMm: 0d,
                verticalGapMm: 0d,
                reservedBottomMm: reservedBottom);
        }

        private static IReadOnlyList<SemanticViewPlan> BuildViews(params string[] ids)
        {
            var project = new ProjectState("P-AUTO-MARGIN-PRECISION", "Auto Margin Precision Smoke");
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
            throw new InvalidOperationException("Auto-layout must fail closed when a configured positive margin is lost to floating-point precision.");
        }
    }
}
