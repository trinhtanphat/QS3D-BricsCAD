using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundaryIntersectionArithmeticSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CrossRemainsFiniteAfterCancellation();
            EndpointProjectionAvoidsLengthSquaredOverflow();
        }

        private static void CrossRemainsFiniteAfterCancellation()
        {
            var method = typeof(RoomBoundaryEngine).GetMethod("Cross", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new Exception("Expected RoomBoundaryEngine.Cross regression target.");

            const double scale = 1e160;
            object? value;
            try
            {
                value = method.Invoke(null, new object[] { scale, scale, scale, 1.000000000000001e160 });
            }
            catch (TargetInvocationException ex)
            {
                throw new Exception("Finite room-boundary determinant must not fail through component-product overflow.", ex.InnerException ?? ex);
            }

            if (!(value is double determinant) || !Finite(determinant) || !(determinant > 0d))
                throw new Exception("Expected a finite positive room-boundary determinant after scale-safe cancellation.");
        }

        private static void EndpointProjectionAvoidsLengthSquaredOverflow()
        {
            var cutType = typeof(RoomBoundaryEngine).GetNestedType("Cut", BindingFlags.NonPublic);
            if (cutType == null) throw new Exception("Expected RoomBoundaryEngine.Cut regression target.");
            var listType = typeof(List<>).MakeGenericType(cutType);
            var cuts = Activator.CreateInstance(listType) ?? throw new Exception("Could not create RoomBoundaryEngine.Cut list.");
            var method = typeof(RoomBoundaryEngine).GetMethod("AddEndpointCut", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new Exception("Expected RoomBoundaryEngine.AddEndpointCut regression target.");

            var point = new Point2(5e159, 5e159);
            var segment = new BoundarySegment(new Point2(0d, 0d), new Point2(1e160, 1e160));
            try
            {
                method.Invoke(null, new object[] { point, segment, cuts, 1d });
            }
            catch (TargetInvocationException ex)
            {
                throw new Exception("Finite collinear endpoint projection must not fail through length-squared overflow.", ex.InnerException ?? ex);
            }

            var count = (int)(listType.GetProperty("Count")?.GetValue(cuts) ?? -1);
            if (count != 1) throw new Exception("Expected exactly one projected room-boundary endpoint cut.");
            var item = listType.GetProperty("Item")?.GetValue(cuts, new object[] { 0 });
            if (item == null) throw new Exception("Expected projected room-boundary endpoint cut value.");
            var t = (double)(cutType.GetProperty("T")?.GetValue(item) ?? double.NaN);
            if (!Finite(t) || Math.Abs(t - 0.5d) > 1e-12d)
                throw new Exception("Expected the large finite collinear endpoint projection parameter to remain 0.5.");
            var projected = (Point2)(cutType.GetProperty("Point")?.GetValue(item) ?? default(Point2));
            if (projected.DistanceTo(point) > 1e145)
                throw new Exception("Expected the large finite endpoint projection to preserve the midpoint.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
