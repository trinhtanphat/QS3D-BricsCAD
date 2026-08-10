using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public sealed class BulgedPolygonVertex2
    {
        public BulgedPolygonVertex2(Point2 point, double bulgeToNext = 0d)
        {
            Point = point;
            BulgeToNext = bulgeToNext;
        }

        public Point2 Point { get; }
        public double BulgeToNext { get; }
    }

    public static class BulgedPolygonFootprintTessellator
    {
        private const int MaxVertices = 4096;

        public static IReadOnlyList<Point2> TessellateClosed(
            IReadOnlyList<BulgedPolygonVertex2> vertices,
            double maximumSagitta = 0.002d)
        {
            if (vertices == null) throw new ArgumentNullException(nameof(vertices));
            if (vertices.Count < 3) throw new ArgumentException("Closed bulged polygon requires at least three vertices.", nameof(vertices));
            if (vertices.Count > MaxVertices) throw new ArgumentException("Closed bulged polygon exceeds the supported " + MaxVertices + " source vertex limit.", nameof(vertices));
            if (!Finite(maximumSagitta) || maximumSagitta <= 0d) throw new ArgumentOutOfRangeException(nameof(maximumSagitta));

            var result = new List<Point2>(Math.Min(vertices.Count * 2, MaxVertices));
            for (var index = 0; index < vertices.Count; index++)
            {
                var current = vertices[index] ?? throw new ArgumentException("Closed bulged polygon contains a null vertex at index " + index + ".", nameof(vertices));
                var next = vertices[(index + 1) % vertices.Count] ?? throw new ArgumentException("Closed bulged polygon contains a null vertex at index " + ((index + 1) % vertices.Count) + ".", nameof(vertices));
                if (!Finite(current.BulgeToNext)) throw new ArgumentOutOfRangeException(nameof(vertices), "Polyline bulge must be finite at vertex " + index + ".");

                var segment = BulgeArcTessellator.Tessellate(current.Point, next.Point, current.BulgeToNext, maximumSagitta);
                for (var pointIndex = 0; pointIndex < segment.Count - 1; pointIndex++)
                {
                    result.Add(segment[pointIndex]);
                    if (result.Count > MaxVertices)
                        throw new InvalidOperationException("Bulged polygon tessellation exceeds the supported " + MaxVertices + " vertex limit.");
                }
            }

            if (result.Count < 3) throw new InvalidOperationException("Bulged polygon tessellation produced fewer than three vertices.");
            return PolygonScanlineClipper.NormalizeAndValidate(result);
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
