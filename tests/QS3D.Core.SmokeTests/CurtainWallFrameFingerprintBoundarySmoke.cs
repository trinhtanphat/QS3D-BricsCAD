using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallFrameFingerprintBoundarySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CanonicalInputsProduceStableDigest();
            SignedZeroIsCanonicalAndFieldsAreSensitive();
            ScalarContractsFailClosed();
            VerticalEnvelopeMustRemainRepresentable();
            GrossAreaMustRemainRepresentable();
            LayoutFeasibilityRemainsPlannerOwned();
        }

        private static void CanonicalInputsProduceStableDigest()
        {
            var input = Input();
            var first = CurtainWallFrameFingerprint.Compute(input);
            var second = CurtainWallFrameFingerprint.Compute(input);
            Equal(first, second, "identical frame input must fingerprint deterministically");
            if (first.Length != 64)
                throw new InvalidOperationException("Curtain frame fingerprint must be a 64-character SHA-256 hex digest.");
            for (var i = 0; i < first.Length; i++)
            {
                var c = first[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                    throw new InvalidOperationException("Curtain frame fingerprint must use canonical lowercase hex.");
            }
        }

        private static void SignedZeroIsCanonicalAndFieldsAreSensitive()
        {
            var baseline = CurtainWallFrameFingerprint.Compute(Input());
            var negativeZero = Input();
            negativeZero.BottomOffsetM = -0d;
            Equal(baseline, CurtainWallFrameFingerprint.Compute(negativeZero), "signed zero must canonicalize");

            var changed = Input();
            changed.FrameDepthM = 0.151d;
            NotEqual(baseline, CurtainWallFrameFingerprint.Compute(changed), "frame depth change must alter fingerprint");

            changed = Input();
            changed.BottomOffsetM = 1d;
            NotEqual(baseline, CurtainWallFrameFingerprint.Compute(changed), "bottom offset change must alter fingerprint");
        }

        private static void ScalarContractsFailClosed()
        {
            Expect<ArgumentNullException>(() => CurtainWallFrameFingerprint.Compute(null!), "null input");

            var nan = Input();
            nan.LengthM = double.NaN;
            Expect<ArgumentOutOfRangeException>(() => CurtainWallFrameFingerprint.Compute(nan), "NaN length");

            var infinity = Input();
            infinity.FrameDepthM = double.PositiveInfinity;
            Expect<ArgumentOutOfRangeException>(() => CurtainWallFrameFingerprint.Compute(infinity), "infinite depth");

            var zeroLength = Input();
            zeroLength.LengthM = 0d;
            Expect<ArgumentOutOfRangeException>(() => CurtainWallFrameFingerprint.Compute(zeroLength), "zero length");

            var zeroHeight = Input();
            zeroHeight.HeightM = 0d;
            Expect<ArgumentOutOfRangeException>(() => CurtainWallFrameFingerprint.Compute(zeroHeight), "zero height");

            var negativePerimeter = Input();
            negativePerimeter.PerimeterFrameWidthM = -0.01d;
            Expect<ArgumentOutOfRangeException>(() => CurtainWallFrameFingerprint.Compute(negativePerimeter), "negative perimeter frame width");
        }

        private static void VerticalEnvelopeMustRemainRepresentable()
        {
            var lostHeight = Input();
            lostHeight.BottomOffsetM = 1e308d;
            lostHeight.HeightM = 1d;
            Expect<OverflowException>(() => CurtainWallFrameFingerprint.Compute(lostHeight), "height lost at extreme bottom elevation");

            var overflowingTop = Input();
            overflowingTop.BottomOffsetM = double.MaxValue;
            overflowingTop.HeightM = double.MaxValue;
            Expect<OverflowException>(() => CurtainWallFrameFingerprint.Compute(overflowingTop), "overflowing top elevation");

            var largeRepresentable = Input();
            largeRepresentable.BottomOffsetM = 1e100d;
            largeRepresentable.HeightM = 1e90d;
            var digest = CurtainWallFrameFingerprint.Compute(largeRepresentable);
            if (digest.Length != 64)
                throw new InvalidOperationException("Representable large vertical envelope must remain fingerprintable.");
        }

        private static void GrossAreaMustRemainRepresentable()
        {
            var overflowingArea = Input();
            overflowingArea.LengthM = double.MaxValue;
            overflowingArea.HeightM = 2d;
            Expect<OverflowException>(() => CurtainWallFrameFingerprint.Compute(overflowingArea), "overflowing gross area");

            var underflowingArea = Input();
            underflowingArea.LengthM = 1e-200d;
            underflowingArea.HeightM = 1e-200d;
            Expect<OverflowException>(() => CurtainWallFrameFingerprint.Compute(underflowingArea), "gross area underflow");

            var smallRepresentable = Input();
            smallRepresentable.LengthM = 1e-100d;
            smallRepresentable.HeightM = 1e-100d;
            smallRepresentable.MaxPanelWidthM = 1e-100d;
            smallRepresentable.MaxPanelHeightM = 1e-100d;
            smallRepresentable.FrameDepthM = 1e-100d;
            var digest = CurtainWallFrameFingerprint.Compute(smallRepresentable);
            if (digest.Length != 64)
                throw new InvalidOperationException("Representable small gross area must remain fingerprintable.");
        }

        private static void LayoutFeasibilityRemainsPlannerOwned()
        {
            var impossibleClearSpan = Input();
            impossibleClearSpan.LengthM = 1d;
            impossibleClearSpan.MaxPanelWidthM = 2d;
            impossibleClearSpan.PerimeterFrameWidthM = 0.6d;

            var digest = CurtainWallFrameFingerprint.Compute(impossibleClearSpan);
            if (digest.Length != 64)
                throw new InvalidOperationException("Fingerprint must remain a canonical scalar/provenance contract; layout feasibility belongs to CurtainWallLayoutPlanner.");

            Expect<InvalidOperationException>(() => CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
            {
                LengthM = impossibleClearSpan.LengthM,
                HeightM = impossibleClearSpan.HeightM,
                MaxPanelWidthM = impossibleClearSpan.MaxPanelWidthM,
                MaxPanelHeightM = impossibleClearSpan.MaxPanelHeightM,
                PerimeterFrameWidthM = impossibleClearSpan.PerimeterFrameWidthM,
                MullionWidthM = impossibleClearSpan.MullionWidthM,
                TransomWidthM = impossibleClearSpan.TransomWidthM
            }), "impossible clear span must be rejected by layout planner");
        }

        private static CurtainWallFrameFingerprintInput Input()
            => new CurtainWallFrameFingerprintInput
            {
                LengthM = 6d,
                HeightM = 3d,
                BottomOffsetM = 0d,
                MaxPanelWidthM = 1.5d,
                MaxPanelHeightM = 1.5d,
                PerimeterFrameWidthM = 0.08d,
                MullionWidthM = 0.05d,
                TransomWidthM = 0.05d,
                FrameDepthM = 0.15d
            };

        private static void Expect<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException(label + " must fail with " + typeof(TException).Name + ".");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + ".");
        }

        private static void NotEqual(string expected, string actual, string label)
        {
            if (string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + ".");
        }
    }
}
