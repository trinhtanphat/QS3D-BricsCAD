using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallPanelFingerprintSourceKindCanonicalSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var canonical = Create("Line", 0);
            var lowerCase = Create("line", 0);
            var canonicalHash = CurtainWallPanelFingerprint.Compute(canonical);
            var lowerHash = CurtainWallPanelFingerprint.Compute(lowerCase);
            if (!string.Equals(canonicalHash, lowerHash, StringComparison.Ordinal))
                throw new InvalidOperationException("SourceKind case-insensitive canonical behavior changed unexpectedly.");

            var openPolylineHash = CurtainWallPanelFingerprint.Compute(Create("OpenPolyline", 1));
            if (string.Equals(canonicalHash, openPolylineHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Distinct curtain panel source kinds must not share a fingerprint.");

            ExpectArgument(() => CurtainWallPanelFingerprint.Compute(Create(" Line", 0)));
            ExpectArgument(() => CurtainWallPanelFingerprint.Compute(Create("Line ", 0)));
            ExpectArgument(() => CurtainWallPanelFingerprint.Compute(Create("\tOpenPolyline", 1)));
            ExpectArgument(() => CurtainWallPanelFingerprint.Compute(Create("OpenPolyline\n", 1)));
            ExpectArgument(() => CurtainWallPanelFingerprint.Compute(Create(null, 0)));
            ExpectArgument(() => CurtainWallPanelFingerprint.Compute(Create(string.Empty, 0)));
            ExpectArgument(() => CurtainWallPanelFingerprint.Compute(Create("Arc", 0)));
        }

        private static CurtainWallPanelFingerprintInput Create(string sourceKind, int pathSegmentCount)
        {
            return new CurtainWallPanelFingerprintInput
            {
                SourceLengthM = 4d,
                HeightM = 3d,
                BottomOffsetM = 0d,
                PanelDepthM = 0.12d,
                SourceKind = sourceKind,
                PathSegmentCount = pathSegmentCount,
                Pieces = new[]
                {
                    new CurtainWallPanelPiece
                    {
                        SourcePanelIndex = 0,
                        X_M = 0d,
                        Z_M = 0d,
                        WidthM = 4d,
                        HeightM = 3d
                    }
                }
            };
        }

        private static void ExpectArgument(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("Malformed curtain panel SourceKind must fail closed.");
        }
    }
}
