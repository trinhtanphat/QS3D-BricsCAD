#nullable enable
using System;
using System.Collections.Generic;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.LocalQualification
{
    // Independent test oracle. Samples are strictly inside each stage, avoiding
    // coincident boundary faces. This is finite cross-section coverage, NOT a
    // proof of the complete solid's BREP topology.
    internal static class SectionVerifier
    {
        internal const double Tolerance = 1e-7d;

        internal static void Verify(Solid3d solid, double l1, double w1, double l2,
            double w2, double h1, double h2, Point3d center)
        {
            Require(Finite(l1) && Finite(w1) && Finite(l2) && Finite(w2) &&
                Finite(h1) && Finite(h2) && l1 > 0 && w1 > 0 && l2 > 0 &&
                w2 > 0 && h1 > 0 && h2 >= 0 && l2 <= l1 && w2 <= w1 &&
                Finite(center.X) && Finite(center.Y) && Finite(center.Z), "section_input");
            foreach (var fraction in new[] { 0.25d, 0.5d, 0.75d })
                VerifyAt(solid, l1, w1, center, center.Z + h1 * fraction);
            if (h2 > 0)
                foreach (var fraction in new[] { 0.25d, 0.5d, 0.75d })
                    VerifyAt(solid, l1 * (1d - fraction) + l2 * fraction,
                        w1 * (1d - fraction) + w2 * fraction, center,
                        center.Z + h1 + h2 * fraction);
        }

        private static void VerifyAt(Solid3d solid, double length, double width,
            Point3d center, double z)
        {
            Require(Finite(z) && Finite(length * width), "section_expected_nonfinite");
            using (var plane = new Plane(new Point3d(center.X, center.Y, z), Vector3d.ZAxis))
            using (var section = solid.GetSection(plane))
            {
                Require(section != null && !section.IsNull, "section_missing");
                var region = section!;
                var xmin = center.X - length / 2d; var xmax = center.X + length / 2d;
                var ymin = center.Y - width / 2d; var ymax = center.Y + width / 2d;
                Require(Near(region.Area, length * width), "section_area");
                Require(Near(region.Perimeter, 2d * (length + width)), "section_perimeter");
                var bounds = region.GeometricExtents;
                Require(Same(bounds.MinPoint, new Point3d(xmin, ymin, z)) &&
                    Same(bounds.MaxPoint, new Point3d(xmax, ymax, z)), "section_bounds");

                // Inspect the actual boundary, not just scalar area/extents. A
                // rectangular section must contain each of its four edges once.
                var corners = new[] { new Point3d(xmin, ymin, z), new Point3d(xmax, ymin, z),
                    new Point3d(xmax, ymax, z), new Point3d(xmin, ymax, z) };
                using (var edges = new DBObjectCollection())
                {
                    try
                    {
                        region.Explode(edges);
                        Require(edges.Count == 4, "section_edge_count");
                        var used = new HashSet<int>();
                        foreach (DBObject entity in edges)
                        {
                            var line = entity as Line;
                            Require(line != null, "section_edge_not_line");
                            var found = -1;
                            for (var i = 0; i < 4; i++)
                            {
                                var next = (i + 1) % 4;
                                if ((Same(line!.StartPoint, corners[i]) && Same(line.EndPoint, corners[next])) ||
                                    (Same(line.StartPoint, corners[next]) && Same(line.EndPoint, corners[i])))
                                    found = i;
                            }
                            Require(found >= 0 && used.Add(found), "section_edge_boundary");
                        }
                    }
                    finally { foreach (DBObject entity in edges) entity.Dispose(); }
                }
            }
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool Near(double actual, double expected) => Finite(actual) && Finite(expected) &&
            Math.Abs(actual - expected) <= Tolerance * Math.Max(1d, Math.Abs(expected));
        private static bool Same(Point3d a, Point3d b) => Near(a.X, b.X) && Near(a.Y, b.Y) && Near(a.Z, b.Z);
        private static void Require(bool condition, string code)
        { if (!condition) throw new InvalidOperationException(code); }
    }
}
