using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarStockDemandSmoke
    {
        public static void Run()
        {
            DemandKeepsMaterialComponentsSeparate();
            DuplicateCutIdentityFailsClosed();
            NonFinitePolicyFailsClosed();
            ProcurementKeepsOffCutSeparate();
            ExcessOffCutFailsClosed();
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
            Near(11d, demand.RequiredCutLengthM);
            Near(0.06d, demand.AllowanceLengthM);
            Near(0.009d, demand.KerfLengthM);
            Near(11.069d, demand.FabricationDemandLengthM);
            Equal("G-01", demand.GroupId);
            Equal("CB400-V", demand.Grade);
        }

        private static void DuplicateCutIdentityFailsClosed()
        {
            Throws<ArgumentException>(() => new RebarStockDemand(
                "G-02",
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

        private static void ProcurementKeepsOffCutSeparate()
        {
            var procurement = new RebarStockProcurementQuantities(12d, 1, 0.931d);
            Equal(1, procurement.StockBarCount);
            Near(12d, procurement.ProcurementLengthM);
            Near(0.931d, procurement.OffCutLengthM);
        }

        private static void ExcessOffCutFailsClosed()
        {
            Throws<ArgumentOutOfRangeException>(() => new RebarStockProcurementQuantities(12d, 1, 12.001d));
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
