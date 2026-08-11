using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class LevelReferenceSmoke
    {
        public static void Run()
        {
            LegacyPlacementRemainsSourceRelative();
            BottomAndTopLevelsResolveAbsolutePlacement();
            LevelReferencesValidateOnlyConsumedLegacyInputs();
            TopAssignmentRequiresBottomAndValidRange();
            DuplicateLevelIdsFailClosedDuringPlacement();
            FloorMutationTracksAllReferenceKinds();
            HealthRejectsBrokenLevelReferences();
            HealthBlocksValidLevelReferencesUntilNativeQualification();
        }

        private static void LegacyPlacementRemainsSourceRelative()
        {
            var project = NewProject();
            var element = NewElement(project, "legacy");
            var placement = ElementVerticalPlacementService.Resolve(project, element, 10d, 3d, 0.2d);
            True(!placement.UsesBottomLevel);
            True(!placement.UsesTopLevel);
            Near(10.2d, placement.BottomElevationM);
            Near(13.2d, placement.TopElevationM);
            Near(3d, placement.HeightM);
        }

        private static void BottomAndTopLevelsResolveAbsolutePlacement()
        {
            var project = NewProject();
            var element = NewElement(project, "absolute");
            Equal(1, ProjectFloorService.AssignBottomLevel(project, "L1", new[] { element }));
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = "0.15";
            var bottomOnly = ElementVerticalPlacementService.Resolve(project, element, 100d, 3d, 99d);
            True(bottomOnly.UsesBottomLevel);
            True(!bottomOnly.UsesTopLevel);
            Near(3.15d, bottomOnly.BottomElevationM);
            Near(6.15d, bottomOnly.TopElevationM);

            Equal(1, ProjectFloorService.AssignTopLevel(project, "L2", new[] { element }));
            element.Properties[ProjectFloorService.TopLevelOffsetKey] = "-0.1";
            var bounded = ElementVerticalPlacementService.Resolve(project, element, 100d, 99d, 99d);
            True(bounded.UsesBottomLevel);
            True(bounded.UsesTopLevel);
            Near(3.15d, bounded.BottomElevationM);
            Near(6.9d, bounded.TopElevationM);
            Near(3.75d, bounded.HeightM);

            Equal(1, ProjectFloorService.ClearVerticalLevels(project, new[] { element }));
            var legacyAgain = ElementVerticalPlacementService.Resolve(project, element, 10d, 3d, 0.2d);
            True(!legacyAgain.UsesBottomLevel);
            Near(10.2d, legacyAgain.BottomElevationM);
        }

        private static void TopAssignmentRequiresBottomAndValidRange()
        {
            var project = NewProject();
            var missingBottom = NewElement(project, "missing-bottom");
            Throws<InvalidOperationException>(() => ProjectFloorService.AssignTopLevel(project, "L2", new[] { missingBottom }));
            True(!missingBottom.Properties.ContainsKey(ProjectFloorService.TopLevelIdKey));

            var invalidRange = NewElement(project, "invalid-range");
            Equal(1, ProjectFloorService.AssignBottomLevel(project, "L2", new[] { invalidRange }));
            Throws<InvalidOperationException>(() => ProjectFloorService.AssignTopLevel(project, "L1", new[] { invalidRange }));
            True(!invalidRange.Properties.ContainsKey(ProjectFloorService.TopLevelIdKey));
        }

        private static void LevelReferencesValidateOnlyConsumedLegacyInputs()
        {
            var project = NewProject();

            var bottomOnly = NewElement(project, "bottom-ignores-source-legacy");
            Equal(1, ProjectFloorService.AssignBottomLevel(project, "L1", new[] { bottomOnly }));
            var bottomPlacement = ElementVerticalPlacementService.Resolve(
                project,
                bottomOnly,
                double.NaN,
                2.5d,
                double.PositiveInfinity);
            Near(3d, bottomPlacement.BottomElevationM);
            Near(5.5d, bottomPlacement.TopElevationM);
            Throws<ArgumentOutOfRangeException>(() => ElementVerticalPlacementService.Resolve(
                project,
                bottomOnly,
                double.NaN,
                0d,
                double.PositiveInfinity));

            var bounded = NewElement(project, "bounded-ignores-all-legacy");
            Equal(1, ProjectFloorService.AssignBottomLevel(project, "L1", new[] { bounded }));
            Equal(1, ProjectFloorService.AssignTopLevel(project, "L2", new[] { bounded }));
            var boundedPlacement = ElementVerticalPlacementService.Resolve(
                project,
                bounded,
                double.NaN,
                double.NaN,
                double.NegativeInfinity);
            Near(3d, boundedPlacement.BottomElevationM);
            Near(7d, boundedPlacement.TopElevationM);
            Near(4d, boundedPlacement.HeightM);

            var legacy = NewElement(project, "legacy-validates-all-inputs");
            Throws<ArgumentOutOfRangeException>(() => ElementVerticalPlacementService.Resolve(project, legacy, double.NaN, 3d, 0d));
            Throws<ArgumentOutOfRangeException>(() => ElementVerticalPlacementService.Resolve(project, legacy, 0d, 0d, 0d));
            Throws<ArgumentOutOfRangeException>(() => ElementVerticalPlacementService.Resolve(project, legacy, 0d, 3d, double.PositiveInfinity));
        }

        private static void DuplicateLevelIdsFailClosedDuringPlacement()
        {
            var project = NewProject();
            project.Floors.Add(new FloorDefinition("l1", "Ambiguous Level 1", 30d));
            var element = NewElement(project, "duplicate-level");
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "L1";
            Throws<InvalidOperationException>(() => ElementVerticalPlacementService.Resolve(project, element, 0d, 3d, 0d));
        }

        private static void FloorMutationTracksAllReferenceKinds()
        {
            var project = NewProject();
            var floorOnly = NewElement(project, "floor-only");
            floorOnly.FloorId = "L1";
            var bottom = NewElement(project, "bottom");
            ProjectFloorService.AssignBottomLevel(project, "L1", new[] { bottom });
            var top = NewElement(project, "top");
            ProjectFloorService.AssignBottomLevel(project, "L0", new[] { top });
            ProjectFloorService.AssignTopLevel(project, "L1", new[] { top });

            floorOnly.MarkClean(ElementDirtyFlags.All);
            bottom.MarkClean(ElementDirtyFlags.All);
            top.MarkClean(ElementDirtyFlags.All);
            ProjectFloorService.Update(project, "L1", "Level 1", 3.2d);
            True((floorOnly.Dirty & ElementDirtyFlags.Geometry) != 0);
            True((bottom.Dirty & ElementDirtyFlags.Geometry) != 0);
            True((top.Dirty & ElementDirtyFlags.Geometry) != 0);
            Equal(3, ProjectFloorService.ReferenceCount(project, "L1"));

            ProjectFloorService.SetActive(project, "L2");
            Throws<InvalidOperationException>(() => ProjectFloorService.Delete(project, "L1"));
        }

        private static void HealthRejectsBrokenLevelReferences()
        {
            var project = NewProject();
            var missingBottom = NewElement(project, "bad-bottom");
            missingBottom.Properties[ProjectFloorService.BottomLevelIdKey] = "missing";
            var topWithoutBottom = NewElement(project, "bad-top");
            topWithoutBottom.Properties[ProjectFloorService.TopLevelIdKey] = "L2";
            topWithoutBottom.Properties[ProjectFloorService.TopLevelOffsetKey] = "0.2";
            var badOffset = NewElement(project, "bad-offset");
            badOffset.Properties[ProjectFloorService.BottomLevelIdKey] = "L1";
            badOffset.Properties[ProjectFloorService.BottomLevelOffsetKey] = "NaN";
            var badRange = NewElement(project, "bad-range");
            badRange.Properties[ProjectFloorService.BottomLevelIdKey] = "L2";
            badRange.Properties[ProjectFloorService.TopLevelIdKey] = "L1";

            var issues = new LevelReferenceHealthService().Inspect(project);
            True(issues.Any(x => x.Code == "BOTTOM_LEVEL_REFERENCE_INVALID" && x.ElementId == missingBottom.Id));
            True(issues.Any(x => x.Code == "TOP_LEVEL_REQUIRES_BOTTOM_LEVEL" && x.ElementId == topWithoutBottom.Id));
            True(!issues.Any(x => x.Code == "TOP_LEVEL_OFFSET_WITHOUT_LEVEL" && x.ElementId == topWithoutBottom.Id));
            True(issues.Any(x => x.Code == "BOTTOM_LEVEL_OFFSET_INVALID" && x.ElementId == badOffset.Id));
            True(issues.Any(x => x.Code == "LEVEL_RANGE_INVALID" && x.ElementId == badRange.Id));
            True(!issues.Any(x => x.Code == "LEVEL_REFERENCE_NATIVE_INTEGRATION_PENDING" && x.ElementId == missingBottom.Id));
            True(!issues.Any(x => x.Code == "LEVEL_REFERENCE_NATIVE_INTEGRATION_PENDING" && x.ElementId == topWithoutBottom.Id));
        }

        private static void HealthBlocksValidLevelReferencesUntilNativeQualification()
        {
            var project = NewProject();
            var bottomOnly = NewElement(project, "valid-bottom");
            ProjectFloorService.AssignBottomLevel(project, "L1", new[] { bottomOnly });
            bottomOnly.Properties[ProjectFloorService.BottomLevelOffsetKey] = "0.1";

            var bounded = NewElement(project, "valid-bounded");
            ProjectFloorService.AssignBottomLevel(project, "L1", new[] { bounded });
            ProjectFloorService.AssignTopLevel(project, "L2", new[] { bounded });
            bounded.Properties[ProjectFloorService.TopLevelOffsetKey] = "-0.1";

            var issues = new LevelReferenceHealthService().Inspect(project);
            True(issues.Any(x => x.Code == "LEVEL_REFERENCE_NATIVE_INTEGRATION_PENDING" && x.ElementId == bottomOnly.Id));
            True(issues.Any(x => x.Code == "LEVEL_REFERENCE_NATIVE_INTEGRATION_PENDING" && x.ElementId == bounded.Id));
            True(!LevelReferenceNativeIntegrationPolicy.IsQualified(ElementCategory.Beam));
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState("level-ref", "Level Reference");
            project.Floors.Add(new FloorDefinition("L0", "Level 0", 0d));
            project.Floors.Add(new FloorDefinition("L1", "Level 1", 3d));
            project.Floors.Add(new FloorDefinition("L2", "Level 2", 7d));
            project.ActiveFloorId = "L0";
            return project;
        }

        private static ProjectElement NewElement(ProjectState project, string id)
        {
            var element = new ProjectElement(id, ElementCategory.Beam, string.Empty, "L0", string.Empty);
            project.Elements.Add(element);
            return element;
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9d) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
