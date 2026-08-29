using System;
using System.Collections.Generic;

namespace QS3D.Core.Reporting
{
    public enum QuantityGeometryRelation
    {
        None = 0,
        VolumeIntersection = 1,
        FaceContact = 2,
        FaceOverlap = 3
    }

    public sealed class QuantityGeometryTolerances
    {
        public const double DefaultVolume = 1e-8;
        public const double DefaultDistance = 1e-6;
        public const double DefaultArea = 1e-6;

        public QuantityGeometryTolerances(
            double volumeTolerance = DefaultVolume,
            double distanceTolerance = DefaultDistance,
            double areaTolerance = DefaultArea)
        {
            Volume = PositiveFinite(volumeTolerance, nameof(volumeTolerance));
            Distance = PositiveFinite(distanceTolerance, nameof(distanceTolerance));
            Area = PositiveFinite(areaTolerance, nameof(areaTolerance));
        }

        public double Volume { get; }
        public double Distance { get; }
        public double Area { get; }

        private static double PositiveFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new ArgumentOutOfRangeException(name, "Geometry tolerance must be finite and greater than zero.");
            return value;
        }
    }

    public sealed class QuantityGeometryDeduction
    {
        public string ElementId { get; set; } = string.Empty;
        public string ElementName { get; set; } = string.Empty;
        public QuantityGeometryRelation Relation { get; set; }
        public double Volume { get; set; }
        public double Area { get; set; }
        public string RegionKey { get; set; } = string.Empty;
        public string FaceId { get; set; } = string.Empty;
        public IReadOnlyList<string> SourceHandles { get; set; } = Array.Empty<string>();
    }

    public sealed class QuantityFormworkFaceExplanation
    {
        public const string BrepRectangleExtentsMeasurementKind = "brep-rectangle-extents-v1";

        public string FaceId { get; set; } = string.Empty;
        public string SemanticKey { get; set; } = string.Empty;
        public string FaceType { get; set; } = "Other";
        public double GrossArea { get; set; }
        public double DeductionArea { get; set; }
        public double NetArea { get; set; }
        public string MeasurementKind { get; set; } = string.Empty;
        public double MeasurementLength { get; set; }
        public double MeasurementHeight { get; set; }
        public IReadOnlyList<QuantityGeometryDeduction> Deductions { get; set; } = Array.Empty<QuantityGeometryDeduction>();

        public bool HasMeasurementTrace =>
            string.Equals(MeasurementKind, BrepRectangleExtentsMeasurementKind, StringComparison.Ordinal) &&
            MeasurementLength > 0d &&
            MeasurementHeight > 0d;
    }

    public sealed class QuantityGeometryExplanation
    {
        public string ElementId { get; set; } = string.Empty;
        public string ElementName { get; set; } = string.Empty;
        public IReadOnlyList<string> SourceHandles { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> Dependencies { get; set; } = Array.Empty<string>();
        public string GeometryFingerprint { get; set; } = string.Empty;
        public bool IsDirty { get; set; }
        public double GrossVolume { get; set; }
        public double DeductionVolume { get; set; }
        public double NetVolume { get; set; }
        public IReadOnlyList<QuantityGeometryDeduction> VolumeDeductions { get; set; } = Array.Empty<QuantityGeometryDeduction>();
        public IReadOnlyList<QuantityFormworkFaceExplanation> FormworkFaces { get; set; } = Array.Empty<QuantityFormworkFaceExplanation>();
        public IReadOnlyList<string> Diagnostics { get; set; } = Array.Empty<string>();

        public double GrossFormworkArea => SumFormworkArea(FormworkFaces, x => x.GrossArea, nameof(GrossFormworkArea));
        public double DeductionFormworkArea => SumFormworkArea(FormworkFaces, x => x.DeductionArea, nameof(DeductionFormworkArea));
        public double NetFormworkArea => SumFormworkArea(FormworkFaces, x => x.NetArea, nameof(NetFormworkArea));

        public void Validate(QuantityGeometryTolerances tolerances)
        {
            if (tolerances == null) throw new ArgumentNullException(nameof(tolerances));
            NonNegativeFinite(GrossVolume, nameof(GrossVolume));
            NonNegativeFinite(DeductionVolume, nameof(DeductionVolume));
            NonNegativeFinite(NetVolume, nameof(NetVolume));
            if (DeductionVolume > GrossVolume + tolerances.Volume)
                throw new InvalidOperationException("Union deduction volume exceeds gross target volume.");
            if (Math.Abs(NetVolume - Math.Max(0d, GrossVolume - DeductionVolume)) > tolerances.Volume)
                throw new InvalidOperationException("Net volume is not gross volume minus union deduction volume.");

            var faces = FormworkFaces ?? throw new InvalidOperationException("FormworkFaces cannot be null.");
            for (var index = 0; index < faces.Count; index++)
            {
                var face = faces[index] ?? throw new InvalidOperationException("FormworkFaces cannot contain null entries.");
                NonNegativeFinite(face.GrossArea, face.FaceId + "/GrossArea");
                NonNegativeFinite(face.DeductionArea, face.FaceId + "/DeductionArea");
                NonNegativeFinite(face.NetArea, face.FaceId + "/NetArea");
                NonNegativeFinite(face.MeasurementLength, face.FaceId + "/MeasurementLength");
                NonNegativeFinite(face.MeasurementHeight, face.FaceId + "/MeasurementHeight");
                if (face.DeductionArea > face.GrossArea + tolerances.Area)
                    throw new InvalidOperationException(face.FaceId + " deduction area exceeds gross face area.");
                if (Math.Abs(face.NetArea - Math.Max(0d, face.GrossArea - face.DeductionArea)) > tolerances.Area)
                    throw new InvalidOperationException(face.FaceId + " net area is not gross area minus deduction area.");

                var hasKind = !string.IsNullOrWhiteSpace(face.MeasurementKind);
                var hasLength = face.MeasurementLength > 0d;
                var hasHeight = face.MeasurementHeight > 0d;
                if (hasKind != hasLength || hasKind != hasHeight)
                    throw new InvalidOperationException(face.FaceId + " measurement trace must provide kind, length and height together.");
                if (hasKind && !string.Equals(
                        face.MeasurementKind,
                        QuantityFormworkFaceExplanation.BrepRectangleExtentsMeasurementKind,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(face.FaceId + " measurement trace kind is not supported.");
                }
                if (face.HasMeasurementTrace)
                {
                    var measuredArea = face.MeasurementLength * face.MeasurementHeight;
                    if (double.IsNaN(measuredArea) || double.IsInfinity(measuredArea))
                        throw new InvalidOperationException(face.FaceId + " measurement trace area must be finite.");
                    var measurementAreaTolerance = Math.Max(tolerances.Area, Math.Abs(face.GrossArea) * 1e-8d);
                    if (Math.Abs(measuredArea - face.GrossArea) > measurementAreaTolerance)
                        throw new InvalidOperationException(face.FaceId + " measurement trace does not reconcile with exact BREP gross area.");
                }
            }

            _ = SumFormworkArea(faces, x => x.GrossArea, nameof(GrossFormworkArea));
            _ = SumFormworkArea(faces, x => x.DeductionArea, nameof(DeductionFormworkArea));
            _ = SumFormworkArea(faces, x => x.NetArea, nameof(NetFormworkArea));
        }

        private static double SumFormworkArea(
            IReadOnlyList<QuantityFormworkFaceExplanation>? faces,
            Func<QuantityFormworkFaceExplanation, double> selector,
            string label)
        {
            if (faces == null) throw new InvalidOperationException("FormworkFaces cannot be null.");
            var total = 0d;
            var compensation = 0d;
            for (var index = 0; index < faces.Count; index++)
            {
                var face = faces[index] ?? throw new InvalidOperationException("FormworkFaces cannot contain null entries.");
                var value = QuantityReportMath.NonNegative(selector(face), label + "[" + index + "]");
                AddCompensated(ref total, ref compensation, value, label + "[" + index + "]");
            }
            return FinalizeCompensated(total, compensation, label);
        }

        private static void AddCompensated(ref double total, ref double compensation, double value, string label)
        {
            QuantityReportMath.Finite(total, label + "/sum");
            QuantityReportMath.Finite(compensation, label + "/compensation");
            var nextTotal = total + value;
            if (double.IsNaN(nextTotal) || double.IsInfinity(nextTotal))
                throw new OverflowException("Formwork explanation total overflow: " + label);

            var correction = Math.Abs(total) >= Math.Abs(value)
                ? (total - nextTotal) + value
                : (value - nextTotal) + total;
            var nextCompensation = compensation + correction;
            if (double.IsNaN(nextCompensation) || double.IsInfinity(nextCompensation))
                throw new OverflowException("Formwork explanation compensation overflow: " + label);

            total = nextTotal == 0d ? 0d : nextTotal;
            compensation = nextCompensation == 0d ? 0d : nextCompensation;
        }

        private static double FinalizeCompensated(double total, double compensation, string label)
        {
            QuantityReportMath.Finite(total, label + "/sum");
            QuantityReportMath.Finite(compensation, label + "/compensation");
            var result = total + compensation;
            if (double.IsNaN(result) || double.IsInfinity(result))
                throw new OverflowException("Formwork explanation total overflow: " + label);
            if (compensation != 0d && result == total && !IsStrictlyBelowHalfUlp(total, compensation))
                throw new OverflowException("Formwork explanation total lost a non-zero compensation at floating-point precision: " + label);
            if (total != 0d && result == compensation)
                throw new OverflowException("Formwork explanation total lost a non-zero accumulated value at floating-point precision: " + label);
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

        private static void NonNegativeFinite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(label + " must be a finite non-negative number.");
        }
    }
}