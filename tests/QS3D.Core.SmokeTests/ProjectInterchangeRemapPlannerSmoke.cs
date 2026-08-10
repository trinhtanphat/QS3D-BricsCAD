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
