using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundarySnapCellRangeSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LargeFiniteCellIndexDoesNotOverflow();
            AdjacentCellsStillSnapByDistance();
            InfiniteQuotientFallsBackToExactCoordinateToken();
        }

        private static void LargeFiniteCellIndexDoesNotOverflow()
        {
            var snapper = CreateSnapper(1e-9d, out var type, out var getOrAdd);
            var first = Invoke(type, getOrAdd, snapper, new Point2(1e10d, -1e10d));
            var same = Invoke(type, getOrAdd, snapper, new Point2(1e10d, -1e10d));
            var distinct = Invoke(type, getOrAdd, snapper, new Point2(1e10d + 1d, -1e10d));

            if (first != 0 || same != first)
                throw new Exception("Expected repeated large finite Room-boundary points to snap to the same vertex without Int64 cell overflow.");
            if (distinct == first)
                throw new Exception("Expected a point one metre away to remain distinct at nanometre snapping tolerance.");
        }

        private static void AdjacentCellsStillSnapByDistance()
        {
            var snapper = CreateSnapper(1e-9d, out var type, out var getOrAdd);
            var left = Invoke(type, getOrAdd, snapper, new Point2(0.9e-9d, 0d));
            var right = Invoke(type, getOrAdd, snapper, new Point2(1.1e-9d, 0d));
            if (left != right)
                throw new Exception("Expected points within tolerance across adjacent spatial cells to snap together.");
        }

        private static void InfiniteQuotientFallsBackToExactCoordinateToken()
        {
            var snapper = CreateSnapper(1e-9d, out var type, out var getOrAdd);
            var positive = Invoke(type, getOrAdd, snapper, new Point2(1e308d, 0d));
            var positiveAgain = Invoke(type, getOrAdd, snapper, new Point2(1e308d, 0d));
            var negative = Invoke(type, getOrAdd, snapper, new Point2(-1e308d, 0d));

            if (positiveAgain != positive)
                throw new Exception("Expected identical finite coordinates to snap when coordinate/tolerance quotient overflows.");
            if (negative == positive)
                throw new Exception("Expected opposite extreme finite coordinates to remain distinct under exact-coordinate fallback.");
        }

        private static object CreateSnapper(double tolerance, out Type type, out MethodInfo getOrAdd)
        {
            type = typeof(RoomBoundaryEngine).GetNestedType("PointSnapper", BindingFlags.NonPublic)
                ?? throw new Exception("Expected RoomBoundaryEngine.PointSnapper regression target.");
            var constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(double) },
                modifiers: null)
                ?? throw new Exception("Expected RoomBoundaryEngine.PointSnapper constructor.");
            getOrAdd = type.GetMethod("GetOrAdd", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new Exception("Expected RoomBoundaryEngine.PointSnapper.GetOrAdd regression target.");
            return constructor.Invoke(new object[] { tolerance });
        }

        private static int Invoke(Type type, MethodInfo method, object target, Point2 point)
        {
            try
            {
                var value = method.Invoke(target, new object[] { point });
                return value is int index ? index : throw new Exception("Room-boundary snapper returned a non-integer vertex index from " + type.FullName + ".");
            }
            catch (TargetInvocationException ex)
            {
                throw new Exception("Room-boundary snapper must accept the finite cell-range regression input.", ex.InnerException ?? ex);
            }
        }
    }
}
