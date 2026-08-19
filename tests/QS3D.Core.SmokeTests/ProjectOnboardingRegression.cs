using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectOnboardingRegression
    {
        public static void Run()
        {
            UnitGateIsNonDestructive();
            MaterialGateIsNonDestructive();
            FreshProjectCreatesStarterRoute();
            ExistingFamilyIsPreserved();
            RepeatIsIdempotent();
        }

        private static void UnitGateIsNonDestructive()
        {
            var project = new ProjectState("P-ONBOARD-UNIT", "unit gate");
            var result = ProjectOnboardingService.Bootstrap(project, new ProjectOnboardingRequest(null, null, Materials()));
            Equal(ProjectOnboardingStatus.NeedsUnitConfirmation, result.Status, "Unresolved units must fail closed.");
            Equal(0, project.Floors.Count, "Unit gate must not create Floor.");
            Equal(0, project.Families.Count, "Unit gate must not create Families.");
            Equal(0, project.Metadata.Count, "Unit gate must not persist assumptions.");
        }

        private static void MaterialGateIsNonDestructive()
        {
            var project = new ProjectState("P-ONBOARD-MAT", "material gate");
            var result = ProjectOnboardingService.Bootstrap(project, new ProjectOnboardingRequest(null, LengthUnit.Millimeter, new Dictionary<ElementCategory, string>()));
            Equal(ProjectOnboardingStatus.NeedsMaterialConfirmation, result.Status, "Fresh catalog needs explicit materials.");
            Equal(6, result.MissingMaterialCategories.Count, "All starter categories should be reported.");
            Equal(0, project.Floors.Count, "Material gate must not create Floor.");
            Equal(0, project.Families.Count, "Material gate must not create Families.");
            False(project.Metadata.ContainsKey(DrawingUnitResolutionPolicy.OverrideMetadataKey), "Material gate must not persist unit override early.");
        }

        private static void FreshProjectCreatesStarterRoute()
        {
            var project = new ProjectState("P-ONBOARD-FRESH", "fresh project");
            var materials = Materials();
            var result = ProjectOnboardingService.Bootstrap(project, new ProjectOnboardingRequest(null, LengthUnit.Millimeter, materials));
            Equal(ProjectOnboardingStatus.ReadyForFirstObject, result.Status, "Fresh project should become ready.");
            Equal(1, project.Floors.Count, "Fresh project needs one starter Floor.");
            Equal(ProjectOnboardingService.StarterFloorId, project.ActiveFloorId, "Starter Floor should be active.");
            Equal(6, project.Families.Count, "Fresh project needs six starter Families.");
            Equal(6, result.CreatedFamilyIds.Count, "Created Family result count mismatch.");
            Equal("Tạo mới", result.NextAuthoringAction, "Authoring route mismatch.");
            Equal("Khối lượng", result.NextQuantityAction, "Quantity route mismatch.");
            Equal("Millimeter", project.Metadata[DrawingUnitResolutionPolicy.OverrideMetadataKey], "Confirmed unit must persist explicitly.");
            foreach (var category in ProjectOnboardingService.StarterCategories)
            {
                var family = project.Families.Single(x => x.Category == category);
                Equal(materials[category], family.Properties["Material"], "Starter material must be explicit for " + category + ".");
            }
        }

        private static void ExistingFamilyIsPreserved()
        {
            var project = new ProjectState("P-ONBOARD-EXIST", "existing project");
            ProjectFloorService.Create(project, "existing-floor", "Ground", 0d);
            var beam = ProjectFamilyService.Create(project, "existing-beam", "Custom Beam", ElementCategory.Beam);
            ProjectFamilyService.SetProperty(project, beam.Id, "WidthM", "0.3");
            ProjectFamilyService.SetProperty(project, beam.Id, "HeightM", "0.5");
            ProjectFamilyService.SetProperty(project, beam.Id, "BottomOffsetM", "0");
            ProjectFamilyService.SetProperty(project, beam.Id, "Material", "Concrete C30");
            ProjectFamilyService.SetProperty(project, beam.Id, "CatalogMarker", "KEEP");
            var materials = Materials();
            materials[ElementCategory.Beam] = "Concrete C30";
            var result = ProjectOnboardingService.Bootstrap(project, new ProjectOnboardingRequest(LengthUnit.Millimeter, null, materials));
            Equal(ProjectOnboardingStatus.ReadyForFirstObject, result.Status, "Existing project should be ready.");
            Equal(1, project.Floors.Count, "Existing Floor must not be replaced.");
            Equal("existing-floor", project.Floors[0].Id, "Existing Floor id changed.");
            Equal(1, project.Families.Count(x => x.Category == ElementCategory.Beam), "Compatible Beam must not be duplicated.");
            Equal("KEEP", beam.Properties["CatalogMarker"], "Existing Family data was overwritten.");
            True(result.ReusedFamilyIds.Contains(beam.Id), "Existing compatible Family should be reported as reused.");
        }

        private static void RepeatIsIdempotent()
        {
            var project = new ProjectState("P-ONBOARD-REPEAT", "repeat project");
            var request = new ProjectOnboardingRequest(null, LengthUnit.Millimeter, Materials());
            ProjectOnboardingService.Bootstrap(project, request);
            var ids = project.Families.Select(x => x.Id).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            var second = ProjectOnboardingService.Bootstrap(project, request);
            Equal(1, project.Floors.Count, "Repeat must not duplicate Floor.");
            True(ids.SequenceEqual(project.Families.Select(x => x.Id).OrderBy(x => x, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase), "Repeat must not duplicate Families.");
            Equal(0, second.CreatedFamilyIds.Count, "Repeat should create no Families.");
            Equal(6, second.ReusedFamilyIds.Count, "Repeat should reuse all starter Families.");
        }

        private static Dictionary<ElementCategory, string> Materials()
        {
            return new Dictionary<ElementCategory, string>
            {
                [ElementCategory.ArchitecturalWall] = "Masonry",
                [ElementCategory.Beam] = "Concrete C30",
                [ElementCategory.Column] = "Concrete C30",
                [ElementCategory.Slab] = "Concrete C30",
                [ElementCategory.StructuralWall] = "Concrete C30",
                [ElementCategory.Foundation] = "Concrete C30"
            };
        }

        private static void True(bool value, string message) { if (!value) throw new Exception(message); }
        private static void False(bool value, string message) { if (value) throw new Exception(message); }
        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual)) throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
