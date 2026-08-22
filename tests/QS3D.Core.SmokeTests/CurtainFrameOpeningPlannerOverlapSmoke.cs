using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainFrameOpeningPlannerOverlapSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OverlappingOpeningOrderPreservesAreaAndCoverage();
        }

        private static void OverlappingOpeningOrderPreservesAreaAndCoverage()
        {
            var frame = new CurtainWallRect(0d, 0d, 12d, 10d);
            var first = new CurtainOpeningRect(6d, 1d, 4d, 2d);
            var second = new CurtainOpeningRect(4d, 1d, 4d, 3d);

            var forward = CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, new[] { first, second });
            var reverse = CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, new[] { second, first });

            AssertArea(forward, 104d, "forward overlapping openings");
            AssertArea(reverse, 104d, "reverse overlapping openings");

            foreach (var sample in new[]
            {
                new Sample(1d, 1d, true),
                new Sample(5d, 2d, false),
                new Sample(7d, 2d, false),
                new Sample(9d, 2d, false),
                new Sample(5d, 3.5d, false),
                new Sample(9d, 3.5d, true),
                new Sample(11d, 9d, true)
            })
            {
                var forwardContains = Contains(forward, sample.X, sample.Z);
                var reverseContains = Contains(reverse, sample.X, sample.Z);
                if (forwardContains != sample.Expected || reverseContains != sample.Expected)
                    throw new InvalidOperationException("Overlapping opening order changed sampled retained coverage at " + sample.X + "," + sample.Z + ".");
            }
        }

        private static bool Contains(IReadOnlyList<CurtainWallRect> rects, double x, double z)
        {
            for (var i = 0; i < rects.Count; i++)
            {
                var rect = rects[i];
                if (x >= rect.X_M && x < rect.X_M + rect.WidthM &&
                    z >= rect.Z_M && z < rect.Z_M + rect.HeightM)
                    return true;
            }
            return false;
        }

        private static void AssertArea(IReadOnlyList<CurtainWallRect> rects, double expected, string label)
        {
            var total = 0d;
            for (var i = 0; i < rects.Count; i++) total += rects[i].AreaM2;
            if (total != expected)
                throw new InvalidOperationException(label + " area mismatch: " + total + " != " + expected + ".");
        }

        private readonly struct Sample
        {
            internal Sample(double x, double z, bool expected)
            {
                X = x;
                Z = z;
                Expected = expected;
            }

            internal double X { get; }
            internal double Z { get; }
            internal bool Expected { get; }
        }
    }
}
