using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallFootprintResultSnapshotSmoke
    {
        internal static void Run()
        {
            ConstructorSnapshotsPolygon();
        }

        private static void ConstructorSnapshotsPolygon()
        {
            var originalFirst = new Point2(0d, 0d);
            var polygon = new[]
            {
                originalFirst,
                new Point2(2d, 0d),
                new Point2(2d, 1d),
                new Point2(0d, 1d)
            };
            var result = new WallFootprintResult(polygon, 2d, 2d, 6d, false);

            polygon[0] = new Point2(99d, 99d);
            Equal(originalFirst, result.Polygon[0], "source mutation isolation");
            Equal(4, result.Polygon.Count, "polygon count");
            AssertMutationRejected(result.Polygon);
        }

        private static void AssertMutationRejected(IReadOnlyList<Point2> polygon)
        {
            if (polygon is Point2[])
                throw new Exception("WallFootprintResultSnapshotSmoke: result must not expose the caller array.");
            if (!(polygon is IList<Point2> list))
                throw new Exception("WallFootprintResultSnapshotSmoke: expected IList compatibility for mutation guard verification.");

            try
            {
                list[0] = new Point2(77d, 77d);
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new Exception("WallFootprintResultSnapshotSmoke: result index mutation was accepted.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception("WallFootprintResultSnapshotSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class WallFootprintResultSnapshotSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => WallFootprintResultSnapshotSmoke.Run();
    }
}
