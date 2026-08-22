using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionOwnershipUnicodeSmoke
    {
        public static void Run()
        {
            MalformedOwnershipIdentitiesAreRejected();
            ValidSupplementaryUnicodeRemainsDeterministic();
        }

        private static void MalformedOwnershipIdentitiesAreRejected()
        {
            Throws<InvalidOperationException>(() => Plan("S-\uD800", "S-B", "W-A", "W-B", "PROJECT", "DRAWING"));
            Throws<InvalidOperationException>(() => Plan("S-\uD801", "S-B", "W-A", "W-B", "PROJECT", "DRAWING"));
            Throws<InvalidOperationException>(() => Plan("S-A", "S-B", "W-\uDC00", "W-B", "PROJECT", "DRAWING"));
            Throws<InvalidOperationException>(() => Plan("S-A", "S-B", "W-A", "W-B", "PROJECT-\uD800", "DRAWING"));
            Throws<InvalidOperationException>(() => Plan("S-A", "S-B", "W-A", "W-B", "PROJECT", "DRAWING-\uDC00"));
        }

        private static void ValidSupplementaryUnicodeRemainsDeterministic()
        {
            const string scalar = "\uD83E\uDDF1";
            var first = Plan("seg-a-" + scalar, "seg-b-" + scalar, "wall-a-" + scalar, "wall-b-" + scalar, "project-" + scalar, "drawing-" + scalar);
            var recased = Plan("SEG-A-" + scalar, "SEG-B-" + scalar, "WALL-A-" + scalar, "WALL-B-" + scalar, "PROJECT-" + scalar, "DRAWING-" + scalar);

            if (first.Count != 1 || recased.Count != 1)
                throw new InvalidOperationException("Valid supplementary Unicode ownership inputs did not produce one deterministic plan.");
            if (!string.Equals(first[0].GroupToken, recased[0].GroupToken, StringComparison.Ordinal) ||
                !string.Equals(first[0].OwnerToken, recased[0].OwnerToken, StringComparison.Ordinal) ||
                !string.Equals(first[0].InputFingerprint, recased[0].InputFingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("Valid supplementary Unicode changed case-insensitive wall-junction ownership identity.");
            if (!first[0].GroupToken.StartsWith("WJP1:", StringComparison.Ordinal) ||
                !first[0].OwnerToken.StartsWith("WJX1:", StringComparison.Ordinal) ||
                !first[0].InputFingerprint.StartsWith("WJF1:", StringComparison.Ordinal))
                throw new InvalidOperationException("Wall-junction ownership token prefixes changed unexpectedly.");
        }

        private static IReadOnlyList<WallJunctionOwnershipPlan> Plan(
            string segmentA,
            string segmentB,
            string wallA,
            string wallB,
            string projectId,
            string drawingFingerprint)
        {
            var junction = new WallJunction(
                new Point2(0d, 0d),
                WallJunctionKind.T,
                new[] { segmentA, segmentB },
                3);
            var mappings = new[]
            {
                new WallJunctionOwnerContext(segmentA, wallA, projectId, drawingFingerprint, 0d, 3d, 0.2d),
                new WallJunctionOwnerContext(segmentB, wallB, projectId, drawingFingerprint, 0d, 3d, 0.25d)
            };
            return WallJunctionOwnershipPlanner.Plan(new[] { junction }, mappings);
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

    internal static class WallJunctionOwnershipUnicodeSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            WallJunctionOwnershipUnicodeSmoke.Run();
        }
    }
}
