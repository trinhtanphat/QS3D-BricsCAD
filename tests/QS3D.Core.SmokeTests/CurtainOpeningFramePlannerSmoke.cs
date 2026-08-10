using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainOpeningFramePlannerSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            VerticalMullionSplitsAroundWindow();
            HorizontalTransomSplitsAroundOpening();
            DoorFromFloorRemovesLowerMullion();
            ClearanceExpandsInterruptedRegion();
            FullCoverRemovesFrame();
            NonIntersectingOpeningLeavesFrameUntouched();
            OutputOrderIsDeterministic();
            InvalidInputsAreRejected();
        }

        private static void VerticalMullionSplitsAroundWindow()
        {
            var frame = Rect(1.00, 0.00, 0.05, 3.60);
            var opening = Opening(0.80, 0.80, 0.60, 1.40);
            var plan = CurtainWallOpeningFramePlanner.Plan(new[] { frame }, new[] { opening });
            Equal(2, plan.Pieces.Count);
            Near(0.80, plan.Pieces[0].HeightM);
            Near(1.40, plan.Pieces[1].HeightM);
            Near(1.00, plan.Pieces[0].X_M);
            Equal(1, plan.InterruptedFrameCount);
            True(plan.RemovedFrameAreaM2 > 0d);
        }

        private static void HorizontalTransomSplitsAroundOpening()
        {
            var frame = Rect(0.00, 1.50, 6.00, 0.05);
            var opening = Opening(2.00, 0.80, 1.20, 1.40);
            var plan = CurtainWallOpeningFramePlanner.Plan(new[] { frame }, new[] { opening });
            Equal(2, plan.Pieces.Count);
            Near(2.00, plan.Pieces[0].WidthM);
            Near(2.80, plan.Pieces[1].WidthM);
            Near(3.20, plan.Pieces[1].X_M);
        }

        private static void DoorFromFloorRemovesLowerMullion()
        {
            var frame = Rect(2.50, 0.00, 0.05, 3.60);
            var door = Opening(2.00, 0.00, 1.00, 2.10);
            var plan = CurtainWallOpeningFramePlanner.Plan(new[] { frame }, new[] { door });
            Equal(1, plan.Pieces.Count);
            Near(2.10, plan.Pieces[0].Z_M);
            Near(1.50, plan.Pieces[0].HeightM);
        }

        private static void ClearanceExpandsInterruptedRegion()
        {
            var frame = Rect(1.00, 0.00, 0.05, 3.60);
            var opening = Opening(0.80, 1.00, 0.60, 1.00);
            var noClearance = CurtainWallOpeningFramePlanner.Plan(new[] { frame }, new[] { opening }, 0d);
            var withClearance = CurtainWallOpeningFramePlanner.Plan(new[] { frame }, new[] { opening }, 0.10d);
            True(withClearance.RemovedFrameAreaM2 > noClearance.RemovedFrameAreaM2);
            Near(0.90, withClearance.Pieces[0].HeightM);
            Near(1.50, withClearance.Pieces[1].HeightM);
        }

        private static void FullCoverRemovesFrame()
        {
            var frame = Rect(1.00, 1.00, 0.05, 1.00);
            var opening = Opening(0.00, 0.00, 3.00, 3.00);
            var plan = CurtainWallOpeningFramePlanner.Plan(new[] { frame }, new[] { opening });
            Equal(0, plan.Pieces.Count);
            Equal(1, plan.InterruptedFrameCount);
            Near(plan.OriginalFrameAreaM2, plan.RemovedFrameAreaM2);
        }

        private static void NonIntersectingOpeningLeavesFrameUntouched()
        {
            var frame = Rect(0.00, 0.00, 0.05, 3.60);
            var opening = Opening(2.00, 0.50, 1.00, 1.50);
            var plan = CurtainWallOpeningFramePlanner.Plan(new[] { frame }, new[] { opening });
            Equal(1, plan.Pieces.Count);
            Equal(0, plan.InterruptedFrameCount);
            Near(0d, plan.RemovedFrameAreaM2);
        }

        private static void OutputOrderIsDeterministic()
        {
            var frames = new[] { Rect(0.00, 1.50, 6.00, 0.05), Rect(2.00, 0.00, 0.05, 3.60) };
            var openings = new[] { Opening(1.50, 0.80, 1.20, 1.40), Opening(3.80, 0.80, 0.90, 1.40) };
            var a = CurtainWallOpeningFramePlanner.Plan(frames, openings).Pieces;
            var b = CurtainWallOpeningFramePlanner.Plan(frames, openings).Pieces;
            Equal(a.Count, b.Count);
            for (var i = 0; i < a.Count; i++)
            {
                Equal(a[i].SourceFrameIndex, b[i].SourceFrameIndex);
                Near(a[i].X_M, b[i].X_M);
                Near(a[i].Z_M, b[i].Z_M);
                Near(a[i].WidthM, b[i].WidthM);
                Near(a[i].HeightM, b[i].HeightM);
            }
        }

        private static void InvalidInputsAreRejected()
        {
            Throws<ArgumentOutOfRangeException>(() => CurtainWallOpeningFramePlanner.Plan(new[] { Rect(0, 0, 1, 1) }, new[] { Opening(0, 0, 1, 1) }, -0.01));
            var bad = Opening(0, 0, 1, 1); bad.WidthM = 0d;
            Throws<ArgumentOutOfRangeException>(() => CurtainWallOpeningFramePlanner.Plan(new[] { Rect(0, 0, 1, 1) }, new[] { bad }));
        }

        private static CurtainWallRect Rect(double x, double z, double width, double height) => new CurtainWallRect { X_M = x, Z_M = z, WidthM = width, HeightM = height };
        private static CurtainWallOpeningRect Opening(double x, double z, double width, double height) => new CurtainWallOpeningRect { X_M = x, Z_M = z, WidthM = width, HeightM = height };

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9d) throw new InvalidOperationException("Curtain opening/frame smoke expected " + expected + " but got " + actual + ".");
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new InvalidOperationException("Curtain opening/frame smoke expected " + expected + " but got " + actual + ".");
        }

        private static void True(bool condition)
        {
            if (!condition) throw new InvalidOperationException("Curtain opening/frame smoke assertion failed.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
