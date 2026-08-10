using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainFrameFingerprintSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DeterministicForSameConfiguration();
            ChangesWhenGridOrDepthChanges();
            RejectsInvalidConfiguration();
        }

        private static CurtainWallFrameFingerprintInput Baseline() => new CurtainWallFrameFingerprintInput
        {
            LengthM = 6d,
            HeightM = 3.6d,
            BottomOffsetM = 0.15d,
            MaxPanelWidthM = 1.2d,
            MaxPanelHeightM = 1.5d,
            PerimeterFrameWidthM = 0.05d,
            MullionWidthM = 0.05d,
            TransomWidthM = 0.05d,
            FrameDepthM = 0.06d
        };

        private static void DeterministicForSameConfiguration()
        {
            var a = CurtainWallFrameFingerprint.Compute(Baseline());
            var b = CurtainWallFrameFingerprint.Compute(Baseline());
            Equal(a, b);
            True(a.Length == 64);
        }

        private static void ChangesWhenGridOrDepthChanges()
        {
            var baseline = CurtainWallFrameFingerprint.Compute(Baseline());
            var grid = Baseline(); grid.MaxPanelWidthM = 1.25d;
            var depth = Baseline(); depth.FrameDepthM = 0.08d;
            var offset = Baseline(); offset.BottomOffsetM = 0.2d;
            NotEqual(baseline, CurtainWallFrameFingerprint.Compute(grid));
            NotEqual(baseline, CurtainWallFrameFingerprint.Compute(depth));
            NotEqual(baseline, CurtainWallFrameFingerprint.Compute(offset));
        }

        private static void RejectsInvalidConfiguration()
        {
            var invalid = Baseline(); invalid.FrameDepthM = 0d;
            Throws<ArgumentOutOfRangeException>(() => CurtainWallFrameFingerprint.Compute(invalid));
            invalid = Baseline(); invalid.MullionWidthM = -0.01d;
            Throws<ArgumentOutOfRangeException>(() => CurtainWallFrameFingerprint.Compute(invalid));
            invalid = Baseline(); invalid.LengthM = double.PositiveInfinity;
            Throws<ArgumentOutOfRangeException>(() => CurtainWallFrameFingerprint.Compute(invalid));
        }

        private static void True(bool condition)
        {
            if (!condition) throw new InvalidOperationException("Curtain frame fingerprint smoke assertion failed.");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new InvalidOperationException("Curtain frame fingerprint must be deterministic.");
        }

        private static void NotEqual(string left, string right)
        {
            if (string.Equals(left, right, StringComparison.Ordinal)) throw new InvalidOperationException("Curtain frame fingerprint must change when geometry configuration changes.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
