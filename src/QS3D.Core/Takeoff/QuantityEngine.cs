using System;
using System.Collections.Generic;
using QS3D.Core.Measurement;
using QS3D.Core.Model;
using QS3D.Core.Units;

namespace QS3D.Core.Takeoff
{
    public static class QuantityEngine
    {
        public static TakeoffResult Calculate(EntitySnapshot entity, TakeoffKind kind, DrawingUnit drawingUnit)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (!Enum.IsDefined(typeof(TakeoffKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (!Enum.IsDefined(typeof(DrawingUnit), drawingUnit)) throw new ArgumentOutOfRangeException(nameof(drawingUnit));

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

        public static TakeoffResultWithTrace CalculateWithTrace(EntitySnapshot entity, TakeoffKind kind, DrawingUnit drawingUnit)
        {
            var result = Calculate(entity, kind, drawingUnit);
            var facts = BuildTraceFacts(entity, kind, drawingUnit);
            var assumptions = kind == TakeoffKind.Count
                ? Array.Empty<string>()
                : new[] { "Conversion path: " + drawingUnit + " -> " + result.Unit };

            var trace = new MeasurementTrace(
                result.Handle,
                result.Handle,
                result.Kind.ToString(),
                facts,
                result.Value,
                Array.Empty<MeasurementTraceAdjustment>(),
                result.Value,
                result.Unit,
                "none",
                assumptions: assumptions);

            return new TakeoffResultWithTrace(result, trace);
        }

        private static IReadOnlyList<MeasurementTraceFact> BuildTraceFacts(EntitySnapshot entity, TakeoffKind kind, DrawingUnit drawingUnit)
        {
            switch (kind)
            {
                case TakeoffKind.Count:
                    return Array.Empty<MeasurementTraceFact>();
                case TakeoffKind.Length:
                    return new[]
                    {
                        new MeasurementTraceFact(
                            "RawLength",
                            entity.LengthDrawingUnits.GetValueOrDefault(),
                            DrawingUnitToken(drawingUnit, 1),
                            entity.Handle)
                    };
                case TakeoffKind.Area:
                    return new[]
                    {
                        new MeasurementTraceFact(
                            "RawArea",
                            entity.AreaDrawingUnitsSquared.GetValueOrDefault(),
                            DrawingUnitToken(drawingUnit, 2),
                            entity.Handle)
                    };
                case TakeoffKind.Volume:
                    return new[]
                    {
                        new MeasurementTraceFact(
                            "RawVolume",
                            entity.VolumeDrawingUnitsCubed.GetValueOrDefault(),
                            DrawingUnitToken(drawingUnit, 3),
                            entity.Handle)
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static string DrawingUnitToken(DrawingUnit drawingUnit, int power)
        {
            var token = drawingUnit.ToString().ToLowerInvariant();
            return power == 1 ? token : token + power;
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
