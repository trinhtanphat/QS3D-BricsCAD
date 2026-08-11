using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class OpeningHostProjectionOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var end = new Point2(8e307, 8e307);
            var segment = new OpeningHostSegment("W-LONG", new Point2(0d, 0d), end, 1e307);
            var opening = new Point2(1.3e308, 1.3e308);

            var result = new OpeningHostMatcher().Match(
                opening,
                new[] { segment },
                maxGapM: 1e308,
                ambiguityToleranceM: 0d);

            if (result.Status != OpeningHostMatchStatus.Matched)
                throw new Exception("Expected the long diagonal host to match after endpoint projection clamping.");
            if (!string.Equals(result.HostElementId, "W-LONG", StringComparison.Ordinal))
                throw new Exception("Expected the long diagonal host identity to be preserved.");
            if (!result.ClosestPoint.Equals(end))
                throw new Exception("Expected the finite segment endpoint to be the closest point for an opening beyond the host.");
            if (!Finite(result.CenterlineDistanceM) || !(result.CenterlineDistanceM > 0d))
                throw new Exception("Expected a finite positive centerline distance after endpoint clamping.");
            if (!Finite(result.GapM) || !(result.GapM >= 0d) || result.GapM > 1e308)
                throw new Exception("Expected a finite accepted host gap after endpoint clamping.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
