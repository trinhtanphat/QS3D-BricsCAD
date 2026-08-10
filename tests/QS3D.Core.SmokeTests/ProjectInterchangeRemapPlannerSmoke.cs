using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeRemapPlannerSmoke
    {
        internal static void Run()
        {
            FamilyNameCollisionIsScopedByCategory();
            SameCategoryFamilyNameCollisionIsRenamed();
            FamilyOpaqueReferenceBlocksPreview();
            ElementOpaqueReferenceBlocksPreviewEvenForExternalValue();
            PortableLevelReferencesAreTypedAndRemapped();
            RegisteredReferenceMissingFromSourceBlocksPreview();
        }

        private static void FamilyNameCollisionIsScopedByCategory()
        {
            var target = NewProject("target", ElementCategory.Column, "TARGET-FAM", "Shared Family", "TARGET-ELEM");
            var source = NewProject("source", ElementCategory.Beam, "SOURCE-FAM", "Shared Family", "SOURCE-ELEM");

            var plan = ProjectInterchangeRemapPlanner.Plan(target, ProjectInterchangeJsonExporter.Build(source));
            var family = plan.Items.Single(x => x.Kind == InterchangeRemapIdentityKind.Family && x.SourceId == "SOURCE-FAM");

            False(family.NameChanged);
            Equal("Shared Family", family.TargetName);
        }

        private static void SameCategoryFamilyNameCollisionIsRenamed()
        {
            var target = NewProject("target", ElementCategory.Beam, "TARGET-FAM", "Shared Family", "TARGET-ELEM");
            var source = NewProject("source", ElementCategory.Beam, "SOURCE-FAM", "Shared Family", "SOURCE-ELEM");

            var plan = ProjectInterchangeRemapPlanner.Plan(target, ProjectInterchangeJsonExporter.Build(source));
            var family = plan.Items.Single(x => x.Kind == InterchangeRemapIdentityKind.Family && x.SourceId == "SOURCE-FAM");

            True(family.NameChanged);
            Equal("Shared Family (Imported)", family.TargetName);
        }

        private static void FamilyOpaqueReferenceBlocksPreview()
        {
            var target = NewProject("target", ElementCategory.Column, "TARGET-FAM", "Target Family", "TARGET-ELEM");
            var source = NewProject("source", ElementCategory.Beam, "SOURCE-FAM", "Source Family", "SOURCE-ELEM");
            source.Families.Single().Properties["CatalogRefIds"] = "EXTERNAL-CATALOG-42";

            var plan = ProjectInterchangeRemapPlanner.Plan(target, ProjectInterchangeJsonExporter.Build(source));

            False(plan.CanAppendAsNew);
            var warning = plan.OpaqueReferenceWarnings.Single(x => x.PropertyKey == "CatalogRefIds");
            Equal("Family SOURCE-FAM", warning.OwnerElementSourceId);
        }

        private static void ElementOpaqueReferenceBlocksPreviewEvenForExternalValue()
        {
            var target = NewProject("target", ElementCategory.Column, "TARGET-FAM", "Target Family", "TARGET-ELEM");
            var source = NewProject("source", ElementCategory.Beam, "SOURCE-FAM", "Source Family", "SOURCE-ELEM");
            source.Elements.Single().Properties["ExternalRefIds"] = "NOT-A-SOURCE-ELEMENT";

            var plan = ProjectInterchangeRemapPlanner.Plan(target, ProjectInterchangeJsonExporter.Build(source));

            False(plan.CanAppendAsNew);
            var warning = plan.OpaqueReferenceWarnings.Single(x => x.PropertyKey == "ExternalRefIds");
            Equal("SOURCE-ELEM", warning.OwnerElementSourceId);
        }

        private static void PortableLevelReferencesAreTypedAndRemapped()
        {
            var target = NewProject("target", ElementCategory.Beam, "TARGET-FAM", "Target Family", "TARGET-ELEM");
            target.Floors.Add(new FloorDefinition("L0", "Existing L0", 0d));
            target.Floors.Add(new FloorDefinition("L1", "Existing L1", 3.6d));

            var source = NewProject("source", ElementCategory.Beam, "SOURCE-FAM", "Source Family", "SOURCE-ELEM");
            source.Floors.Add(new FloorDefinition("L0", "Source L0", 0d));
            source.Floors.Add(new FloorDefinition("L1", "Source L1", 3.6d));
            var element = source.Elements.Single();
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "L0";
            element.Properties[ProjectFloorService.TopLevelIdKey] = "L1";

            var plan = ProjectInterchangeRemapPlanner.Plan(target, ProjectInterchangeJsonExporter.Build(source));

            True(plan.CanAppendAsNew);
            var bottom = plan.ReferenceRewrites.Single(x => x.OwnerElementSourceId == "SOURCE-ELEM" && x.PropertyKey == ProjectFloorService.BottomLevelIdKey);
            var top = plan.ReferenceRewrites.Single(x => x.OwnerElementSourceId == "SOURCE-ELEM" && x.PropertyKey == ProjectFloorService.TopLevelIdKey);
            Equal("L0-import", bottom.TargetReferenceId);
            Equal("L1-import", top.TargetReferenceId);
            Equal("PropertyFloorId", bottom.ReferenceKind);
            Equal("PropertyFloorId", top.ReferenceKind);
        }

        private static void RegisteredReferenceMissingFromSourceBlocksPreview()
        {
            var target = NewProject("target", ElementCategory.Beam, "TARGET-FAM", "Target Family", "TARGET-ELEM");
            var source = NewProject("source", ElementCategory.Beam, "SOURCE-FAM", "Source Family", "SOURCE-ELEM");
            source.Elements.Single().Properties[ProjectFloorService.BottomLevelIdKey] = "MISSING-LEVEL";

            var plan = ProjectInterchangeRemapPlanner.Plan(target, ProjectInterchangeJsonExporter.Build(source));

            False(plan.CanAppendAsNew);
            var warning = plan.OpaqueReferenceWarnings.Single(x => x.PropertyKey == ProjectFloorService.BottomLevelIdKey);
            True(warning.Reason.IndexOf("does not resolve inside the source snapshot", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static ProjectState NewProject(
            string id,
            ElementCategory category,
            string familyId,
            string familyName,
            string elementId)
        {
            var project = new ProjectState(id, "Project " + id);
            project.Zones.Add(new ZoneDefinition(id + "-zone", "Zone " + id));
            project.Floors.Add(new FloorDefinition(id + "-floor", "Floor " + id, 0d));
            project.Families.Add(new ProjectFamily(familyId, familyName, category));
            project.Elements.Add(new ProjectElement(elementId, category, familyId, id + "-floor", id + "-zone"));
            return project;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected condition to be false.");
        }
    }

    internal static class ProjectInterchangeRemapPlannerSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeRemapPlannerSmoke.Run();
    }
}
