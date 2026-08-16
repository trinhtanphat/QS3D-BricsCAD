using System;
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
            var totals = new QuantityReportTotals();
            var grossConcreteCompensation = 0d;
            var deductionCompensation = 0d;
            var netConcreteCompensation = 0d;
            var formworkCompensation = 0d;
            var lengthCompensation = 0d;
            var doorAreaCompensation = 0d;
            var rowIndex = 0;
            foreach (var row in rows)
            {
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
            return totals;
        }

        private static double Add(double current, ref double compensation, double value, int rowIndex, string quantity)
        {
            var label = "row " + rowIndex + "/" + quantity;
            QuantityReportMath.Finite(current, label);
            QuantityReportMath.Finite(compensation, label + "/compensation");
            var incoming = QuantityReportMath.NonNegative(value, label);
            var corrected = incoming - compensation;
            if (double.IsNaN(corrected) || double.IsInfinity(corrected))
                throw new OverflowException("Quantity report total overflow: " + label);

            var result = current + corrected;
            if (double.IsNaN(result) || double.IsInfinity(result))
                throw new OverflowException("Quantity report total overflow: " + label);

            var nextCompensation = (result - current) - corrected;
            if (double.IsNaN(nextCompensation) || double.IsInfinity(nextCompensation))
                throw new OverflowException("Quantity report total compensation overflow: " + label);
            compensation = nextCompensation;
            return result == 0d ? 0d : result;
        }
    }
}
