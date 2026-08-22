using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticPropertyUnitClassifierSmoke
    {
        internal static void Run()
        {
            KnownLinearMeterPropertiesAreRecognized();
            NonLinearAndArbitraryPropertiesAreNotMisclassified();
        }

        private static void KnownLinearMeterPropertiesAreRecognized()
        {
            var keys = new[]
            {
                "ThicknessM",
                "WidthM",
                "DepthM",
                "HeightM",
                "LengthM",
                "PerimeterM",
                "BottomOffsetM",
                "TopOffsetM",
                "SillHeightM",
                "AxisLeftOffsetM",
                "AxisRightOffsetM",
                "CoverM",
                "BooleanClearanceM",
                "WallArcSagittaM",
                "BaseElevationM",
                "PipeRadiusM"
            };

            foreach (var key in keys)
                if (!SemanticPropertyUnitClassifier.IsLinearMeterProperty(key))
                    throw new Exception("Expected linear meter semantic property: " + key);
        }

        private static void NonLinearAndArbitraryPropertiesAreNotMisclassified()
        {
            var keys = new[]
            {
                "AreaM2",
                "VolumeM3",
                "RebarDiameterMm",
                "SpacingMm",
                "BIM",
                "Team",
                "Form",
                "Material",
                "ProfileMode",
                "ClassificationCode",
                string.Empty
            };

            foreach (var key in keys)
                if (SemanticPropertyUnitClassifier.IsLinearMeterProperty(key))
                    throw new Exception("Non-linear semantic property was misclassified as meter-backed length: " + key);
        }
    }

    internal static class SemanticPropertyUnitClassifierSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticPropertyUnitClassifierSmoke.Run();
    }
}
