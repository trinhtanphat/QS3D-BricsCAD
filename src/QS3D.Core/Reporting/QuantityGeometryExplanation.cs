using System;
using System.Collections.Generic;
using System.Linq;

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
        public IReadOnlyList<string> SourceHandles { get; set; } = Array.Empty<string>();
    }

    public sealed class QuantityFormworkFaceExplanation
    {
        public string FaceId { get; set; } = string.Empty;
        public string FaceType { get; set; } = "Other";
        public double GrossArea { get; set; }
        public double DeductionArea { get; set; }
        public double NetArea { get; set; }
        public IReadOnlyList<QuantityGeometryDeduction> Deductions { get; set; } = Array.Empty<QuantityGeometryDeduction>();
    }

    public sealed class QuantityGeometryExplanation
    {
        public string ElementId { get; set; } = string.Empty;
        public string ElementName { get; set; } = string.Empty;
        public IReadOnlyList<string> SourceHandles { get; set; } = Array.Empty<string>();
        public double GrossVolume { get; set; }
        public double DeductionVolume { get; set; }
        public double NetVolume { get; set; }
        public IReadOnlyList<QuantityGeometryDeduction> VolumeDeductions { get; set; } = Array.Empty<QuantityGeometryDeduction>();
        public IReadOnlyList<QuantityFormworkFaceExplanation> FormworkFaces { get; set; } = Array.Empty<QuantityFormworkFaceExplanation>();
        public IReadOnlyList<string> Diagnostics { get; set; } = Array.Empty<string>();

        public double GrossFormworkArea => FormworkFaces.Sum(x => x.GrossArea);
        public double DeductionFormworkArea => FormworkFaces.Sum(x => x.DeductionArea);
        public double NetFormworkArea => FormworkFaces.Sum(x => x.NetArea);

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

            foreach (var face in FormworkFaces)
            {
                NonNegativeFinite(face.GrossArea, face.FaceId + "/GrossArea");
                NonNegativeFinite(face.DeductionArea, face.FaceId + "/DeductionArea");
                NonNegativeFinite(face.NetArea, face.FaceId + "/NetArea");
                if (face.DeductionArea > face.GrossArea + tolerances.Area)
                    throw new InvalidOperationException(face.FaceId + " deduction area exceeds gross face area.");
                if (Math.Abs(face.NetArea - Math.Max(0d, face.GrossArea - face.DeductionArea)) > tolerances.Area)
                    throw new InvalidOperationException(face.FaceId + " net area is not gross area minus deduction area.");
            }
        }

        private static void NonNegativeFinite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(label + " must be a finite non-negative number.");
        }
    }
}
