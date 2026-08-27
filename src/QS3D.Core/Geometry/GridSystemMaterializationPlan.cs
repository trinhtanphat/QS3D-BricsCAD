using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public sealed class GridSystemMaterializationItem
    {
        internal GridSystemMaterializationItem(int ordinal, GridReferenceCurve curve)
        {
            Ordinal = ordinal;
            Curve = curve ?? throw new ArgumentNullException(nameof(curve));
        }

        public int Ordinal { get; }
        public string ElementId => Curve.ElementId;
        public GridReferenceCurve Curve { get; }
    }

    public static class GridSystemMaterializationPlan
    {
        private const int MaxCurves = 2000;
        private const double GeometryToleranceM = 1e-8d;
        private const double TwoPi = Math.PI * 2d;

        public static IReadOnlyList<GridSystemMaterializationItem> Create(IReadOnlyList<GridReferenceCurve> curves)
        {
            if (curves == null) throw new ArgumentNullException(nameof(curves));
            if (curves.Count == 0) throw new InvalidOperationException("Grid system materialization requires at least one planned curve.");
            if (curves.Count > MaxCurves)
                throw new InvalidOperationException("Grid system materialization exceeds the supported " + MaxCurves + " curve limit.");

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<GridSystemMaterializationItem>(curves.Count);
            for (var index = 0; index < curves.Count; index++)
            {
                var curve = curves[index] ?? throw new ArgumentException("Grid system materialization contains a null curve at index " + index + ".", nameof(curves));
                if (!ids.Add(curve.ElementId))
                    throw new InvalidOperationException("Grid system materialization contains duplicate semantic element id: " + curve.ElementId + ".");

                ValidateCurve(curve, index);
                result.Add(new GridSystemMaterializationItem(index, curve));
            }
            return result.AsReadOnly();
        }

        private static void ValidateCurve(GridReferenceCurve curve, int index)
        {
            switch (curve.Kind)
            {
                case GridReferenceCurveKind.Line:
                    EnsureFinite(curve.Start.X, "line start X", index);
                    EnsureFinite(curve.Start.Y, "line start Y", index);
                    EnsureFinite(curve.End.X, "line end X", index);
                    EnsureFinite(curve.End.Y, "line end Y", index);
                    var dx = curve.End.X - curve.Start.X;
                    var dy = curve.End.Y - curve.Start.Y;
                    EnsureFinite(dx, "line delta X", index);
                    EnsureFinite(dy, "line delta Y", index);
                    if (Hypot(dx, dy) <= GeometryToleranceM)
                        throw new InvalidOperationException("Grid system materialization contains a degenerate LINE at index " + index + ".");
                    return;

                case GridReferenceCurveKind.Arc:
                    EnsureFinite(curve.Center.X, "arc center X", index);
                    EnsureFinite(curve.Center.Y, "arc center Y", index);
                    EnsureFinite(curve.Radius, "arc radius", index);
                    EnsureFinite(curve.StartAngleRad, "arc start angle", index);
                    EnsureFinite(curve.SweepAngleRad, "arc sweep angle", index);
                    if (!(curve.Radius > GeometryToleranceM))
                        throw new InvalidOperationException("Grid system materialization ARC radius must be positive at index " + index + ".");
                    if (!(curve.SweepAngleRad > GeometryToleranceM) || curve.SweepAngleRad > TwoPi + GeometryToleranceM)
                        throw new InvalidOperationException("Grid system materialization ARC sweep must be in (0, 2π] at index " + index + ".");
                    return;

                default:
                    throw new InvalidOperationException("Grid system materialization contains an unsupported curve kind at index " + index + ".");
            }
        }

        private static double Hypot(double x, double y)
        {
            var ax = Math.Abs(x);
            var ay = Math.Abs(y);
            var scale = Math.Max(ax, ay);
            if (scale == 0d) return 0d;
            var ratio = Math.Min(ax, ay) / scale;
            var result = scale * Math.Sqrt(1d + ratio * ratio);
            return Finite(result) ? result : double.PositiveInfinity;
        }

        private static void EnsureFinite(double value, string label, int index)
        {
            if (!Finite(value))
                throw new InvalidOperationException("Grid system materialization " + label + " is not finite at index " + index + ".");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
