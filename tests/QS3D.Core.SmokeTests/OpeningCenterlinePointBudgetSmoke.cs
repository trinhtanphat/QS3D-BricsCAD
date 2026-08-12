using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class OpeningCenterlinePointBudgetSmoke
    {
        private const int OversizedCount = 8193;

        [ModuleInitializer]
        internal static void Initialize()
        {
            var validCenterline = new[] { new Point2(0d, 0d), new Point2(10d, 0d) };

            var polyline = PolylineOpeningCutPlanner.Plan(new PolylineOpeningCutInput
            {
                Centerline = validCenterline,
                OpeningCenter = new Point2(5d, 0d),
                HostThicknessM = 0.2d,
                HostHeightM = 3d,
                OpeningWidthM = 1d,
                OpeningHeightM = 2d,
                SillHeightM = 0.5d,
                ClearanceM = 0.01d,
                MaximumCenterlineOffsetM = 0.5d
            });
            if (polyline.SegmentIndex != 0 || Math.Abs(polyline.StationM - 5d) > 1e-9d)
                throw new Exception("Supported polyline opening centerline must remain plannable.");

            var curved = CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput
            {
                Centerline = validCenterline,
                OpeningPoint = new Point2(5d, 0d),
                OpeningWidthM = 1d,
                HostThicknessM = 0.2d,
                ClearanceM = 0.01d,
                MaximumCenterlineOffsetM = 0.5d,
                AmbiguityMarginM = 0.01d,
                MiterLimit = 4d,
                ToleranceM = 1e-9d
            });
            if (curved.ProjectionSegmentIndex != 0 || Math.Abs(curved.CenterStationM - 5d) > 1e-9d)
                throw new Exception("Supported curved opening centerline must remain plannable.");

            var oversizedPolyline = new OversizedPointList(OversizedCount);
            RejectsBeforeRead(
                () => PolylineOpeningCutPlanner.Plan(new PolylineOpeningCutInput { Centerline = oversizedPolyline }),
                oversizedPolyline,
                "polyline opening centerline");

            var oversizedCurved = new OversizedPointList(OversizedCount);
            RejectsBeforeRead(
                () => CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput { Centerline = oversizedCurved }),
                oversizedCurved,
                "curved opening centerline");
        }

        private static void RejectsBeforeRead(Action action, OversizedPointList points, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                if (points.ReadAttempted)
                    throw new Exception("Oversized " + label + " was read before the point-budget guard rejected it.");
                return;
            }

            throw new Exception("Oversized " + label + " was accepted.");
        }

        private sealed class OversizedPointList : IReadOnlyList<Point2>
        {
            public OversizedPointList(int count) => Count = count;

            public int Count { get; }
            public bool ReadAttempted { get; private set; }

            public Point2 this[int index]
            {
                get
                {
                    ReadAttempted = true;
                    throw new InvalidOperationException("Oversized point-list indexer must not be read.");
                }
            }

            public IEnumerator<Point2> GetEnumerator()
            {
                ReadAttempted = true;
                throw new InvalidOperationException("Oversized point-list enumerator must not be read.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
