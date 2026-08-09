using System;
using System.Collections.Generic;
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
            var grouped = new Dictionary<string, QuantityReportRow>(StringComparer.OrdinalIgnoreCase); var order = new List<string>();
            foreach (var item in snapshots)
            {
                var key = item.EntityType + "\u001f" + item.Layer;
                if (!grouped.TryGetValue(key, out var row)) { row = new QuantityReportRow { Floor = "Selection", Category = item.EntityType, FamilyName = item.Layer }; grouped.Add(key, row); order.Add(key); }
                row.Count++;
                if (item.LengthDrawingUnits.HasValue) row.LengthM += UnitScale.ToMeters(item.LengthDrawingUnits.Value, unit);
                if (item.AreaDrawingUnitsSquared.HasValue) row.SideAreaM2 += UnitScale.ToSquareMeters(item.AreaDrawingUnitsSquared.Value, unit);
                if (item.VolumeDrawingUnitsCubed.HasValue) row.NetConcreteM3 += UnitScale.ToCubicMeters(item.VolumeDrawingUnitsCubed.Value, unit);
            }
            var result = new List<QuantityReportRow>(order.Count); foreach (var key in order) result.Add(grouped[key]); return result;
        }
    }
}
