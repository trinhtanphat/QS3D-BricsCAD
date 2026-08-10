using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Model;
using QS3D.Core.Reporting;
using QS3D.Core.Units;

namespace QS3D.BricsCAD.V25.Reporting
{
    internal static class SnapshotQuantityAdapter
    {
        public static IReadOnlyList<QuantityReportRow> Build(IReadOnlyList<EntitySnapshot> snapshots, DrawingUnit unit)
        {
            if (snapshots == null) throw new ArgumentNullException(nameof(snapshots));
            var grouped = new Dictionary<string, QuantityReportRow>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();
            foreach (var item in snapshots)
            {
                if (item == null) throw new InvalidOperationException("Snapshot quantity input cannot contain null items.");
                var key = item.EntityType + "\u001f" + item.Layer;
                if (!grouped.TryGetValue(key, out var row))
                {
                    row = new QuantityReportRow { Floor = "Selection", Category = item.EntityType, FamilyName = item.Layer };
                    grouped.Add(key, row);
                    order.Add(key);
                }

                row.Count = checked(row.Count + 1);
                if (!string.IsNullOrWhiteSpace(item.Handle) && !row.SourceHandles.Any(x => string.Equals(x, item.Handle, StringComparison.OrdinalIgnoreCase)))
                    row.SourceHandles.Add(item.Handle);
                if (item.LengthDrawingUnits.HasValue)
                    row.LengthM = AddFinite(row.LengthM, UnitScale.ToMeters(item.LengthDrawingUnits.Value, unit), "LengthM");
                if (item.AreaDrawingUnitsSquared.HasValue)
                    row.SideAreaM2 = AddFinite(row.SideAreaM2, UnitScale.ToSquareMeters(item.AreaDrawingUnitsSquared.Value, unit), "SideAreaM2");
                if (item.VolumeDrawingUnitsCubed.HasValue)
                    row.NetConcreteM3 = AddFinite(row.NetConcreteM3, UnitScale.ToCubicMeters(item.VolumeDrawingUnitsCubed.Value, unit), "NetConcreteM3");
            }

            var result = new List<QuantityReportRow>(order.Count);
            foreach (var key in order) result.Add(grouped[key]);
            return result;
        }

        private static double AddFinite(double current, double value, string label)
        {
            if (!IsFinite(current) || !IsFinite(value))
                throw new InvalidOperationException("Snapshot quantity value is not finite: " + label + ".");
            var result = current + value;
            if (!IsFinite(result)) throw new OverflowException("Snapshot quantity total overflow: " + label + ".");
            return result;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
