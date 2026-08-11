using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class BulgeArcMidpointOverflowSmoke
    {
        internal static void Run()
        {
            var start = new Point2(double.MaxValue * 0.75d, 0d);
            var end = new Point2(double.MaxValue * 0.90d, 0d);
            if (!double.IsInfinity(start.X + end.X))
                throw new InvalidOperationException("Regression setup did not overflow the naive midpoint sum.");
            if (double.IsInfinity(start.DistanceTo(end)))
                throw new InvalidOperationException("Regression setup requires a finite chord.");

            var points = BulgeArcTessellator.Tessellate(start, end, 1d, 1e307d);
            if (points.Count < 3)
                throw new InvalidOperationException("Expected tessellated arc points.");
            if (!points[0].Equals(start) || !points[points.Count - 1].Equals(end))
                throw new InvalidOperationException("Tessellation did not preserve exact endpoints.");
            foreach (var point in points)
            {
                if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
                    throw new InvalidOperationException("Tessellation emitted a non-finite point.");
            }

            var ordinary = BulgeArcTessellator.Tessellate(new Point2(0d, 0d), new Point2(2d, 0d), 1d, 0.1d);
            if (ordinary.Count < 3 || !ordinary[0].Equals(new Point2(0d, 0d)) || !ordinary[ordinary.Count - 1].Equals(new Point2(2d, 0d)))
                throw new InvalidOperationException("Ordinary bulge tessellation behavior changed unexpectedly.");

            Throws<ArgumentOutOfRangeException>(() =>
                BulgeArcTessellator.Tessellate(new Point2(double.PositiveInfinity, 0d), new Point2(1d, 0d), 1d));
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
