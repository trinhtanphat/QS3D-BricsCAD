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
                case TakeoffKind.Count:
                    return new TakeoffResult(entity.Handle, kind, 1d, "ea");
                case TakeoffKind.Length:
                    return new TakeoffResult(entity.Handle, kind, ConvertMetric(entity.LengthDrawingUnits, entity.Handle, "length", value => UnitScale.ToMeters(value, drawingUnit)), "m");
                case TakeoffKind.Area:
                    return new TakeoffResult(entity.Handle, kind, ConvertMetric(entity.AreaDrawingUnitsSquared, entity.Handle, "area", value => UnitScale.ToSquareMeters(value, drawingUnit)), "m2");
                case TakeoffKind.Volume:
                    return new TakeoffResult(entity.Handle, kind, ConvertMetric(entity.VolumeDrawingUnitsCubed, entity.Handle, "volume", value => UnitScale.ToCubicMeters(value, drawingUnit)), "m3");
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static double ConvertMetric(double? raw, string handle, string label, Func<double, double> converter)
        {
            if (!raw.HasValue) throw new InvalidOperationException("Entity " + handle + " has no " + label + ".");
            if (double.IsNaN(raw.Value) || double.IsInfinity(raw.Value) || raw.Value < 0d)
                throw new InvalidOperationException("Entity " + handle + " has invalid " + label + ".");
            var result = converter(raw.Value);
            if (double.IsNaN(result) || double.IsInfinity(result) || result < 0d)
                throw new OverflowException("Converted " + label + " for entity " + handle + " is not finite.");
            return result;
        }
    }
}
