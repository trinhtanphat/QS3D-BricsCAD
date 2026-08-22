using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportTotalsCompensationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PreservesRepresentableSmallRowsAfterHugeRow();
            PreservesSmallRowsAcrossInputOrder();
            OrdinaryTotalsRemainUnchanged();
            InvalidAndOverflowingRowsStillFailClosed();
        }

        private static void PreservesRepresentableSmallRowsAfterHugeRow()
        {
            var rows = new[]
            {
                Row(1e16, 1),
                Row(1d, 1),
                Row(1d, 1)
            };

            AssertAllTotals(QuantityReportTotals.FromRows(rows), 10000000000000002d, "huge-plus-small total");
        }

        private static void PreservesSmallRowsAcrossInputOrder()
        {
            var rows = new[]
            {
                Row(1d, 1),
                Row(1e16, 1),
                Row(1d, 1)
            };

            AssertAllTotals(QuantityReportTotals.FromRows(rows), 10000000000000002d, "small-huge-small total");
        }

        private static void AssertAllTotals(QuantityReportTotals totals, double expected, string scenario)
        {
            Exact(expected, totals.GrossConcreteM3, "GrossConcreteM3 " + scenario);
            Exact(expected, totals.DeductionM3, "DeductionM3 " + scenario);
            Exact(expected, totals.NetConcreteM3, "NetConcreteM3 " + scenario);
            Exact(expected, totals.FormworkM2, "FormworkM2 " + scenario);
            Exact(expected, totals.LengthM, "LengthM " + scenario);
            Exact(expected, totals.DoorAreaM2, "DoorAreaM2 " + scenario);
            if (totals.Count != 3)
                throw new InvalidOperationException("Quantity report compensated totals changed checked row count aggregation.");
        }

        private static void OrdinaryTotalsRemainUnchanged()
        {
            var totals = QuantityReportTotals.FromRows(new[]
            {
                Row(2.5d, 2),
                Row(3.5d, 3)
            });

            Exact(6d, totals.GrossConcreteM3, "ordinary GrossConcreteM3 total");
            Exact(6d, totals.DeductionM3, "ordinary DeductionM3 total");
            Exact(6d, totals.NetConcreteM3, "ordinary NetConcreteM3 total");
            Exact(6d, totals.FormworkM2, "ordinary FormworkM2 total");
            Exact(6d, totals.LengthM, "ordinary LengthM total");
            Exact(6d, totals.DoorAreaM2, "ordinary DoorAreaM2 total");
            if (totals.Count != 5)
                throw new InvalidOperationException("Quantity report ordinary totals changed checked row count aggregation.");
        }

        private static void InvalidAndOverflowingRowsStillFailClosed()
        {
            Expect<InvalidOperationException>(
                () => QuantityReportTotals.FromRows(new[] { Row(-1d, 1) }),
                "negative quantity row");
            Expect<OverflowException>(
                () => QuantityReportTotals.FromRows(new[] { Row(double.MaxValue, 1), Row(double.MaxValue, 1) }),
                "overflowing quantity total");
        }

        private static QuantityReportRow Row(double value, int count)
        {
            return new QuantityReportRow
            {
                Count = count,
                GrossConcreteM3 = value,
                DeductionM3 = value,
                NetConcreteM3 = value,
                FormworkM2 = value,
                LengthM = value,
                DoorAreaM2 = value
            };
        }

        private static void Exact(double expected, double actual, string scenario)
        {
            if (actual != expected)
                throw new InvalidOperationException("Unexpected " + scenario + ": expected " + expected + ", got " + actual + ".");
        }

        private static void Expect<TException>(Action action, string scenario) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + " for " + scenario + ".");
        }
    }
}
