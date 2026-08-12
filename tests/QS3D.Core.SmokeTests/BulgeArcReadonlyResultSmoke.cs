using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class BulgeArcReadonlyResultSmoke
    {
        internal static void Run()
        {
            StraightResultIsReadOnly();
            CurvedResultRemainsReadOnly();
        }

        private static void StraightResultIsReadOnly()
        {
            var start = new Point2(0d, 0d);
            var end = new Point2(10d, 0d);
            var result = BulgeArcTessellator.Tessellate(start, end, 0d, 0.1d);

            Equal(2, result.Count, "straight count");
            Equal(start, result[0], "straight start");
            Equal(end, result[1], "straight end");
            AssertMutationRejected(result, "straight");
        }

        private static void CurvedResultRemainsReadOnly()
        {
            var result = BulgeArcTessellator.Tessellate(
                new Point2(0d, 0d),
                new Point2(10d, 0d),
                0.5d,
                0.1d);

            if (result.Count < 2)
                throw new Exception("BulgeArcReadonlyResultSmoke curved count: expected at least two points.");
            AssertMutationRejected(result, "curved");
        }

        private static void AssertMutationRejected(IReadOnlyList<Point2> result, string label)
        {
            if (result is Point2[])
                throw new Exception("BulgeArcReadonlyResultSmoke " + label + ": result must not expose a mutable array.");
            if (!(result is IList<Point2> list))
                throw new Exception("BulgeArcReadonlyResultSmoke " + label + ": expected IList compatibility for mutation guard verification.");

            try
            {
                list[0] = new Point2(99d, 99d);
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new Exception("BulgeArcReadonlyResultSmoke " + label + ": index mutation was accepted.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception("BulgeArcReadonlyResultSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class BulgeArcReadonlyResultSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => BulgeArcReadonlyResultSmoke.Run();
    }
}
