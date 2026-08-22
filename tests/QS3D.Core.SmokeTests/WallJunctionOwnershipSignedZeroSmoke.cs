using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionOwnershipSignedZeroSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SignedZeroDoesNotChangeOwnershipFingerprint();
            NonZeroGeometryStillChangesFingerprint();
        }

        private static void SignedZeroDoesNotChangeOwnershipFingerprint()
        {
            var negativeZero = Plan(-0d, -0d);
            var positiveZero = Plan(0d, 0d);

            Equal(positiveZero.GroupToken, negativeZero.GroupToken);
            Equal(positiveZero.OwnerToken, negativeZero.OwnerToken);
            Equal(positiveZero.InputFingerprint, negativeZero.InputFingerprint);
        }

        private static void NonZeroGeometryStillChangesFingerprint()
        {
            var baseline = Plan(0d, 0d);
            var moved = Plan(0.01d, 0d);

            Equal(baseline.GroupToken, moved.GroupToken);
            Equal(baseline.OwnerToken, moved.OwnerToken);
            NotEqual(baseline.InputFingerprint, moved.InputFingerprint);
        }

        private static WallJunctionOwnershipPlan Plan(double x, double bottomM)
        {
            var junction = new WallJunction(
                new Point2(x, 1d),
                WallJunctionKind.T,
                new[] { "S1", "S2", "S3" },
                3);
            var owners = new[]
            {
                new WallJunctionOwnerContext("S1", "W1", "P-SIGNED-ZERO", "D-SIGNED-ZERO", bottomM, 3d, 0.2d),
                new WallJunctionOwnerContext("S2", "W2", "P-SIGNED-ZERO", "D-SIGNED-ZERO", bottomM, 3d, 0.2d),
                new WallJunctionOwnerContext("S3", "W3", "P-SIGNED-ZERO", "D-SIGNED-ZERO", bottomM, 3d, 0.2d)
            };

            var plans = WallJunctionOwnershipPlanner.Plan(new[] { junction }, owners);
            Equal(1, plans.Count);
            return plans[0];
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }

        private static void NotEqual<T>(T left, T right)
        {
            if (EqualityComparer<T>.Default.Equals(left, right))
                throw new InvalidOperationException("Expected values to differ but both were " + left + ".");
        }
    }
}
