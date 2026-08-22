using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionIdentityUnicodeSmoke
    {
        public static void Run()
        {
            ReferenceCurveFactoriesRejectMalformedIdentityText();
            MalformedIdentityTextIsRejected();
            AssignRejectsMalformedIdentityText();
            ValidSupplementaryUnicodeRemainsDeterministic();
        }

        private static void ReferenceCurveFactoriesRejectMalformedIdentityText()
        {
            var start = new Point2(0d, 0d);
            var end = new Point2(1d, 0d);
            Throws<ArgumentException>(() => GridReferenceCurve.Line("GRID-\uD800", start, end));
            Throws<ArgumentException>(() => GridReferenceCurve.Line("GRID-\uDC00", start, end));
            Throws<ArgumentException>(() => GridReferenceCurve.Arc("GRID-\uD801", start, 1d, 0d, Math.PI));
            Throws<ArgumentException>(() => GridReferenceCurve.Arc("GRID-\uDC01", start, 1d, 0d, Math.PI));
        }

        private static void MalformedIdentityTextIsRejected()
        {
            Throws<ArgumentException>(() => GridIntersectionIdentityPlanner.BuildPairToken("GRID-\uD800", "B"));
            Throws<ArgumentException>(() => GridIntersectionIdentityPlanner.BuildPairToken("GRID-\uD801", "B"));
            Throws<ArgumentException>(() => GridIntersectionIdentityPlanner.BuildPairToken("A", "GRID-\uDC00"));
        }

        private static void AssignRejectsMalformedIdentityText()
        {
            var intersections = new[]
            {
                new GridIntersection("GRID-\uD800", "B", new Point2(0d, 0d))
            };
            Throws<ArgumentException>(() => GridIntersectionIdentityPlanner.Assign(intersections));
        }

        private static void ValidSupplementaryUnicodeRemainsDeterministic()
        {
            const string scalar = "\uD83E\uDDF1";
            var first = "grid-" + scalar;
            var second = "B";
            var direct = GridIntersectionIdentityPlanner.BuildPairToken(first, second);
            var reversedAndRecased = GridIntersectionIdentityPlanner.BuildPairToken(second.ToLowerInvariant(), first.ToUpperInvariant());

            if (!string.Equals(direct, reversedAndRecased, StringComparison.Ordinal))
                throw new InvalidOperationException("Valid supplementary Grid identity text changed pair-token order/case determinism.");
            if (!direct.StartsWith("GIP1:", StringComparison.Ordinal) || direct.Length != "GIP1:".Length + 64)
                throw new InvalidOperationException("Valid Grid identity pair-token format changed unexpectedly.");

            var firstCurve = GridReferenceCurve.Line(
                " " + first + " ",
                new Point2(-1d, 0d),
                new Point2(1d, 0d));
            var secondCurve = GridReferenceCurve.Line(
                second,
                new Point2(0d, -1d),
                new Point2(0d, 1d));
            if (!string.Equals(firstCurve.ElementId, first, StringComparison.Ordinal))
                throw new InvalidOperationException("Valid supplementary Grid identity text lost its existing trim-only normalization.");

            var intersections = GridIntersectionPlanner.FindIntersections(new[] { firstCurve, secondCurve });
            if (intersections.Count != 1)
                throw new InvalidOperationException("Valid supplementary Grid identity curves must produce one deterministic intersection.");
            var identities = GridIntersectionIdentityPlanner.Assign(intersections);
            if (identities.Count != 1 || !string.Equals(identities[0].PairToken, direct, StringComparison.Ordinal))
                throw new InvalidOperationException("Valid supplementary Grid identity text did not flow through intersection and deterministic assignment.");
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class GridIntersectionIdentityUnicodeSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GridIntersectionIdentityUnicodeSmoke.Run();
        }
    }
}
