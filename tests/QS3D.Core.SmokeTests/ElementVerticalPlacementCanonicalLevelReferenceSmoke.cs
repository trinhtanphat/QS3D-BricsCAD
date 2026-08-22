using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementVerticalPlacementCanonicalLevelReferenceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPaddedBottomReference();
            RejectsWhitespaceOnlyBottomReference();
            RejectsPaddedTopReference();
            PreservesCanonicalReferences();
            PreservesExactEmptyLegacyFallback();
        }

        private static void RejectsPaddedBottomReference()
        {
            var project = CreateProject();
            var element = LevelElement(" BOTTOM ", "TOP");
            AssertReadOnlyRejection(project, element, ProjectFloorService.BottomLevelIdKey);
        }

        private static void RejectsWhitespaceOnlyBottomReference()
        {
            var project = CreateProject();
            var element = LevelElement("   ", string.Empty);
            AssertReadOnlyRejection(project, element, ProjectFloorService.BottomLevelIdKey);
        }

        private static void RejectsPaddedTopReference()
        {
            var project = CreateProject();
            var element = LevelElement("BOTTOM", " TOP ");
            AssertReadOnlyRejection(project, element, ProjectFloorService.TopLevelIdKey);
        }

        private static void AssertReadOnlyRejection(ProjectState project, ProjectElement element, string key)
        {
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;
            var expectedMessage = element.Id + "/" + key + " must use a canonical Floor/Level reference without surrounding whitespace.";

            ThrowsExpected(() => ElementVerticalPlacementService.Resolve(project, element, 0d, 4d, 0d), expectedMessage);
            ThrowsExpected(() => ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, 4d), expectedMessage);

            if (project.ChangeVersion != changeVersion || project.UpdatedUtc != updatedUtc)
                throw new InvalidOperationException("Canonical Level reference rejection must remain read-only.");
        }

        private static void PreservesCanonicalReferences()
        {
            var project = CreateProject();
            var element = LevelElement("BOTTOM", "TOP");
            var placement = ElementVerticalPlacementService.Resolve(project, element, 10d, 4d, 0d);
            if (!placement.UsesBottomLevel || !placement.UsesTopLevel ||
                placement.BottomElevationM != 0d || placement.TopElevationM != 3d || placement.HeightM != 3d)
                throw new InvalidOperationException("Canonical Level reference placement changed behavior.");
            if (ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, 4d) != 3d)
                throw new InvalidOperationException("Canonical Level reference effective height changed behavior.");
        }

        private static void PreservesExactEmptyLegacyFallback()
        {
            var project = CreateProject();
            var element = LevelElement(string.Empty, string.Empty);
            var placement = ElementVerticalPlacementService.Resolve(project, element, 1d, 2.5d, 0.5d);
            if (placement.UsesBottomLevel || placement.UsesTopLevel ||
                placement.BottomElevationM != 1.5d || placement.TopElevationM != 4d)
                throw new InvalidOperationException("Exact empty Level references must preserve legacy placement fallback.");
            if (ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, 2.5d) != 2.5d)
                throw new InvalidOperationException("Exact empty Level references must preserve legacy effective height fallback.");
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("VERTICAL-CANONICAL-LEVELS", "Vertical canonical levels");
            project.Floors.Add(new FloorDefinition("BOTTOM", "Bottom", 0d));
            project.Floors.Add(new FloorDefinition("TOP", "Top", 3d));
            return project;
        }

        private static ProjectElement LevelElement(string bottom, string top)
        {
            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = bottom;
            element.Properties[ProjectFloorService.TopLevelIdKey] = top;
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
                    throw new InvalidOperationException("Unexpected canonical Level reference error.", ex);
                return;
            }
            throw new InvalidOperationException("Expected non-canonical Level reference to be rejected.");
        }
    }
}
