using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementVerticalPlacementBlankOffsetSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsBlankBottomOffsetWithLevel();
            RejectsBlankTopOffsetWithLevels();
            RejectsBlankOffsetWithoutRequiredLevel();
            PreservesMissingOffsetFallback();
            PreservesFiniteSignedOffsets();
        }

        private static void RejectsBlankBottomOffsetWithLevel()
        {
            var project = CreateProject();
            var element = new ProjectElement("BOTTOM-BLANK", ElementCategory.Beam);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "BOTTOM";
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = "   ";

            var expected = element.Id + "/" + ProjectFloorService.BottomLevelOffsetKey + " must be a finite invariant number.";
            AssertReadOnlyRejection(
                project,
                () => ElementVerticalPlacementService.Resolve(project, element, 10d, 2d, 0d),
                expected);
            AssertReadOnlyRejection(
                project,
                () => ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, 2d),
                expected);
            ThrowsExpected(
                () => ElementVerticalPlacementService.ReadLevelOffset(element, ProjectFloorService.BottomLevelOffsetKey),
                expected);
        }

        private static void RejectsBlankTopOffsetWithLevels()
        {
            var project = CreateProject();
            var element = new ProjectElement("TOP-BLANK", ElementCategory.Beam);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "BOTTOM";
            element.Properties[ProjectFloorService.TopLevelIdKey] = "TOP";
            element.Properties[ProjectFloorService.TopLevelOffsetKey] = string.Empty;

            var expected = element.Id + "/" + ProjectFloorService.TopLevelOffsetKey + " must be a finite invariant number.";
            AssertReadOnlyRejection(
                project,
                () => ElementVerticalPlacementService.Resolve(project, element, 0d, 2d, 0d),
                expected);
            AssertReadOnlyRejection(
                project,
                () => ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, 2d),
                expected);
        }

        private static void RejectsBlankOffsetWithoutRequiredLevel()
        {
            var project = CreateProject();
            var element = new ProjectElement("ORPHAN-BLANK", ElementCategory.Beam);
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = " ";

            var expected = "Level offset requires its level reference on element " + element.Id + ".";
            AssertReadOnlyRejection(
                project,
                () => ElementVerticalPlacementService.Resolve(project, element, 1d, 2d, 0.5d),
                expected);
            AssertReadOnlyRejection(
                project,
                () => ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, 2d),
                expected);
        }

        private static void PreservesMissingOffsetFallback()
        {
            var project = CreateProject();
            var element = new ProjectElement("MISSING-OFFSETS", ElementCategory.Beam);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "BOTTOM";
            element.Properties[ProjectFloorService.TopLevelIdKey] = "TOP";

            var placement = ElementVerticalPlacementService.Resolve(project, element, 100d, 2d, 10d);
            if (placement.BottomElevationM != 1d || placement.TopElevationM != 4d || placement.HeightM != 3d)
                throw new InvalidOperationException("Missing Level offsets must continue to default to zero.");
            if (ElementVerticalPlacementService.ReadLevelOffset(element, ProjectFloorService.BottomLevelOffsetKey) != 0d)
                throw new InvalidOperationException("Missing BottomLevelOffsetM must continue to read as zero.");
        }

        private static void PreservesFiniteSignedOffsets()
        {
            var project = CreateProject();
            var element = new ProjectElement("SIGNED-OFFSETS", ElementCategory.Beam);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "BOTTOM";
            element.Properties[ProjectFloorService.TopLevelIdKey] = "TOP";
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = "-0.25";
            element.Properties[ProjectFloorService.TopLevelOffsetKey] = "+0.5";

            var placement = ElementVerticalPlacementService.Resolve(project, element, 0d, 2d, 0d);
            if (placement.BottomElevationM != 0.75d || placement.TopElevationM != 4.5d || placement.HeightM != 3.75d)
                throw new InvalidOperationException("Finite signed Level offsets changed behavior.");
            if (ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, 2d) != 3.75d)
                throw new InvalidOperationException("Finite signed Level offsets changed effective-height behavior.");
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("VERTICAL-BLANK-OFFSETS", "Vertical blank offsets");
            project.Floors.Add(new FloorDefinition("BOTTOM", "Bottom", 1d));
            project.Floors.Add(new FloorDefinition("TOP", "Top", 4d));
            return project;
        }

        private static void AssertReadOnlyRejection(ProjectState project, Action action, string expectedMessage)
        {
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;
            ThrowsExpected(action, expectedMessage);
            if (project.ChangeVersion != changeVersion || project.UpdatedUtc != updatedUtc)
                throw new InvalidOperationException("Blank Level offset rejection must remain read-only.");
        }

        private static void ThrowsExpected(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, expectedMessage, StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected blank Level offset error.", ex);
                return;
            }

            throw new InvalidOperationException("Expected blank Level offset state to be rejected.");
        }
    }
}
