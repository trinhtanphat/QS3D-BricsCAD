using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeKeepTargetImporterSmoke
    {
        internal static void Run()
        {
            PlanIsReadOnlyAndClassifiesAddVersusKeep();
            CaseInsensitiveCollisionsKeepTargetAndAddDistinctItems();
            ImportKeepsExistingAndAddsPortableState();
            NameAndCategoryConflictsFailBeforeMutation();
            InvalidSnapshotFailsBeforeMutation();
        }

        private static void CaseInsensitiveCollisionsKeepTargetAndAddDistinctItems()
        {
            var target = LowercaseIdentityTargetProject();
            var existingFamily = target.Families.Single();
            var existingElement = target.Elements.Single();

            var result = ProjectInterchangeKeepTargetImporter.Import(
                target,
                ProjectInterchangeJsonExporter.Build(SourceProject()));

            Equal(1, result.ZonesAdded);
            Equal(1, result.FloorsAdded);
            Equal(1, result.FamiliesAdded);
            Equal(1, result.ElementsAdded);
            Equal(4, result.TargetIdentitiesKept);
            Equal(2, target.Zones.Count);
            Equal(2, target.Floors.Count);
            Equal(2, target.Families.Count);
            Equal(2, target.Elements.Count);
            True(ReferenceEquals(existingFamily, target.Families.Single(x => x.Id == "fam1")));
            True(ReferenceEquals(existingElement, target.Elements.Single(x => x.Id == "e1")));
            Equal("z1", target.ActiveZoneId);
            Equal("f1", target.ActiveFloorId);
            Equal("fam1", target.Metadata["ActiveFamilyId"]);
            True(target.FindElement("E2") != null);
        }

        private static void PlanIsReadOnlyAndClassifiesAddVersusKeep()
        {
            var target = TargetProject();
            var json = ProjectInterchangeJsonExporter.Build(SourceProject());
            var updated = new DateTime(2026, 8, 10, 15, 40, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            var zones = target.Zones.Count;
            var floors = target.Floors.Count;
            var families = target.Families.Count;
            var elements = target.Elements.Count;
            var metadata = target.Metadata.Count;
            var audits = target.AuditEvents.Count;

            var plan = ProjectInterchangeKeepTargetImporter.Plan(target, json);

            Equal("SOURCE-P", plan.SourceProjectId);
            Equal(1, plan.ZonesToAdd);
            Equal(1, plan.FloorsToAdd);
            Equal(1, plan.FamiliesToAdd);
            Equal(1, plan.ElementsToAdd);
            Equal(1, plan.ZonesToKeep);
            Equal(1, plan.FloorsToKeep);
            Equal(1, plan.FamiliesToKeep);
            Equal(1, plan.ElementsToKeep);
            Equal(4, plan.TotalSemanticIdentitiesToAdd);
            Equal(4, plan.TotalSemanticIdentitiesToKeep);
            Equal(2, plan.SourceHandlesToDiscard);
            Equal(zones, target.Zones.Count);
            Equal(floors, target.Floors.Count);
            Equal(families, target.Families.Count);
            Equal(elements, target.Elements.Count);
            Equal(metadata, target.Metadata.Count);
            Equal(audits, target.AuditEvents.Count);
            Equal(updated, target.UpdatedUtc);
        }

        private static void ImportKeepsExistingAndAddsPortableState()
        {
            var target = TargetProject();
            var existingFamily = target.Families.Single();
            var existingElement = target.Elements.Single();
            var originalProjectId = target.ProjectId;
            var originalName = target.Name;
            var originalDrawingPath = target.DrawingPath;
            var originalDrawingFingerprint = target.DrawingFingerprint;
            var originalActiveZone = target.ActiveZoneId;
            var originalActiveFloor = target.ActiveFloorId;
            var originalActiveFamily = target.Metadata["ActiveFamilyId"];
            var originalExistingMark = existingElement.Properties["Mark"];
            var originalExistingLength = existingElement.Quantities["LengthM"];
            var originalExistingFamilyMaterial = existingFamily.Properties["Material"];

            var source = SourceProject();
            var result = ProjectInterchangeKeepTargetImporter.Import(target, ProjectInterchangeJsonExporter.Build(source));

            Equal("SOURCE-P", result.SourceProjectId);
            Equal(1, result.ZonesAdded);
            Equal(1, result.FloorsAdded);
            Equal(1, result.FamiliesAdded);
            Equal(1, result.ElementsAdded);
            Equal(4, result.TargetIdentitiesKept);
            Equal(2, result.SourceHandlesDiscarded);

            Equal(originalProjectId, target.ProjectId);
            Equal(originalName, target.Name);
            Equal(originalDrawingPath, target.DrawingPath);
            Equal(originalDrawingFingerprint, target.DrawingFingerprint);
            Equal(originalActiveZone, target.ActiveZoneId);
            Equal(originalActiveFloor, target.ActiveFloorId);
            Equal(originalActiveFamily, target.Metadata["ActiveFamilyId"]);

            var keptZone = target.FindZone("Z1") ?? throw new Exception("Kept target Zone missing.");
            var keptFloor = target.FindFloor("F1") ?? throw new Exception("Kept target Floor missing.");
            var keptFamily = target.Families.Single(x => x.Id == "FAM1");
            var keptElement = target.FindElement("E1") ?? throw new Exception("Kept target element missing.");
            Equal("Target Zone", keptZone.Name);
            Equal("Target Floor", keptFloor.Name);
            Near(0d, keptFloor.ElevationM);
            True(ReferenceEquals(existingFamily, keptFamily));
            True(ReferenceEquals(existingElement, keptElement));
            Equal(originalExistingFamilyMaterial, keptFamily.Properties["Material"]);
            Equal(originalExistingMark, keptElement.Properties["Mark"]);
            Near(originalExistingLength, keptElement.Quantities["LengthM"]);

            Equal(2, target.Zones.Count);
            Equal(2, target.Floors.Count);
            Equal(2, target.Families.Count);
            Equal(2, target.Elements.Count);
            var imported = target.FindElement("E2") ?? throw new Exception("New source element was not imported.");
            Equal("FAM2", imported.FamilyId);
            Equal("F2", imported.FloorId);
            Equal("Z2", imported.ZoneId);
            Equal(1, imported.DependsOn.Count);
            Equal("E1", imported.DependsOn[0]);
            Equal("B-02", imported.Properties["Mark"]);
            Near(6.25d, imported.Quantities["LengthM"]);
            Equal(ElementDirtyFlags.All, imported.Dirty);
            Equal(string.Empty, imported.DrawingFingerprint);
            Equal(0, imported.SourceHandles.Count);

            Equal(ProjectInterchangeKeepTargetImporter.ImportMode, target.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey]);
            Equal("4", target.Metadata[ProjectInterchangeKeepTargetImporter.LastSemanticIdentitiesAddedKey]);
            Equal("4", target.Metadata[ProjectInterchangeKeepTargetImporter.LastTargetIdentitiesKeptKey]);
            Equal("2", target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceHandlesDiscardedKey]);
            Equal("ImportInterchangeKeepTarget", target.AuditEvents.Last().Action);
        }

        private static void NameAndCategoryConflictsFailBeforeMutation()
        {
            var target = TargetProject();
            var source = SourceProject();
            source.Zones.Add(new ZoneDefinition("Z3", "Target Zone"));
            AssertImportFailsWithoutMutation(target, ProjectInterchangeJsonExporter.Build(source));

            target = TargetProject();
            source = SourceProject();
            source.Families.Remove(source.Families.Single(x => x.Id == "FAM1"));
            source.Families.Add(new ProjectFamily("FAM1", "Source Column Family", ElementCategory.Column));
            source.Elements.Remove(source.Elements.Single(x => x.Id == "E1"));
            source.Elements.Add(new ProjectElement("E1", ElementCategory.Column, "FAM1", "F1", "Z1"));
            AssertImportFailsWithoutMutation(target, ProjectInterchangeJsonExporter.Build(source));
        }

        private static void InvalidSnapshotFailsBeforeMutation()
        {
            var target = TargetProject();
            var updated = new DateTime(2026, 8, 10, 15, 45, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            var elements = target.Elements.Count;

            Throws<InvalidDataException>(() => ProjectInterchangeKeepTargetImporter.Import(target, "{\"format\":\"Wrong\"}"));

            Equal(elements, target.Elements.Count);
            Equal(updated, target.UpdatedUtc);
            True(!target.Metadata.ContainsKey(ProjectInterchangeAppendOnlyImporter.LastModeKey));
        }

        private static void AssertImportFailsWithoutMutation(ProjectState target, string json)
        {
            var zones = target.Zones.Count;
            var floors = target.Floors.Count;
            var families = target.Families.Count;
            var elements = target.Elements.Count;
            var metadata = target.Metadata.Count;
            var audits = target.AuditEvents.Count;
            var updated = new DateTime(2026, 8, 10, 15, 50, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;

            Throws<InvalidOperationException>(() => ProjectInterchangeKeepTargetImporter.Import(target, json));

            Equal(zones, target.Zones.Count);
            Equal(floors, target.Floors.Count);
            Equal(families, target.Families.Count);
            Equal(elements, target.Elements.Count);
            Equal(metadata, target.Metadata.Count);
            Equal(audits, target.AuditEvents.Count);
            Equal(updated, target.UpdatedUtc);
            True(!target.Metadata.ContainsKey(ProjectInterchangeAppendOnlyImporter.LastModeKey));
        }

        private static ProjectState TargetProject()
        {
            var project = new ProjectState("TARGET-P", "Target Project")
            {
                DrawingPath = "target.dwg",
                DrawingFingerprint = "target-fingerprint",
                ActiveZoneId = "Z1",
                ActiveFloorId = "F1"
            };
            project.Zones.Add(new ZoneDefinition("Z1", "Target Zone"));
            project.Floors.Add(new FloorDefinition("F1", "Target Floor", 0d));
            var family = new ProjectFamily("FAM1", "Target Beam Family", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            project.Families.Add(family);
            project.Metadata["ActiveFamilyId"] = family.Id;
            var element = new ProjectElement("E1", ElementCategory.Beam, family.Id, "F1", "Z1");
            element.Properties["Mark"] = "TARGET-B-01";
            element.Quantities["LengthM"] = 4d;
            project.Elements.Add(element);
            return project;
        }

        private static ProjectState LowercaseIdentityTargetProject()
        {
            var project = new ProjectState("TARGET-P", "Target Project")
            {
                DrawingPath = "target.dwg",
                DrawingFingerprint = "target-fingerprint",
                ActiveZoneId = "z1",
                ActiveFloorId = "f1"
            };
            project.Zones.Add(new ZoneDefinition("z1", "Target Zone"));
            project.Floors.Add(new FloorDefinition("f1", "Target Floor", 0d));
            var family = new ProjectFamily("fam1", "Target Beam Family", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            project.Families.Add(family);
            project.Metadata["ActiveFamilyId"] = family.Id;
            var element = new ProjectElement("e1", ElementCategory.Beam, family.Id, "f1", "z1");
            element.Properties["Mark"] = "TARGET-B-01";
            element.Quantities["LengthM"] = 4d;
            project.Elements.Add(element);
            return project;
        }

        private static ProjectState SourceProject()
        {
            var project = new ProjectState("SOURCE-P", "Source Project")
            {
                DrawingFingerprint = "source-fingerprint",
                UpdatedUtc = new DateTime(2026, 8, 10, 15, 30, 0, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition("Z1", "Source Zone Same Id"));
            project.Zones.Add(new ZoneDefinition("Z2", "Source Zone 2"));
            project.Floors.Add(new FloorDefinition("F1", "Source Floor Same Id", 9d));
            project.Floors.Add(new FloorDefinition("F2", "Source Floor 2", 3.5d));

            var existingFamily = new ProjectFamily("FAM1", "Source Beam Same Id", ElementCategory.Beam);
            existingFamily.Properties["Material"] = "SOURCE-C99";
            project.Families.Add(existingFamily);
            var newFamily = new ProjectFamily("FAM2", "Source Beam 2", ElementCategory.Beam);
            newFamily.Properties["Material"] = "C40";
            project.Families.Add(newFamily);

            var existingElement = new ProjectElement("E1", ElementCategory.Beam, "FAM1", "F1", "Z1")
            {
                DrawingFingerprint = project.DrawingFingerprint
            };
            existingElement.SourceHandles.Add("1A2B");
            existingElement.Properties["Mark"] = "SOURCE-B-01";
            existingElement.Quantities["LengthM"] = 99d;
            project.Elements.Add(existingElement);

            var newElement = new ProjectElement("E2", ElementCategory.Beam, "FAM2", "F2", "Z2")
            {
                DrawingFingerprint = project.DrawingFingerprint
            };
            newElement.SourceHandles.Add("2B3C");
            newElement.DependsOn.Add("E1");
            newElement.Properties["Mark"] = "B-02";
            newElement.Quantities["LengthM"] = 6.25d;
            project.Elements.Add(newElement);
            return project;
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class ProjectInterchangeKeepTargetImporterSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeKeepTargetImporterSmoke.Run();
    }
}
