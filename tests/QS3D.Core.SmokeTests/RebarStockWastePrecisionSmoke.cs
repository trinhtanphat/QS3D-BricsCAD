using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarStockWastePrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            RejectsSwallowedKerfContribution();
            RejectsSwallowedOffCutContribution();
            PreservesZeroAndOrdinaryWaste();
            PreservesExistingExcessWasteGuard();
        }

        private static void RejectsSwallowedKerfContribution()
        {
            ExpectThrows<InvalidOperationException>(
                () => new RebarStockProcurementQuantities(1e16, 1, 1d, 1e16));
        }

        private static void RejectsSwallowedOffCutContribution()
        {
            ExpectThrows<InvalidOperationException>(
                () => new RebarStockProcurementQuantities(1e16, 1, 1e16, 1d));
        }

        private static void PreservesZeroAndOrdinaryWaste()
        {
            var zeroCompanion = new RebarStockProcurementQuantities(1e16, 1, 0d, 1e16);
            Equal(1e16, zeroCompanion.ProcurementLengthM, "Zero-companion procurement length changed.");
            Equal(0d, zeroCompanion.KerfLengthM, "Zero kerf changed.");
            Equal(1e16, zeroCompanion.OffCutLengthM, "Zero-companion off-cut changed.");

            var ordinary = new RebarStockProcurementQuantities(10d, 1, 1d, 2d);
            Equal(10d, ordinary.ProcurementLengthM, "Ordinary procurement length changed.");
            Equal(1d, ordinary.KerfLengthM, "Ordinary kerf changed.");
            Equal(2d, ordinary.OffCutLengthM, "Ordinary off-cut changed.");
        }

        private static void PreservesExistingExcessWasteGuard()
        {
            ExpectThrows<ArgumentOutOfRangeException>(
                () => new RebarStockProcurementQuantities(2d, 1, 1.5d, 1d));
        }

        private static void Equal(double expected, double actual, string message)
        {
            if (expected != actual)
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void ExpectThrows<T>(Action action) where T : Exception
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
