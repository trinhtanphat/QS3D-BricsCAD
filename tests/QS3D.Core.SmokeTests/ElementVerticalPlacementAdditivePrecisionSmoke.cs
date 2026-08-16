using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementVerticalPlacementAdditivePrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            LegacyBottomOffsetMustNotDisappear();
            LegacyHeightMustNotDisappear();
            LevelBottomOffsetMustNotDisappear();
            TopLevelOffsetMustNotDisappear();
            OrdinaryAndZeroTermsRemainValid();
            CancellationRemainsValid();
        }

        private static void LegacyBottomOffsetMustNotDisappear()
        {
            var project = NewProject();
            var element = NewElement("legacy-bottom");
            ExpectPrecisionLoss(() => ElementVerticalPlacementService.Resolve(project, element, 1e16d, 4d, 1d));
        }

        private static void LegacyHeightMustNotDisappear()
        {
            var project = NewProject();
            var element = NewElement("legacy-height");
            ExpectPrecisionLoss(() => ElementVerticalPlacementService.Resolve(project, element, 1e16d, 1d, 0d));
        }

        private static void LevelBottomOffsetMustNotDisappear()
        {
            var project = NewProject();
            project.Floors.Add(new FloorDefinition("big", "Big", 1e16d));
            var element = NewElement("level-bottom");
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "big";
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = "1";
            ExpectPrecisionLoss(() => ElementVerticalPlacementService.Resolve(project, element, 0d, 4d, 0d));
        }

        private static void TopLevelOffsetMustNotDisappear()
        {
            var project = NewProject();
            project.Floors.Add(new FloorDefinition("base", "Base", 0d));
            project.Floors.Add(new FloorDefinition("big", "Big", 1e16d));
            var element = NewElement("level-top");
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "base";
            element.Properties[ProjectFloorService.TopLevelIdKey] = "big";
            element.Properties[ProjectFloorService.TopLevelOffsetKey] = "1";
            ExpectPrecisionLoss(() => ElementVerticalPlacementService.Resolve(project, element, 0d, 4d, 0d));
        }

        private static void OrdinaryAndZeroTermsRemainValid()
        {
            var project = NewProject();
            var ordinary = ElementVerticalPlacementService.Resolve(project, NewElement("ordinary"), 100d, 4d, 1d);
            Equal(101d, ordinary.BottomElevationM, "ordinary bottom");
            Equal(105d, ordinary.TopElevationM, "ordinary top");

            var largeZero = ElementVerticalPlacementService.Resolve(project, NewElement("zero"), 1e16d, 4d, 0d);
            Equal(1e16d, largeZero.BottomElevationM, "large zero bottom");
            Equal(1e16d + 4d, largeZero.TopElevationM, "large zero top");
        }

        private static void CancellationRemainsValid()
        {
            var placement = ElementVerticalPlacementService.Resolve(NewProject(), NewElement("cancel"), 1d, 2d, -1d);
            Equal(0d, placement.BottomElevationM, "cancellation bottom");
            Equal(2d, placement.TopElevationM, "cancellation top");
        }

        private static ProjectState NewProject() => new ProjectState(Guid.NewGuid().ToString("N"), "Vertical placement additive precision");

        private static ProjectElement NewElement(string id) =>
            new ProjectElement(id, ElementCategory.Room, string.Empty, string.Empty, string.Empty);

        private static void ExpectPrecisionLoss(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex) when (
                ex.Message.IndexOf("precision", StringComparison.OrdinalIgnoreCase) >= 0 &&
                ex.Message.IndexOf("non-zero", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            throw new InvalidOperationException("A swallowed non-zero vertical-placement term must fail closed with a precision-loss diagnostic.");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + " mismatch. Expected " + expected.ToString("R") + ", actual " + actual.ToString("R") + ".");
        }
    }
}
