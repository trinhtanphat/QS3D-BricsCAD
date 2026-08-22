using System;
using System.IO;
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
            IncomingDuplicateNamesAreRemappedWithinBatch();
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

        private static void IncomingDuplicateNamesAreRemappedWithinBatch()
        {
            var target = NewProject("target", ElementCategory.Column, "TARGET-FAM", "Target Family", "TARGET-ELEM");
            var source = new ProjectState("source", "Source");
            source.Zones.Add(new ZoneDefinition("ZA", "Shared Zone"));
            source.Zones.Add(new ZoneDefinition("ZB", "Shared Zone"));
            source.Floors.Add(new FloorDefinition("FA", "Shared Floor", 0d));
            source.Floors.Add(new FloorDefinition("FB", "Shared Floor", 3d));
            source.Families.Add(new ProjectFamily("FAMA", "Shared Family", ElementCategory.Beam));
            source.Families.Add(new ProjectFamily("FAMB", "Shared Family", ElementCategory.Beam));
            var json = ProjectInterchangeJsonExporter.Build(source);
            var plan = ProjectInterchangeRemapAppendImporter.Plan(target, json);
            True(plan.CanImport);
            var zones = plan.Remap.Items.Where(x => x.Kind == InterchangeRemapIdentityKind.Zone).OrderBy(x => x.SourceId).ToList();
            var floors = plan.Remap.Items.Where(x => x.Kind == InterchangeRemapIdentityKind.Floor).OrderBy(x => x.SourceId).ToList();
            var families = plan.Remap.Items.Where(x => x.Kind == InterchangeRemapIdentityKind.Family).OrderBy(x => x.SourceId).ToList();
            False(zones[0].NameChanged);
            True(zones[1].NameChanged);
            False(floors[0].NameChanged);
            True(floors[1].NameChanged);
            False(families[0].NameChanged);
            True(families[1].NameChanged);
            True(!string.Equals(zones[0].TargetName, zones[1].TargetName, StringComparison.OrdinalIgnoreCase));
            True(!string.Equals(floors[0].TargetName, floors[1].TargetName, StringComparison.OrdinalIgnoreCase));
            True(!string.Equals(families[0].TargetName, families[1].TargetName, StringComparison.OrdinalIgnoreCase));
            var result = ProjectInterchangeRemapAppendImporter.Import(target, json);
            Equal(2, result.ZonesAdded);
            Equal(2, result.FloorsAdded);
            Equal(2, result.FamiliesAdded);
            Equal(0, result.ElementsAdded);
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
            source.Elements.Single().Properties[ProjectFloorService.BottomLevelIdKey] = "source-floor";
            var validJson = ProjectInterchangeJsonExporter.Build(source);
            var invalidJson = validJson.Replace(
                "\"BottomLevelId\":\"source-floor\"",
                "\"BottomLevelId\":\"MISSING-LEVEL\"",
                StringComparison.Ordinal);
            True(!string.Equals(validJson, invalidJson, StringComparison.Ordinal));
            Throws<InvalidDataException>(() => ProjectInterchangeRemapPlanner.Plan(target, invalidJson));
        }

        private static ProjectState NewProject(string id, ElementCategory category, string familyId, string familyName, string elementId)
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
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class ProjectInterchangeRemapPlannerSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeRemapPlannerSmoke.Run();
    }
}
