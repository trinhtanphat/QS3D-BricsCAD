using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RectangularMeshEndClearancePrecisionSmoke
    {
        internal static void Run()
        {
            SlabRunLengthLostClearanceFailsClosed();
            WallRunLengthLostClearanceFailsClosed();
            RepresentableLargeDeductionsRemainAccepted();
            OrdinaryMeshesRemainStable();
        }

        private static void SlabRunLengthLostClearanceFailsClosed()
        {
            var error = Capture<OverflowException>(() => RectangularSlabMeshPlanner.Plan(new RectangularSlabMeshInput
            {
                SpanXM = 1e16d,
                SpanYM = 10d,
                ThicknessM = 10d,
                CoverM = 0d,
                XDiameterMm = 1000d,
                YDiameterMm = 2000d,
                XCount = 2,
                YCount = 2,
                IncludeBottom = true,
                IncludeTop = false
            }));

            Assert(
                error.Message == "Slab X bar length lost positive end clearance at the current numeric scale.",
                "Slab run-length precision-collapse diagnostic changed unexpectedly.");
        }

        private static void WallRunLengthLostClearanceFailsClosed()
        {
            var error = Capture<OverflowException>(() => RectangularWallMeshPlanner.Plan(new RectangularWallMeshInput
            {
                LengthM = 1e16d,
                HeightM = 10d,
                ThicknessM = 10d,
                CoverM = 0d,
                HorizontalDiameterMm = 1000d,
                VerticalDiameterMm = 2000d,
                HorizontalCount = 2,
                VerticalCount = 2,
                IncludeNear = true,
                IncludeFar = false
            }));

            Assert(
                error.Message == "Structural wall horizontal bar length lost positive end clearance at the current numeric scale.",
                "Wall run-length precision-collapse diagnostic changed unexpectedly.");
        }

        private static void RepresentableLargeDeductionsRemainAccepted()
        {
            var slab = RectangularSlabMeshPlanner.Plan(new RectangularSlabMeshInput
            {
                SpanXM = 1e16d,
                SpanYM = 10d,
                ThicknessM = 10d,
                CoverM = 0d,
                XDiameterMm = 2000d,
                YDiameterMm = 2000d,
                XCount = 2,
                YCount = 2,
                IncludeBottom = true,
                IncludeTop = false
            });
            AssertDirectionLength(slab, SlabMeshDirection.X, 9999999999999998d, true, "Large-scale slab X bar deduction changed unexpectedly.");

            var wall = RectangularWallMeshPlanner.Plan(new RectangularWallMeshInput
            {
                LengthM = 1e16d,
                HeightM = 10d,
                ThicknessM = 10d,
                CoverM = 0d,
                HorizontalDiameterMm = 2000d,
                VerticalDiameterMm = 2000d,
                HorizontalCount = 2,
                VerticalCount = 2,
                IncludeNear = true,
                IncludeFar = false
            });
            AssertDirectionLength(wall, WallMeshDirection.Horizontal, 9999999999999998d, true, "Large-scale wall horizontal bar deduction changed unexpectedly.");
        }

        private static void OrdinaryMeshesRemainStable()
        {
            var slab = RectangularSlabMeshPlanner.Plan(new RectangularSlabMeshInput
            {
                SpanXM = 10d,
                SpanYM = 8d,
                ThicknessM = 1d,
                CoverM = 0.05d,
                XDiameterMm = 20d,
                YDiameterMm = 20d,
                XCount = 2,
                YCount = 2,
                IncludeBottom = true,
                IncludeTop = false
            });
            AssertDirectionLength(slab, SlabMeshDirection.X, 9.88d, false, "Ordinary slab X bar length changed unexpectedly.");
            AssertDirectionLength(slab, SlabMeshDirection.Y, 7.88d, false, "Ordinary slab Y bar length changed unexpectedly.");

            var wall = RectangularWallMeshPlanner.Plan(new RectangularWallMeshInput
            {
                LengthM = 10d,
                HeightM = 8d,
                ThicknessM = 0.5d,
                CoverM = 0.05d,
                HorizontalDiameterMm = 20d,
                VerticalDiameterMm = 20d,
                HorizontalCount = 2,
                VerticalCount = 2,
                IncludeNear = true,
                IncludeFar = false
            });
            AssertDirectionLength(wall, WallMeshDirection.Horizontal, 9.88d, false, "Ordinary wall horizontal bar length changed unexpectedly.");
            AssertDirectionLength(wall, WallMeshDirection.Vertical, 7.88d, false, "Ordinary wall vertical bar length changed unexpectedly.");
        }

        private static void AssertDirectionLength(RectangularSlabMeshLayout layout, SlabMeshDirection direction, double expected, bool requireExact, string message)
        {
            var found = false;
            foreach (var bar in layout.Bars)
            {
                if (bar.Direction != direction) continue;
                found = true;
                Assert(requireExact ? bar.LengthM == expected : Math.Abs(bar.LengthM - expected) <= 1e-12d, message);
            }
            Assert(found, "Expected slab mesh direction was not generated.");
        }

        private static void AssertDirectionLength(RectangularWallMeshLayout layout, WallMeshDirection direction, double expected, bool requireExact, string message)
        {
            var found = false;
            foreach (var bar in layout.Bars)
            {
                if (bar.Direction != direction) continue;
                found = true;
                Assert(requireExact ? bar.LengthM == expected : Math.Abs(bar.LengthM - expected) <= 1e-12d, message);
            }
            Assert(found, "Expected wall mesh direction was not generated.");
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
