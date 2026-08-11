using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonTranslatedAreaSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            const double origin = 1e155d;
            const double span = 1e140d;
            var right = origin + span;
            var top = origin + span;
            var scan = origin + (right - origin) * 0.5d;
            if (!double.IsInfinity(origin * origin)) throw new Exception("Fixture must overflow with absolute-coordinate area products.");

            var polygon = new[]
            {
                new Point2(origin, origin),
                new Point2(right, origin),
                new Point2(right, top),
                new Point2(origin, top)
            };

            var normalized = PolygonScanlineClipper.NormalizeAndValidate(polygon);
            if (normalized.Count != 4) throw new Exception("Expected translated rectangle to remain valid.");

            var segments = PolygonScanlineClipper.Clip(polygon, PolygonScanAxis.Horizontal, scan);
            if (segments.Count != 1) throw new Exception("Expected one translated scanline segment.");
            var segment = segments[0];
            if (!segment.Start.Equals(new Point2(origin, scan)) || !segment.End.Equals(new Point2(right, scan)))
                throw new Exception("Translated scanline endpoints changed.");
            if (double.IsNaN(segment.Length) || double.IsInfinity(segment.Length) || !(segment.Length > 0d))
                throw new Exception("Expected finite positive translated scanline length.");
        }
    }
}
