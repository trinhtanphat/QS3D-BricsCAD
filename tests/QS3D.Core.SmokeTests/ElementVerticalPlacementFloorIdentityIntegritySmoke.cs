using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementVerticalPlacementFloorIdentityIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsUnrelatedDuplicateFloors();
            RejectsNullFloorAtCatalogBoundary();
            PreservesValidFloorPlacement();
            PreservesLegacyFallbackWithoutLevelMetadata();
        }

        private static void RejectsUnrelatedDuplicateFloors()
        {
            var project = CreateProjectWithLevels();
            project.Floors.Add(new FloorDefinition("OTHER", "Other A", 6d));
            project.Floors.Add(new FloorDefinition("other", "Other B", 9d));
            var element = LevelElement();
            AssertReadOnlyRejection(project, element, "Project contains duplicate floor id: other.");
        }

        private static void RejectsNullFloorAtCatalogBoundary()
        {
            var project = CreateProjectWithLevels();
            var floorCount = project.Floors.Count;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            ThrowsArgumentNull(() => project.Floors.Add(null!));

            if (project.Floors.Count != floorCount ||
                project.ChangeVersion != changeVersion ||
                project.UpdatedUtc != updatedUtc)
                throw new InvalidOperationException("Null Floor structural rejection must remain read-only.");
        }

        private static void AssertReadOnlyRejection(ProjectState project, ProjectElement element, string expectedMessage)
        {
            var floorCount = project.Floors.Count;
            var propertyCount = element.Properties.Count;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            ThrowsExpected(() => ElementVerticalPlacementService.Resolve(project, element, 10d, 4d, 0d), expectedMessage);
            ThrowsExpected(() => ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, 4d), expectedMessage);

            if (project.Floors.Count != floorCount ||
                element.Properties.Count != propertyCount ||
                project.ChangeVersion != changeVersion ||
                project.UpdatedUtc != updatedUtc)
                throw new InvalidOperationException("Vertical placement Floor identity rejection must remain read-only.");
        }

        private static void PreservesValidFloorPlacement()
        {
            var project = CreateProjectWithLevels();
            var element = LevelElement();
            var placement = ElementVerticalPlacementService.Resolve(project, element, 10d, 4d, 0d);
            if (!placement.UsesBottomLevel || !placement.UsesTopLevel ||
                placement.BottomElevationM != 0d || placement.TopElevationM != 3d || placement.HeightM != 3d)
                throw new InvalidOperationException("Valid Floor-based vertical placement changed behavior.");
            if (ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, 4d) != 3d)
                throw new InvalidOperationException("Valid Floor-based effective height changed behavior.");
        }

        private static void PreservesLegacyFallbackWithoutLevelMetadata()
        {
            var project = CreateProjectWithLevels();
            project.Floors.Add(new FloorDefinition("OTHER", "Other A", 6d));
            project.Floors.Add(new FloorDefinition("other", "Other B", 9d));
            var element = new ProjectElement("LEGACY", ElementCategory.Beam);
            if (ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, 2.5d) != 2.5d)
                throw new InvalidOperationException("Legacy height fallback must not require Floor collection preflight without level metadata.");
            var placement = ElementVerticalPlacementService.Resolve(project, element, 1d, 2.5d, 0.5d);
            if (placement.UsesBottomLevel || placement.UsesTopLevel || placement.BottomElevationM != 1.5d || placement.TopElevationM != 4d)
                throw new InvalidOperationException("Legacy vertical placement fallback changed behavior.");
        }

        private static ProjectState CreateProjectWithLevels()
        {
            var project = new ProjectState("VERTICAL-FLOOR-INTEGRITY", "Vertical Floor integrity");
            project.Floors.Add(new FloorDefinition("BOTTOM", "Bottom", 0d));
            project.Floors.Add(new FloorDefinition("TOP", "Top", 3d));
            return project;
        }

        private static ProjectElement LevelElement()
        {
            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "BOTTOM";
            element.Properties[ProjectFloorService.TopLevelIdKey] = "TOP";
            return element;
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
                    throw new InvalidOperationException("Unexpected vertical placement Floor identity error.", ex);
                return;
            }
            throw new InvalidOperationException("Expected vertical placement to reject malformed Floor identity state.");
        }

        private static void ThrowsArgumentNull(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentNullException ex)
            {
                if (!string.Equals(ex.ParamName, "item", StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected null Floor rejection parameter.", ex);
                return;
            }
            throw new InvalidOperationException("Expected the Floor catalog boundary to reject a null entry.");
        }
    }
}
