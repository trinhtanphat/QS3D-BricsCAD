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
            BlockedAppendPlanRemainsInspectableAndImportFailsClosed();
            OverLimitCatalogIdentitiesAreBoundedBeforeImport();
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

        private static void BlockedAppendPlanRemainsInspectableAndImportFailsClosed()
        {
            var target = NewProject("target", ElementCategory.Column, "TARGET-FAM", "Target Family", "TARGET-ELEM");
            var source = NewProject("source", ElementCategory.Beam, "SOURCE-FAM", "Source Family", "SOURCE-ELEM");
            source.Elements.Single().Properties["ExternalRefIds"] = "NOT-A-SOURCE-ELEMENT";
            var json = ProjectInterchangeJsonExporter.Build(source);
            var zones = target.Zones.Count;
            var floors = target.Floors.Count;
            var families = target.Families.Count;
            var elements = target.Elements.Count;

            var plan = ProjectInterchangeRemapAppendImporter.Plan(target, json);

            False(plan.CanImport);
            True(plan.Remap.OpaqueReferenceWarnings.Any(x => x.PropertyKey == "ExternalRefIds"));
            Throws<InvalidOperationException>(() => ProjectInterchangeRemapAppendImporter.Import(target, json));
            Equal(zones, target.Zones.Count);
            Equal(floors, target.Floors.Count);
            Equal(families, target.Families.Count);
            Equal(elements, target.Elements.Count);
        }

        private static void OverLimitCatalogIdentitiesAreBoundedBeforeImport()
        {
            var target = NewProject("target", ElementCategory.Column, "TARGET-FAM", "Target Family", "TARGET-ELEM");
            var source = new ProjectState("source", "Source");
            var zoneId = new string('Z', 70);
            var zoneName = new string('N', 130);
            var floorId = new string('F', 70);
            var floorName = new string('L', 130);
            var familyId = new string('A', 90);
            var familyName = new string('B', 170);
            source.Zones.Add(new ZoneDefinition(zoneId, zoneName));
            source.Floors.Add(new FloorDefinition(floorId, floorName, 3d));
            source.Families.Add(new ProjectFamily(familyId, familyName, ElementCategory.Beam));
            source.Elements.Add(new ProjectElement("SOURCE-ELEM", ElementCategory.Beam, familyId, floorId, zoneId));
            var json = ProjectInterchangeJsonExporter.Build(source);

            var plan = ProjectInterchangeRemapAppendImporter.Plan(target, json);
            True(plan.CanImport);
            var zone = plan.Remap.Items.Single(x => x.Kind == InterchangeRemapIdentityKind.Zone);
            var floor = plan.Remap.Items.Single(x => x.Kind == InterchangeRemapIdentityKind.Floor);
            var family = plan.Remap.Items.Single(x => x.Kind == InterchangeRemapIdentityKind.Family);
            True(zone.IdChanged && zone.NameChanged);
            True(floor.IdChanged && floor.NameChanged);
            True(family.IdChanged && family.NameChanged);
            True(zone.TargetId.Length <= 64 && zone.TargetName.Length <= 120);
            True(floor.TargetId.Length <= 64 && floor.TargetName.Length <= 120);
            True(family.TargetId.Length <= 80 && family.TargetName.Length <= 160);

            var result = ProjectInterchangeRemapAppendImporter.Import(target, json);
            Equal(1, result.ZonesAdded);
            Equal(1, result.FloorsAdded);
            Equal(1, result.FamiliesAdded);
            Equal(1, result.ElementsAdded);
            True(target.FindZone(zone.TargetId) != null);
            True(target.FindFloor(floor.TargetId) != null);
            True(target.FindFamily(family.TargetId) != null);
            var imported = target.FindElement("SOURCE-ELEM");
            True(imported != null);
            Equal(zone.TargetId, imported!.ZoneId);
            Equal(floor.TargetId, imported.FloorId);
            Equal(family.TargetId, imported.FamilyId);
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
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class ProjectInterchangeRemapPlannerSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeRemapPlannerSmoke.Run();
    }
}
