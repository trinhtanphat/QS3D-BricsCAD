using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallDetailSmoke
    {
        public static void Run()
        {
            DetailGridMatchesClearGlassArea();
            SinglePanelKeepsOnlyPerimeterFrames();
            NativeDetailCapRejectsHugeGrid();
            PublicRectRejectsInvalidGeometry();
            PublicRectPreservesCanonicalGeometry();
            FrameFingerprintPreservesStableDigest();
            PanelFingerprintPreservesStableDigest();
            FingerprintsStillRejectInvalidSnapshots();
        }

        private static CurtainWallLayoutInput Standard() => new CurtainWallLayoutInput
        {
            LengthM = 6d,
            HeightM = 3d,
            MaxPanelWidthM = 1.5d,
            MaxPanelHeightM = 1.5d,
            PerimeterFrameWidthM = 0.05d,
            MullionWidthM = 0.05d,
            TransomWidthM = 0.05d
        };

        private static void DetailGridMatchesClearGlassArea()
        {
            var detail = CurtainWallDetailPlanner.Plan(Standard());
            Equal(8, detail.Panels.Count);
            Equal(5, detail.VerticalFrames.Count);
            Equal(3, detail.HorizontalFrames.Count);
            Equal(16, detail.DetailSolidCount);
            var area = 0d;
            foreach (var panel in detail.Panels) area += panel.AreaM2;
            Near(detail.Layout.ClearGlassAreaM2, area);
            Near(detail.PanelAreaM2, area);
            Near(0.05d, detail.VerticalFrames[0].WidthM);
            Near(0.05d, detail.HorizontalFrames[0].HeightM);
        }

        private static void SinglePanelKeepsOnlyPerimeterFrames()
        {
            var detail = CurtainWallDetailPlanner.Plan(new CurtainWallLayoutInput
            {
                LengthM = 1d,
                HeightM = 1d,
                MaxPanelWidthM = 2d,
                MaxPanelHeightM = 2d,
                PerimeterFrameWidthM = 0.05d,
                MullionWidthM = 0.05d,
                TransomWidthM = 0.05d
            });
            Equal(1, detail.Panels.Count);
            Equal(2, detail.VerticalFrames.Count);
            Equal(2, detail.HorizontalFrames.Count);
            Near(0.05d, detail.Panels[0].X_M);
            Near(0.05d, detail.Panels[0].Z_M);
            Near(0.9d, detail.Panels[0].WidthM);
            Near(0.9d, detail.Panels[0].HeightM);
        }

        private static void NativeDetailCapRejectsHugeGrid()
        {
            Throws<InvalidOperationException>(() => CurtainWallDetailPlanner.Plan(new CurtainWallLayoutInput
            {
                LengthM = 200d,
                HeightM = 200d,
                MaxPanelWidthM = 1d,
                MaxPanelHeightM = 1d,
                PerimeterFrameWidthM = 0.02d,
                MullionWidthM = 0.02d,
                TransomWidthM = 0.02d
            }));
        }

        private static void PublicRectRejectsInvalidGeometry()
        {
            Throws<ArgumentOutOfRangeException>(() => new CurtainWallRect(double.NaN, 0d, 1d, 1d));
            Throws<ArgumentOutOfRangeException>(() => new CurtainWallRect(0d, double.PositiveInfinity, 1d, 1d));
            Throws<ArgumentOutOfRangeException>(() => new CurtainWallRect(-1d, 0d, 1d, 1d));
            Throws<ArgumentOutOfRangeException>(() => new CurtainWallRect(0d, -1d, 1d, 1d));
            Throws<ArgumentOutOfRangeException>(() => new CurtainWallRect(0d, 0d, 0d, 1d));
            Throws<ArgumentOutOfRangeException>(() => new CurtainWallRect(0d, 0d, -1d, -1d));
            Throws<ArgumentOutOfRangeException>(() => new CurtainWallRect(0d, 0d, double.NegativeInfinity, 1d));
            Throws<OverflowException>(() => new CurtainWallRect(double.MaxValue, 0d, double.MaxValue, 1d));
            Throws<OverflowException>(() => new CurtainWallRect(double.MaxValue, 0d, double.Epsilon, 1d));
            Throws<OverflowException>(() => new CurtainWallRect(0d, double.MaxValue, 1d, double.Epsilon));
        }

        private static void PublicRectPreservesCanonicalGeometry()
        {
            var rect = new CurtainWallRect(1.25d, 2.5d, 3d, 4d);
            Near(1.25d, rect.X_M);
            Near(2.5d, rect.Z_M);
            Near(3d, rect.WidthM);
            Near(4d, rect.HeightM);
            Near(12d, rect.AreaM2);
        }

        private static void FrameFingerprintPreservesStableDigest()
        {
            var digest = CurtainWallFrameFingerprint.Compute(new CurtainWallFrameFingerprintInput
            {
                LengthM = 5d,
                HeightM = 3d,
                BottomOffsetM = 0.25d,
                MaxPanelWidthM = 1.2d,
                MaxPanelHeightM = 1.5d,
                PerimeterFrameWidthM = 0.05d,
                MullionWidthM = 0.04d,
                TransomWidthM = 0.03d,
                FrameDepthM = 0.12d
            });

            Equal("fc4adc60f49495179c4d63efc8c716de431e36e0da63befddb9bc8bfc2232735", digest);
        }

        private static void PanelFingerprintPreservesStableDigest()
        {
            var digest = CurtainWallPanelFingerprint.Compute(new CurtainWallPanelFingerprintInput
            {
                SourceLengthM = 5d,
                HeightM = 3d,
                BottomOffsetM = 0.25d,
                PanelDepthM = 0.12d,
                SourceKind = "Line",
                PathSegmentCount = 0,
                Pieces = new[]
                {
                    new CurtainWallPanelPiece
                    {
                        SourcePanelIndex = 0,
                        X_M = 0d,
                        Z_M = 0.25d,
                        WidthM = 1d,
                        HeightM = 1d
                    }
                }
            });

            Equal("66b33d87ab85ad08bbe90be45ada4948ac7a76b3cac0ab98a89de8eb09f3f49c", digest);
        }

        private static void FingerprintsStillRejectInvalidSnapshots()
        {
            Throws<ArgumentOutOfRangeException>(() => CurtainWallFrameFingerprint.Compute(new CurtainWallFrameFingerprintInput
            {
                LengthM = double.NaN,
                HeightM = 3d,
                BottomOffsetM = 0d,
                MaxPanelWidthM = 1d,
                MaxPanelHeightM = 1d,
                FrameDepthM = 0.1d
            }));

            Throws<OverflowException>(() => CurtainWallPanelFingerprint.Compute(new CurtainWallPanelFingerprintInput
            {
                SourceLengthM = 5d,
                HeightM = 3d,
                BottomOffsetM = 0d,
                PanelDepthM = 0.1d,
                SourceKind = "Line",
                Pieces = new[]
                {
                    new CurtainWallPanelPiece
                    {
                        SourcePanelIndex = 0,
                        X_M = double.MaxValue,
                        Z_M = 0d,
                        WidthM = double.Epsilon,
                        HeightM = 1d
                    }
                }
            }));
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}