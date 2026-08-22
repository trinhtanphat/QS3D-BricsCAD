using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallPanelFingerprintAreaFiniteSmoke
    {
        internal static void Run()
        {
            OverflowingAreaIsRejected();
            FiniteAreaFingerprintRemainsDeterministic();
            SignedZeroCoordinatesRemainCanonical();
        }

        private static void OverflowingAreaIsRejected()
        {
            var input = Input(new CurtainWallPanelPiece
            {
                SourcePanelIndex = 0,
                X_M = 0d,
                Z_M = 0d,
                WidthM = 1e308d,
                HeightM = 1e308d
            });

            Throws<OverflowException>(() => CurtainWallPanelFingerprint.Compute(input));
        }

        private static void FiniteAreaFingerprintRemainsDeterministic()
        {
            var input = Input(new CurtainWallPanelPiece
            {
                SourcePanelIndex = 0,
                X_M = 0d,
                Z_M = 0d,
                WidthM = 2d,
                HeightM = 3d
            });

            var first = CurtainWallPanelFingerprint.Compute(input);
            var second = CurtainWallPanelFingerprint.Compute(input);

            Equal(64, first.Length);
            Equal(first, second);
        }

        private static void SignedZeroCoordinatesRemainCanonical()
        {
            var positive = Input(new CurtainWallPanelPiece
            {
                SourcePanelIndex = 0,
                X_M = 0d,
                Z_M = 0d,
                WidthM = 2d,
                HeightM = 3d
            });
            var negative = Input(new CurtainWallPanelPiece
            {
                SourcePanelIndex = 0,
                X_M = -0d,
                Z_M = -0d,
                WidthM = 2d,
                HeightM = 3d
            });
            Equal(CurtainWallPanelFingerprint.Compute(positive), CurtainWallPanelFingerprint.Compute(negative));

            positive.BottomOffsetM = 0d;
            negative.BottomOffsetM = -0d;
            Equal(CurtainWallPanelFingerprint.Compute(positive), CurtainWallPanelFingerprint.Compute(negative));
        }

        private static CurtainWallPanelFingerprintInput Input(CurtainWallPanelPiece piece)
        {
            return new CurtainWallPanelFingerprintInput
            {
                SourceLengthM = 2d,
                HeightM = 3d,
                BottomOffsetM = 0d,
                PanelDepthM = 0.1d,
                SourceKind = "Line",
                PathSegmentCount = 0,
                Pieces = new[] { piece }
            };
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class CurtainWallPanelFingerprintAreaFiniteSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CurtainWallPanelFingerprintAreaFiniteSmoke.Run();
    }
}
