using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionIdentityUnicodeSmoke
    {
        public static void Run()
        {
            MalformedIdentityTextIsRejected();
            AssignRejectsMalformedIdentityText();
            ValidSupplementaryUnicodeRemainsDeterministic();
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

            var identities = GridIntersectionIdentityPlanner.Assign(new[]
            {
                new GridIntersection(first, second, new Point2(1d, 2d))
            });
            if (identities.Count != 1 || !string.Equals(identities[0].PairToken, direct, StringComparison.Ordinal))
                throw new InvalidOperationException("Valid supplementary Grid identity text did not retain deterministic assignment.");
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
