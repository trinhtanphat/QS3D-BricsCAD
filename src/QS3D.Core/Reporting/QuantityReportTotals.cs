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
            foreach (var row in rows)
            {
                if (row == null) continue;
                totals.Count += row.Count;
                totals.GrossConcreteM3 += row.GrossConcreteM3;
                totals.DeductionM3 += row.DeductionM3;
                totals.NetConcreteM3 += row.NetConcreteM3;
                totals.FormworkM2 += row.FormworkM2;
                totals.LengthM += row.LengthM;
                totals.DoorAreaM2 += row.DoorAreaM2;
            }
            return totals;
        }
    }
}
