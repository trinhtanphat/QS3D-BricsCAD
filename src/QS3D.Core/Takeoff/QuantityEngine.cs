using System;
using QS3D.Core.Model;
using QS3D.Core.Units;

namespace QS3D.Core.Takeoff
{
    public static class QuantityEngine
    {
        public static TakeoffResult Calculate(EntitySnapshot entity, TakeoffKind kind, DrawingUnit drawingUnit)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            switch (kind)
            {
                case TakeoffKind.Count: return new TakeoffResult(entity.Handle, kind, 1d, "ea");
                case TakeoffKind.Length:
                    if (!entity.LengthDrawingUnits.HasValue) throw new InvalidOperationException($"Entity {entity.Handle} has no length.");
                    return new TakeoffResult(entity.Handle, kind, UnitScale.ToMeters(entity.LengthDrawingUnits.Value, drawingUnit), "m");
                case TakeoffKind.Area:
                    if (!entity.AreaDrawingUnitsSquared.HasValue) throw new InvalidOperationException($"Entity {entity.Handle} has no area.");
                    return new TakeoffResult(entity.Handle, kind, UnitScale.ToSquareMeters(entity.AreaDrawingUnitsSquared.Value, drawingUnit), "m2");
                case TakeoffKind.Volume:
                    if (!entity.VolumeDrawingUnitsCubed.HasValue) throw new InvalidOperationException($"Entity {entity.Handle} has no volume.");
                    return new TakeoffResult(entity.Handle, kind, UnitScale.ToCubicMeters(entity.VolumeDrawingUnitsCubed.Value, drawingUnit), "m3");
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
