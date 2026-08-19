using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportTotalsFinalCompensationSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            RejectsSwallowedFinalCompensation();
            PreservesRepresentableCompensation();
            PreservesOrdinaryExactTotals();
            PreservesEmptyTotals();
        }

        private static void RejectsSwallowedFinalCompensation()
        {
            ExpectThrows<OverflowException>(() => QuantityReportTotals.FromRows(new[]
            {
                Row(9007199254740992d),
                Row(1d)
            }));
        }

        private static void PreservesRepresentableCompensation()
        {
            var totals = QuantityReportTotals.FromRows(new[]
            {
                Row(9007199254740992d),
                Row(1d),
                Row(1d)
            });

            Equal(9007199254740994d, totals.GrossConcreteM3,
                "Representable compensated GrossConcreteM3 total changed.");
            Equal(3, totals.Count, "Representable compensated row count changed.");
        }

        private static void PreservesOrdinaryExactTotals()
        {
            var totals = QuantityReportTotals.FromRows(new[]
            {
                Row(10d),
                Row(2d)
            });

            Equal(12d, totals.GrossConcreteM3, "Ordinary exact GrossConcreteM3 total changed.");
            Equal(2, totals.Count, "Ordinary exact row count changed.");
            Equal(0d, totals.DeductionM3, "Zero DeductionM3 total changed.");
            Equal(0d, totals.NetConcreteM3, "Zero NetConcreteM3 total changed.");
            Equal(0d, totals.FormworkM2, "Zero FormworkM2 total changed.");
            Equal(0d, totals.LengthM, "Zero LengthM total changed.");
            Equal(0d, totals.DoorAreaM2, "Zero DoorAreaM2 total changed.");
        }

        private static void PreservesEmptyTotals()
        {
            var totals = QuantityReportTotals.FromRows(Array.Empty<QuantityReportRow>());
            Equal(0, totals.Count, "Empty quantity report count changed.");
            Equal(0d, totals.GrossConcreteM3, "Empty GrossConcreteM3 total changed.");
        }

        private static QuantityReportRow Row(double grossConcreteM3) =>
            new QuantityReportRow
            {
                Count = 1,
                GrossConcreteM3 = grossConcreteM3
            };

        private static void Equal(double expected, double actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void ExpectThrows<TException>(Action action) where TException : Exception
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
