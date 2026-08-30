using System;
using System.Collections;
using System.Collections.Generic;

namespace QS3D.Core.Reporting
{
    public sealed class QuantityReportTotals
    {
        public int Count { get; private set; }
        public double GrossConcreteM3 { get; private set; }
        public double DeductionM3 { get; private set; }
        public double NetConcreteM3 { get; private set; }
        public double FormworkM2 { get; private set; }
        public double LengthM { get; private set; }
        public double DoorAreaM2 { get; private set; }

        public static QuantityReportTotals FromRows(IEnumerable<QuantityReportRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var knownCount = SnapshotKnownRowCount(rows, out var knownCountSources);
            var totals = new QuantityReportTotals();
            var grossConcreteCompensation = 0d;
            var deductionCompensation = 0d;
            var netConcreteCompensation = 0d;
            var formworkCompensation = 0d;
            var lengthCompensation = 0d;
            var doorAreaCompensation = 0d;
            var rowIndex = 0;

            using (var enumerator = rows.GetEnumerator())
            {
                while (true)
                {
                    RequireKnownRowCountStable(rows, knownCount, knownCountSources);
                    if (!enumerator.MoveNext())
                    {
                        RequireKnownRowCountStable(rows, knownCount, knownCountSources);
                        break;
                    }

                    RequireKnownRowCountStable(rows, knownCount, knownCountSources);
                    if (knownCount.HasValue && rowIndex >= knownCount.Value)
                        throw RowCountMismatch(knownCount.Value, rowIndex + 1);

                    var row = enumerator.Current;
                    RequireKnownRowCountStable(rows, knownCount, knownCountSources);
                    if (row == null)
                        throw new ArgumentException("Quantity report rows cannot contain null entries. Invalid row index: " + rowIndex + ".", nameof(rows));
                    totals.Count = QuantityReportMath.AddCount(totals.Count, row.Count);
                    totals.GrossConcreteM3 = Add(totals.GrossConcreteM3, ref grossConcreteCompensation, row.GrossConcreteM3, rowIndex, "GrossConcreteM3");
                    totals.DeductionM3 = Add(totals.DeductionM3, ref deductionCompensation, row.DeductionM3, rowIndex, "DeductionM3");
                    totals.NetConcreteM3 = Add(totals.NetConcreteM3, ref netConcreteCompensation, row.NetConcreteM3, rowIndex, "NetConcreteM3");
                    totals.FormworkM2 = Add(totals.FormworkM2, ref formworkCompensation, row.FormworkM2, rowIndex, "FormworkM2");
                    totals.LengthM = Add(totals.LengthM, ref lengthCompensation, row.LengthM, rowIndex, "LengthM");
                    totals.DoorAreaM2 = Add(totals.DoorAreaM2, ref doorAreaCompensation, row.DoorAreaM2, rowIndex, "DoorAreaM2");
                    rowIndex++;
                }
            }

            if (knownCount.HasValue && rowIndex != knownCount.Value)
                throw RowCountMismatch(knownCount.Value, rowIndex);

            RequireKnownRowCountStable(rows, knownCount, knownCountSources);

            totals.GrossConcreteM3 = Finalize(totals.GrossConcreteM3, grossConcreteCompensation, "GrossConcreteM3");
            totals.DeductionM3 = Finalize(totals.DeductionM3, deductionCompensation, "DeductionM3");
            totals.NetConcreteM3 = Finalize(totals.NetConcreteM3, netConcreteCompensation, "NetConcreteM3");
            totals.FormworkM2 = Finalize(totals.FormworkM2, formworkCompensation, "FormworkM2");
            totals.LengthM = Finalize(totals.LengthM, lengthCompensation, "LengthM");
            totals.DoorAreaM2 = Finalize(totals.DoorAreaM2, doorAreaCompensation, "DoorAreaM2");
            return totals;
        }

        private static void RequireKnownRowCountStable(
            IEnumerable<QuantityReportRow> rows,
            int? expectedKnownCount,
            int expectedKnownCountSources)
        {
            var currentKnownCount = SnapshotKnownRowCount(rows, out var currentKnownCountSources);
            if (expectedKnownCount != currentKnownCount || expectedKnownCountSources != currentKnownCountSources)
                throw new InvalidOperationException("Quantity report row input Count changed during enumeration.");
        }

        private static int? SnapshotKnownRowCount(IEnumerable<QuantityReportRow> rows, out int knownCountSources)
        {
            int? knownCount = null;
            var sources = 0;
            if (rows is ICollection<QuantityReportRow> genericCollection)
            {
                sources |= 1;
                ObserveKnownRowCount(genericCollection.Count, ref knownCount);
            }
            if (rows is IReadOnlyCollection<QuantityReportRow> readOnlyCollection)
            {
                sources |= 2;
                ObserveKnownRowCount(readOnlyCollection.Count, ref knownCount);
            }
            if (rows is ICollection nonGenericCollection)
            {
                sources |= 4;
                ObserveKnownRowCount(nonGenericCollection.Count, ref knownCount);
            }
            knownCountSources = sources;
            return knownCount;
        }

        private static void ObserveKnownRowCount(int count, ref int? knownCount)
        {
            if (count < 0)
                throw new InvalidOperationException("Quantity report row input reported a negative known count.");
            if (knownCount.HasValue && knownCount.Value != count)
                throw new InvalidOperationException(
                    "Quantity report row input exposes conflicting known counts: " + knownCount.Value + " and " + count + ".");
            knownCount = count;
        }

        private static InvalidOperationException RowCountMismatch(int reportedCount, int observedCount)
        {
            return new InvalidOperationException(
                "Quantity report row input changed during enumeration; Count reported " + reportedCount +
                " rows but enumeration produced " + observedCount + ".");
        }

        private static double Add(double current, ref double compensation, double value, int rowIndex, string quantity)
        {
            var label = "row " + rowIndex + "/" + quantity;
            QuantityReportMath.Finite(current, label);
            QuantityReportMath.Finite(compensation, label + "/compensation");
            var incoming = QuantityReportMath.NonNegative(value, label);

            var result = current + incoming;
            if (double.IsNaN(result) || double.IsInfinity(result))
                throw new OverflowException("Quantity report total overflow: " + label);

            var correction = Math.Abs(current) >= Math.Abs(incoming)
                ? (current - result) + incoming
                : (incoming - result) + current;
            var nextCompensation = compensation + correction;
            if (double.IsNaN(nextCompensation) || double.IsInfinity(nextCompensation))
                throw new OverflowException("Quantity report total compensation overflow: " + label);
            compensation = nextCompensation == 0d ? 0d : nextCompensation;
            return result == 0d ? 0d : result;
        }

        private static double Finalize(double current, double compensation, string quantity)
        {
            QuantityReportMath.Finite(current, "total/" + quantity);
            QuantityReportMath.Finite(compensation, "total/" + quantity + "/compensation");
            var result = current + compensation;
            if (double.IsNaN(result) || double.IsInfinity(result))
                throw new OverflowException("Quantity report total overflow: " + quantity);
            if (compensation != 0d && result == current && !IsStrictlyBelowHalfUlp(current, compensation))
                throw new OverflowException("Quantity report total lost a non-zero compensation at floating-point precision: " + quantity);
            if (current != 0d && result == compensation)
                throw new OverflowException("Quantity report total lost a non-zero accumulated value at floating-point precision: " + quantity);
            return result == 0d ? 0d : result;
        }

        private static bool IsStrictlyBelowHalfUlp(double current, double compensation)
        {
            if (current <= 0d || compensation == 0d) return false;

            var currentBits = BitConverter.DoubleToInt64Bits(current);
            var adjacentBits = compensation > 0d ? currentBits + 1L : currentBits - 1L;
            var adjacent = BitConverter.Int64BitsToDouble(adjacentBits);
            var spacing = Math.Abs(adjacent - current);
            return Math.Abs(compensation) < spacing / 2d;
        }
    }
}