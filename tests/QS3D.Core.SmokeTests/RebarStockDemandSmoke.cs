using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarStockDemandSmoke
    {
        public static void Run()
        {
            DemandKeepsMaterialComponentsSeparate();
            CanonicalIdentityFailsClosed();
            DuplicateCutIdentityFailsClosed();
            NonFinitePolicyFailsClosed();
            ProcurementKeepsKerfAndOffCutSeparate();
            ExcessWasteFailsClosed();
        }

        private static void DemandKeepsMaterialComponentsSeparate()
        {
            var demand = new RebarStockDemand(
                "G-01",
                "CB400-V",
                20d,
                12d,
                new[]
                {
                    new RebarCutRequirement("C-01", 4.5d, 2),
                    new RebarCutRequirement("C-02", 2d, 1)
                },
                new RebarCutAllowancePolicy(0.003d, 0.02d));

            Equal(3L, demand.RequiredCutCount);
            Equal(2, demand.RequiredCuts.Count);
            Near(11d, demand.RequiredCutLengthM);
            Near(0.06d, demand.AllowanceLengthM);
            Near(11.06d, demand.DemandLengthBeforeKerfM);
            Near(0.003d, demand.AllowancePolicy.KerfPerCutM);
            Near(20d, demand.DiameterMm);
            Near(12d, demand.StockLengthM);
            Equal("G-01", demand.GroupId);
            Equal("CB400-V", demand.Grade);
        }

        private static void CanonicalIdentityFailsClosed()
        {
            Throws<ArgumentException>(() => new RebarCutRequirement(" CUT-A", 3d, 1));
            Throws<ArgumentException>(() => new RebarStockDemand(
                "G-02 ",
                "CB500-V",
                16d,
                12d,
                new[] { new RebarCutRequirement("CUT-A", 3d, 1) },
                new RebarCutAllowancePolicy()));
        }

        private static void DuplicateCutIdentityFailsClosed()
        {
            Throws<ArgumentException>(() => new RebarStockDemand(
                "G-03",
                "CB500-V",
                16d,
                12d,
                new[]
                {
                    new RebarCutRequirement("CUT-A", 3d, 1),
                    new RebarCutRequirement("cut-a", 2d, 1)
                },
                new RebarCutAllowancePolicy()));
        }

        private static void NonFinitePolicyFailsClosed()
        {
            Throws<ArgumentOutOfRangeException>(() => new RebarCutAllowancePolicy(double.NaN, 0d));
            Throws<ArgumentOutOfRangeException>(() => new RebarCutAllowancePolicy(0d, double.PositiveInfinity));
        }

        private static void ProcurementKeepsKerfAndOffCutSeparate()
        {
            var procurement = new RebarStockProcurementQuantities(12d, 1, 0.009d, 0.931d);
            Equal(1, procurement.StockBarCount);
            Near(12d, procurement.ProcurementLengthM);
            Near(0.009d, procurement.KerfLengthM);
            Near(0.931d, procurement.OffCutLengthM);
        }

        private static void ExcessWasteFailsClosed()
        {
            Throws<ArgumentOutOfRangeException>(() => new RebarStockProcurementQuantities(12d, 1, 0.01d, 12d));
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(long expected, long actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
