using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementVerticalPlacementSignedZeroSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var bottomZero = new ElementVerticalPlacement(false, false, -0d, 1d);
            CanonicalPositiveZero(bottomZero.BottomElevationM, "bottom elevation");
            Equal(1d, bottomZero.TopElevationM, "positive top elevation");
            Equal(1d, bottomZero.HeightM, "height from zero bottom");

            var topZero = new ElementVerticalPlacement(false, false, -1d, -0d);
            Equal(-1d, topZero.BottomElevationM, "negative bottom elevation");
            CanonicalPositiveZero(topZero.TopElevationM, "top elevation");
            Equal(1d, topZero.HeightM, "height to zero top");

            var host = new ElementVerticalPlacement(false, false, 0d, 3d);
            var opening = new ElementVerticalPlacement(false, false, 0d, 2d);
            var hosted = new HostedOpeningVerticalPlacement(host, opening, -0d);
            CanonicalPositiveZero(hosted.RelativeSillM, "relative sill");

            var element = new ProjectElement("E-VERTICAL-ZERO", ElementCategory.ArchitecturalWall);
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = "-0";
            CanonicalPositiveZero(
                ElementVerticalPlacementService.ReadLevelOffset(element, ProjectFloorService.BottomLevelOffsetKey),
                "parsed level offset");

            var project = new ProjectState("P-VERTICAL-ZERO", "Vertical zero smoke");
            var resolved = ElementVerticalPlacementService.Resolve(project, element: new ProjectElement("E-LEGACY-ZERO", ElementCategory.ArchitecturalWall), sourceBaseElevationM: -0d, legacyHeightM: 2d, legacyBottomOffsetM: -0d);
            CanonicalPositiveZero(resolved.BottomElevationM, "resolved legacy bottom elevation");
            Equal(2d, resolved.TopElevationM, "resolved legacy top elevation");

            Throws<ArgumentOutOfRangeException>(() => new ElementVerticalPlacement(false, false, double.NaN, 1d));
            Throws<ArgumentOutOfRangeException>(() => new ElementVerticalPlacement(false, false, 0d, double.PositiveInfinity));
            Throws<ArgumentOutOfRangeException>(() => new ElementVerticalPlacement(false, false, 1d, 1d));
            Throws<ArgumentOutOfRangeException>(() => new HostedOpeningVerticalPlacement(host, opening, -1d));
            Throws<ArgumentOutOfRangeException>(() => new HostedOpeningVerticalPlacement(host, opening, double.NaN));
        }

        private static void CanonicalPositiveZero(double value, string label)
        {
            if (value != 0d)
                throw new InvalidOperationException(label + ": expected zero but got " + value + ".");
            if (BitConverter.DoubleToInt64Bits(value) != BitConverter.DoubleToInt64Bits(0d))
                throw new InvalidOperationException(label + ": expected canonical positive zero.");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + ": expected " + expected + " but got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }
    }
}
