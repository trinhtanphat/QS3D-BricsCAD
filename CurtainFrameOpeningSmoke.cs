using System;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainFrameOpeningSmoke
    {
        public static void Run()
        {
            DoorInterruptsVerticalAndHorizontalFrames();
            NonIntersectingOpeningLeavesFrameIntact();
            ClearanceExpandsInterruptedRegion();
            MultipleOpeningsRemainDeterministic();
            OpeningDerivedBoundsMustRemainFinite();
            FrameDerivedBoundsMustRemainFinite();
        }

        private static void DoorInterruptsVerticalAndHorizontalFrames()
        {
            var frames = new[]
            {
                new CurtainWallRect(2.95d, 0d, 0.1d, 3d),
                new CurtainWallRect(0d, 0.95d, 6d, 0.1d)
            };
            var output = CurtainFrameOpeningPlanner.Interrupt(frames, new[]
            {
                new CurtainOpeningRect(2.5d, 0d, 1d, 2.1d)
            });
            if (output.Count != 3) throw new Exception("Expected vertical top fragment + two horizontal side fragments.");
            var vertical = output.Single(x => Math.Abs(x.WidthM - 0.1d) < 1e-12d);
            Near(2.1d, vertical.Z_M);
            Near(0.9d, vertical.HeightM);
            var horizontal = output.Where(x => Math.Abs(x.HeightM - 0.1d) < 1e-12d).OrderBy(x => x.X_M).ToArray();
            Near(2.5d, horizontal[0].WidthM);
            Near(3.5d, horizontal[1].X_M);
            Near(2.5d, horizontal[1].WidthM);
        }

        private static void NonIntersectingOpeningLeavesFrameIntact()
        {
            var frame = new CurtainWallRect(0d, 0d, 0.05d, 3d);
            var output = CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, new[]
            {
                new CurtainOpeningRect(2d, 0d, 1d, 2d)
            });
            if (output.Count != 1) throw new Exception("Non-intersecting opening must preserve frame count.");
            Near(frame.WidthM, output[0].WidthM);
            Near(frame.HeightM, output[0].HeightM);
        }

        private static void ClearanceExpandsInterruptedRegion()
        {
            var output = CurtainFrameOpeningPlanner.Interrupt(new[]
            {
                new CurtainWallRect(0d, 1d, 6d, 0.05d)
            }, new[]
            {
                new CurtainOpeningRect(2.5d, 0d, 1d, 2d, 0.1d)
            });
            if (output.Count != 2) throw new Exception("Expected two horizontal fragments around cleared opening.");
            var ordered = output.OrderBy(x => x.X_M).ToArray();
            Near(2.4d, ordered[0].WidthM);
            Near(3.6d, ordered[1].X_M);
            Near(2.4d, ordered[1].WidthM);
        }

        private static void MultipleOpeningsRemainDeterministic()
        {
            var frames = new[] { new CurtainWallRect(0d, 1d, 10d, 0.05d) };
            var openings = new[]
            {
                new CurtainOpeningRect(2d, 0d, 1d, 2d),
                new CurtainOpeningRect(7d, 0d, 1d, 2d)
            };
            var first = CurtainFrameOpeningPlanner.Interrupt(frames, openings).ToArray();
            var second = CurtainFrameOpeningPlanner.Interrupt(frames, openings).ToArray();
            if (first.Length != 3 || second.Length != first.Length) throw new Exception("Expected three deterministic horizontal fragments.");
            for (var i = 0; i < first.Length; i++)
            {
                Near(first[i].X_M, second[i].X_M);
                Near(first[i].WidthM, second[i].WidthM);
            }
        }

        private static void OpeningDerivedBoundsMustRemainFinite()
        {
            Throws<OverflowException>(
                () => new CurtainOpeningRect(double.MaxValue, 0d, 1d, 1d),
                "Opening right bound overflow must fail closed.");
            Throws<OverflowException>(
                () => new CurtainOpeningRect(-double.MaxValue, 0d, 1d, 1d, double.MaxValue),
                "Clearance-expanded opening left bound overflow must fail closed.");
            Throws<OverflowException>(
                () => new CurtainOpeningRect(0d, double.MaxValue, 1d, 1d),
                "Opening top bound overflow must fail closed.");
        }

        private static void FrameDerivedBoundsMustRemainFinite()
        {
            Throws<InvalidOperationException>(
                () => CurtainFrameOpeningPlanner.Interrupt(
                    new[] { new CurtainWallRect(double.MaxValue, 0d, 1d, 1d) },
                    Array.Empty<CurtainOpeningRect>()),
                "Frame right bound overflow must fail closed.");
            Throws<InvalidOperationException>(
                () => CurtainFrameOpeningPlanner.Interrupt(
                    new[] { new CurtainWallRect(0d, double.MaxValue, 1d, 1d) },
                    Array.Empty<CurtainOpeningRect>()),
                "Frame top bound overflow must fail closed.");
        }

        private static void Throws<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new Exception(message);
        }

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
