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
            var rowIndex = 0;
            foreach (var row in rows)
            {
                if (row == null)
                    throw new ArgumentException("Quantity report rows cannot contain null entries. Invalid row index: " + rowIndex + ".", nameof(rows));
                totals.Count = QuantityReportMath.AddCount(totals.Count, row.Count);
                totals.GrossConcreteM3 = Add(totals.GrossConcreteM3, row.GrossConcreteM3, rowIndex, "GrossConcreteM3");
                totals.DeductionM3 = Add(totals.DeductionM3, row.DeductionM3, rowIndex, "DeductionM3");
                totals.NetConcreteM3 = Add(totals.NetConcreteM3, row.NetConcreteM3, rowIndex, "NetConcreteM3");
                totals.FormworkM2 = Add(totals.FormworkM2, row.FormworkM2, rowIndex, "FormworkM2");
                totals.LengthM = Add(totals.LengthM, row.LengthM, rowIndex, "LengthM");
                totals.DoorAreaM2 = Add(totals.DoorAreaM2, row.DoorAreaM2, rowIndex, "DoorAreaM2");
                rowIndex++;
            }
            return totals;
        }

        private static double Add(double current, double value, int rowIndex, string quantity)
        {
            var label = "row " + rowIndex + "/" + quantity;
            return QuantityReportMath.Add(current, QuantityReportMath.NonNegative(value, label), label);
        }
    }
}
