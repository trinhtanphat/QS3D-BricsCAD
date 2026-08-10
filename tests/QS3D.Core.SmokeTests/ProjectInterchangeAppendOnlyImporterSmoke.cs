using System;
using System.IO;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeAppendOnlyImporterSmoke
    {
        public static void Run()
        {
            ImportAppendsPortableStateAndDiscardsCadOwnership();
            AppendPlanIsReadOnlyAndRejectsNameCollision();
            CollisionFailsBeforeMutation();
            InvalidSnapshotFailsBeforeMutation();
            ApplyFailureRollsBackPartialMutation();
        }

        private static void ImportAppendsPortableStateAndDiscardsCadOwnership()
        {
            var target = TargetProject();
            var existing = target.Elements.Single();
            var originalProjectId = target.ProjectId;
            var originalName = target.Name;
            var originalDrawingPath = target.DrawingPath;
            var originalDrawingFingerprint = target.DrawingFingerprint;
            var originalActiveZone = target.ActiveZoneId;
            var originalActiveFloor = target.ActiveFloorId;
            var originalActiveFamily = target.Metadata["ActiveFamilyId"];

            var source = SourceProject();
            var json = ProjectInterchangeJsonExporter.Build(source);
            var result = ProjectInterchangeAppendOnlyImporter.Import(target, json);

            Equal(source.ProjectId, result.SourceProjectId);
            Equal(source.SchemaVersion, result.SourceSchemaVersion);
            Equal(source.DrawingFingerprint, result.SourceDrawingFingerprint);
            Equal(1, result.ZonesAdded);
            Equal(1, result.FloorsAdded);
            Equal(1, result.FamiliesAdded);
            Equal(2, result.ElementsAdded);
            Equal(2, result.SourceHandlesDiscarded);

            Equal(originalProjectId, target.ProjectId);
            Equal(originalName, target.Name);
            Equal(originalDrawingPath, target.DrawingPath);
            Equal(originalDrawingFingerprint, target.DrawingFingerprint);
            Equal(originalActiveZone, target.ActiveZoneId);
            Equal(originalActiveFloor, target.ActiveFloorId);
            Equal(originalActiveFamily, target.Metadata["ActiveFamilyId"]);
            True(ReferenceEquals(existing, target.FindElement(existing.Id)));

            Equal(2, target.Zones.Count);
            Equal(2, target.Floors.Count);
            Equal(2, target.Families.Count);
            Equal(3, target.Elements.Count);

            var importedBase = target.FindElement("SRC-E1") ?? throw new Exception("Imported base element missing.");
            var importedBeam = target.FindElement("SRC-E2") ?? throw new Exception("Imported dependent element missing.");
            Equal("SRC-FAM", importedBeam.FamilyId);
            Equal("SRC-FLOOR", importedBeam.FloorId);
            Equal("SRC-ZONE", importedBeam.ZoneId);
            Equal(1, importedBeam.DependsOn.Count);
            Equal(importedBase.Id, importedBeam.DependsOn[0]);
            Equal("B-02", importedBeam.Properties["Mark"]);
            Near(5.5d, importedBeam.Quantities["LengthM"]);
            Equal(ElementDirtyFlags.All, importedBeam.Dirty);
            Equal(string.Empty, importedBase.DrawingFingerprint);
            Equal(string.Empty, importedBeam.DrawingFingerprint);
            Equal(0, importedBase.SourceHandles.Count);
            Equal(0, importedBeam.SourceHandles.Count);

            Equal(ProjectInterchangeAppendOnlyImporter.ImportMode, target.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey]);
            Equal(source.ProjectId, target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceProjectIdKey]);
            Equal(source.DrawingFingerprint, target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceDrawingFingerprintKey]);
            Equal("2", target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceHandlesDiscardedKey]);
            Equal("ImportInterchangeAppendOnly", target.AuditEvents.Last().Action);
        }

        private static void AppendPlanIsReadOnlyAndRejectsNameCollision()
        {
            var target = TargetProject();
            var sourceJson = ProjectInterchangeJsonExporter.Build(SourceProject());
            var updated = new DateTime(2026, 8, 10, 11, 30, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            var zones = target.Zones.Count;
            var floors = target.Floors.Count;
            var families = target.Families.Count;
            var elements = target.Elements.Count;
            var audits = target.AuditEvents.Count;
            var metadata = target.Metadata.Count;

            var plan = ProjectInterchangeAppendOnlyImporter.Plan(target, sourceJson);

            Equal("SOURCE-P", plan.SourceProjectId);
            Equal(1, plan.ZonesToAdd);
            Equal(1, plan.FloorsToAdd);
            Equal(1, plan.FamiliesToAdd);
            Equal(2, plan.ElementsToAdd);
            Equal(5, plan.TotalSemanticIdentitiesToAdd);
            Equal(2, plan.SourceHandlesToDiscard);
            Equal(zones, target.Zones.Count);
            Equal(floors, target.Floors.Count);
            Equal(families, target.Families.Count);
            Equal(elements, target.Elements.Count);
            Equal(audits, target.AuditEvents.Count);
            Equal(metadata, target.Metadata.Count);
            Equal(updated, target.UpdatedUtc);

            target.Families.Add(new ProjectFamily("TGT-COLLISION", "Source Beam", ElementCategory.Beam));
            families = target.Families.Count;
            Throws<InvalidOperationException>(() => ProjectInterchangeAppendOnlyImporter.Plan(target, sourceJson));
            Equal(families, target.Families.Count);
            Equal(elements, target.Elements.Count);
            Equal(audits, target.AuditEvents.Count);
            True(!target.Metadata.ContainsKey(ProjectInterchangeAppendOnlyImporter.LastModeKey));
        }

        private static void CollisionFailsBeforeMutation()
        {
            var target = TargetProject();
            target.Families.Add(new ProjectFamily("TGT-COLLISION", "Source Beam", ElementCategory.Beam));
            var sourceJson = ProjectInterchangeJsonExporter.Build(SourceProject());
            var zones = target.Zones.Count;
            var floors = target.Floors.Count;
            var families = target.Families.Count;
            var elements = target.Elements.Count;
            var audits = target.AuditEvents.Count;
            var updated = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;

            Throws<InvalidOperationException>(() => ProjectInterchangeAppendOnlyImporter.Import(target, sourceJson));

            Equal(zones, target.Zones.Count);
            Equal(floors, target.Floors.Count);
            Equal(families, target.Families.Count);
            Equal(elements, target.Elements.Count);
            Equal(audits, target.AuditEvents.Count);
            Equal(updated, target.UpdatedUtc);
            True(!target.Metadata.ContainsKey(ProjectInterchangeAppendOnlyImporter.LastModeKey));
        }

        private static void InvalidSnapshotFailsBeforeMutation()
        {
            var target = TargetProject();
            var elements = target.Elements.Count;
            var updated = new DateTime(2026, 8, 10, 12, 30, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;

            Throws<InvalidDataException>(() => ProjectInterchangeAppendOnlyImporter.Import(target, "{\"format\":\"Wrong\"}"));

            Equal(elements, target.Elements.Count);
            Equal(updated, target.UpdatedUtc);
            True(!target.Metadata.ContainsKey(ProjectInterchangeAppendOnlyImporter.LastModeKey));
        }

        private static void ApplyFailureRollsBackPartialMutation()
        {
            var target = TargetProject();
            for (var index = 0; index < 1998; index++)
                target.Zones.Add(new ZoneDefinition("TGT-Z-" + index.ToString("D4"), "Target Zone " + index.ToString("D4")));

            var source = SourceProject();
            source.Zones.Add(new ZoneDefinition("SRC-ZONE-2", "Source Zone 2"));
            var json = ProjectInterchangeJsonExporter.Build(source);
            var zones = target.Zones.Count;
            var floors = target.Floors.Count;
            var families = target.Families.Count;
            var elements = target.Elements.Count;
            var audits = target.AuditEvents.Count;
            var metadata = target.Metadata.Count;
            var updated = new DateTime(2026, 8, 10, 13, 0, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;

            Throws<InvalidOperationException>(() => ProjectInterchangeAppendOnlyImporter.Import(target, json));

            Equal(zones, target.Zones.Count);
            Equal(floors, target.Floors.Count);
            Equal(families, target.Families.Count);
            Equal(elements, target.Elements.Count);
            Equal(audits, target.AuditEvents.Count);
            Equal(metadata, target.Metadata.Count);
            Equal(updated, target.UpdatedUtc);
            True(target.FindZone("SRC-ZONE") == null);
            True(target.FindZone("SRC-ZONE-2") == null);
            True(!target.Metadata.ContainsKey(ProjectInterchangeAppendOnlyImporter.LastModeKey));
        }

        private static ProjectState TargetProject()
        {
            var project = new ProjectState("TARGET-P", "Target Project")
            {
                DrawingPath = "target.dwg",
                DrawingFingerprint = "target-fingerprint",
                ActiveZoneId = "TGT-ZONE",
                ActiveFloorId = "TGT-FLOOR"
            };
            project.Zones.Add(new ZoneDefinition("TGT-ZONE", "Target Zone"));
            project.Floors.Add(new FloorDefinition("TGT-FLOOR", "Target Floor", 0d));
            project.Families.Add(new ProjectFamily("TGT-FAM", "Target Beam", ElementCategory.Beam));
            project.Metadata["ActiveFamilyId"] = "TGT-FAM";
            project.Elements.Add(new ProjectElement("TGT-E1", ElementCategory.Beam, "TGT-FAM", "TGT-FLOOR", "TGT-ZONE"));
            return project;
        }

        private static ProjectState SourceProject()
        {
            var project = new ProjectState("SOURCE-P", "Source Project")
            {
                DrawingFingerprint = "source-fingerprint",
                UpdatedUtc = new DateTime(2026, 8, 10, 11, 0, 0, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition("SRC-ZONE", "Source Zone"));
            project.Floors.Add(new FloorDefinition("SRC-FLOOR", "Source Floor", 3.25d));
            var family = new ProjectFamily("SRC-FAM", "Source Beam", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            project.Families.Add(family);

            var baseElement = new ProjectElement("SRC-E1", ElementCategory.Beam, family.Id, "SRC-FLOOR", "SRC-ZONE")
            {
                DrawingFingerprint = project.DrawingFingerprint
            };
            baseElement.SourceHandles.Add("1A2B");
            baseElement.Properties["Mark"] = "B-01";
            baseElement.Quantities["LengthM"] = 1d;
            project.Elements.Add(baseElement);

            var beam = new ProjectElement("SRC-E2", ElementCategory.Beam, family.Id, "SRC-FLOOR", "SRC-ZONE")
            {
                DrawingFingerprint = project.DrawingFingerprint
            };
            beam.SourceHandles.Add("2B3C");
            beam.DependsOn.Add(baseElement.Id);
            beam.Properties["Mark"] = "B-02";
            beam.Quantities["LengthM"] = 5.5d;
            project.Elements.Add(beam);
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
}
