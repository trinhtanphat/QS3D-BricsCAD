using System;
using System.Collections.Generic;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CostRateBuildUpAdditivePrecisionSmoke
    {
        internal static void Run()
        {
            SwallowedDirectContributionFailsClosed();
            SwallowedAccumulatedSubtotalFailsClosed();
            RepresentableLowOrderContributionRemainsAccepted();
            OrdinaryPercentageCompositionRemainsStable();
        }

        private static void SwallowedDirectContributionFailsClosed()
        {
            var components = new[]
            {
                new CostResourceComponent(
                    "A-LARGE",
                    "Large direct component",
                    "ea",
                    1m,
                    70000000000000000000000000000m),
                new CostResourceComponent(
                    "B-SMALL",
                    "Small direct component",
                    "ea",
                    1m,
                    0.1m)
            };

            Throws<OverflowException>(() =>
                new CostRateBuildUp(
                    "BUILD-SWALLOWED-DIRECT",
                    new CostCode("CONC"),
                    "ea",
                    "VND",
                    components));
        }

        private static void SwallowedAccumulatedSubtotalFailsClosed()
        {
            var components = new[]
            {
                new CostResourceComponent(
                    "A-SMALL",
                    "Small direct component",
                    "ea",
                    1m,
                    0.1m),
                new CostResourceComponent(
                    "B-LARGE",
                    "Large direct component",
                    "ea",
                    1m,
                    70000000000000000000000000000m)
            };

            Throws<OverflowException>(() =>
                new CostRateBuildUp(
                    "BUILD-SWALLOWED-ACCUMULATED",
                    new CostCode("CONC"),
                    "ea",
                    "VND",
                    components));
        }

        private static void RepresentableLowOrderContributionRemainsAccepted()
        {
            var buildUp = new CostRateBuildUp(
                "BUILD-REPRESENTABLE-DIRECT",
                new CostCode("CONC"),
                "ea",
                "VND",
                new[]
                {
                    new CostResourceComponent(
                        "A-LARGE",
                        "Large direct component",
                        "ea",
                        1m,
                        70000000000000000000000000000m),
                    new CostResourceComponent(
                        "B-ONE",
                        "Representable low-order component",
                        "ea",
                        1m,
                        1m)
                });

            Equal(
                70000000000000000000000000001m,
                buildUp.DirectUnitCost,
                "Representable low-order direct contribution changed.");
            Equal(
                buildUp.DirectUnitCost,
                buildUp.UnitRate,
                "Zero overhead/profit composition should preserve direct cost.");
        }

        private static void OrdinaryPercentageCompositionRemainsStable()
        {
            var buildUp = new CostRateBuildUp(
                "BUILD-NORMAL-PERCENTAGES",
                new CostCode("CONC"),
                "ea",
                "VND",
                new[]
                {
                    new CostResourceComponent("MAT", "Material", "ea", 1m, 100m)
                },
                overheadPercent: 10m,
                profitPercent: 10m);

            Equal(100m, buildUp.DirectUnitCost, "Ordinary direct cost changed.");
            Equal(10m, buildUp.OverheadUnitCost, "Ordinary overhead cost changed.");
            Equal(11m, buildUp.ProfitUnitCost, "Ordinary profit cost changed.");
            Equal(121m, buildUp.UnitRate, "Ordinary unit-rate composition changed.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
