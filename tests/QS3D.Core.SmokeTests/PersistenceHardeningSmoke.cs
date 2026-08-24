using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class PersistenceHardeningSmoke
    {
        public static void Run()
        {
            V1Migration();
            BackupRecovery();
            RecoverySavePreservesValidatedBackup();
            PublishNewRejectsExistingPair();
            MissingPrimaryBackupRecovery();
            DuplicateIdRejected();
            InvalidNumericRejected();
            DirtyStateRoundtrip();
            CheckpointCaptureRejectsRevisionDrift();
            CheckpointCaptureStableControl();
            SnapshotRejectsOversizedTopLevelState();
            SnapshotRejectsOversizedNestedState();
            SnapshotAcceptsExactBoundaryAndRestores();
            StableIdQuantityGrouping();
            ExportHeadersAndWorksheetUx();
            HealthRecoveryStates();
        }

        private static void CheckpointCaptureRejectsRevisionDrift()
        {
            var project = NewProject("checkpoint-drift", "Checkpoint drift");
            var first = new ProjectElement("E1", ElementCategory.ArchitecturalWall);
            var second = new ProjectElement("E2", ElementCategory.ArchitecturalWall);
            project.Elements.Add(first);
            project.Elements.Add(second);
            project.Zones.Add(new ZoneDefinition("z2", "Vùng-2"));

            var rejected = false;
            try
            {
                ProjectPersistenceCheckpoint.Capture(project, MutatingCheckpointIds(project));
            }
            catch (InvalidOperationException ex)
            {
                rejected = ex.Message.IndexOf("project revision is changing", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            Require(rejected, "Persistence checkpoint accepted element state captured across a project revision change.");
        }

        private static IEnumerable<string> MutatingCheckpointIds(ProjectState project)
        {
            yield return "E1";
            project.ActiveZoneId = "z2";
            yield return "E2";
        }

        private static void CheckpointCaptureStableControl()
        {
            var project = NewProject("checkpoint-stable", "Checkpoint stable");
            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall);
            project.Elements.Add(element);
            var checkpoint = ProjectPersistenceCheckpoint.Capture(project, new[] { "E1" });

            Require(checkpoint.Matches(project), "Stable persistence checkpoint did not match its unchanged project revision.");
            var expectedVersion = checkpoint.ProjectChangeVersion;
            var expectedUpdatedUtc = checkpoint.ProjectUpdatedUtc;
            element.SetProperty("LengthM", "5");
            Require(!checkpoint.Matches(project), "Persistence checkpoint ignored changed element persistence state.");
            checkpoint.Restore(project);
            Require(checkpoint.Matches(project), "Persistence checkpoint did not restore its captured persistence state.");
            Require(project.ChangeVersion == expectedVersion && project.UpdatedUtc == expectedUpdatedUtc,
                "Persistence checkpoint did not restore the captured project persistence revision.");
        }

        private static void SnapshotRejectsOversizedTopLevelState()
        {
            var project = new ProjectState("snapshot-top-level-bound", "Snapshot top-level bound");
            for (var index = 0; index <= 100000; index++)
                project.Zones.Add(new ZoneDefinition("Z" + index, "Zone " + index));

            var captureRejected = false;
            try { ProjectStateSnapshot.Capture(project); }
            catch (InvalidOperationException ex)
            {
                captureRejected = ex.Message.IndexOf("zones", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                  ex.Message.IndexOf("100000", StringComparison.Ordinal) >= 0;
            }
            Require(captureRejected, "ProjectStateSnapshot.Capture accepted more than 100,000 top-level zones.");

            var detachedRejected = false;
            try { ProjectStateSnapshot.CreateDetachedCopy(project); }
            catch (InvalidOperationException ex)
            {
                detachedRejected = ex.Message.IndexOf("zones", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                   ex.Message.IndexOf("100000", StringComparison.Ordinal) >= 0;
            }
            Require(detachedRejected, "ProjectStateSnapshot.CreateDetachedCopy bypassed the top-level snapshot bound.");
        }

        private static void SnapshotRejectsOversizedNestedState()
        {
            var project = new ProjectState("snapshot-nested-bound", "Snapshot nested bound");
            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall);
            for (var index = 0; index <= 10000; index++)
                element.Properties.Add("P" + index, index.ToString());
            project.Elements.Add(element);

            var rejected = false;
            try { ProjectStateSnapshot.Capture(project); }
            catch (InvalidOperationException ex)
            {
                rejected = ex.Message.IndexOf("element E1 properties", StringComparison.OrdinalIgnoreCase) >= 0 &&
                           ex.Message.IndexOf("10000", StringComparison.Ordinal) >= 0;
            }
            Require(rejected, "ProjectStateSnapshot accepted more than 10,000 nested element properties.");
        }

        private static void SnapshotAcceptsExactBoundaryAndRestores()
        {
            var project = new ProjectState("snapshot-exact-bound", "Snapshot exact bound");
            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall);
            for (var index = 0; index < 10000; index++)
                element.SourceHandles.Add(index.ToString("X"));
            project.Elements.Add(element);

            var snapshot = ProjectStateSnapshot.Capture(project);
            element.SourceHandles[0] = "MUTATED";
            element.SourceHandles.RemoveAt(element.SourceHandles.Count - 1);
            snapshot.Restore(project);

            Require(ReferenceEquals(project.Elements.Single(), element), "Snapshot restore replaced the captured element identity at the exact bound.");
            Require(element.SourceHandles.Count == 10000, "Snapshot restore lost exact-bound source handles.");
            Require(element.SourceHandles[0] == "0" && element.SourceHandles[9999] == 9999.ToString("X"),
                "Snapshot restore did not reproduce exact-bound nested state.");
        }

        private static void RecoverySavePreservesValidatedBackup()
        {
            var path = Temp("recovery-heal", ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(NewProject("p", "Known Good"), path);
                store.Save(NewProject("p", "Newer"), path);
                File.WriteAllText(path, "<broken", Encoding.UTF8);
                var recovered = store.LoadWithBackupFallback(path);
                Require(recovered.RecoveredFromBackup, "Recovery-safe save test did not load the backup generation.");

                store.SavePreservingValidatedBackup(recovered.Project, path);
                Require(store.Load(path).Name == "Known Good", "Recovery-safe save did not heal the primary from the validated project.");
                Require(store.Load(path + ".bak").Name == "Known Good", "Recovery-safe save replaced the validated backup with the corrupt primary.");
            }
            finally { Delete(path); Delete(path + ".bak"); }
        }

        private static void PublishNewRejectsExistingPair()
        {
            var path = Temp("publish-new", ".qsdb");
            try
            {
                File.WriteAllText(path + ".bak", "external-backup", Encoding.UTF8);
                var rejected = false;
                try { new QsdbProjectStore().SaveNew(NewProject("p", "New"), path); }
                catch (IOException) { rejected = true; }
                Require(rejected, "Create-new QSDB publication accepted an existing backup.");
                Require(!File.Exists(path), "Rejected create-new QSDB publication left a primary behind.");
                Require(File.ReadAllText(path + ".bak", Encoding.UTF8) == "external-backup", "Rejected create-new QSDB publication changed the existing backup.");
            }
            finally { Delete(path); Delete(path + ".bak"); }
        }

        private static void V1Migration()
        {
            var path = Temp("legacy", ".qsdb");
            try
            {
                File.WriteAllText(path,
                    "<qs3d schema=\"1\" projectId=\"legacy\" name=\"Legacy\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"\" activeFloorId=\"\">" +
                    "<metadata/><zones/><floors/><families/><elements/></qs3d>", Encoding.UTF8);
                var project = new QsdbProjectStore().Load(path);
                Require(project.SchemaVersion == ProjectState.CurrentSchemaVersion, "Legacy project did not migrate to current schema.");
                Require(project.Metadata.TryGetValue("QS3D.SchemaMigratedFrom", out var migrated) && migrated == "1", "Migration provenance metadata missing.");
                Require(project.UpdatedUtc == new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Legacy updatedUtc fallback is not deterministic.");
            }
            finally { Delete(path); }
        }

        private static void BackupRecovery()
        {
            var path = Temp("recovery", ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                var first = NewProject("p", "First");
                store.Save(first, path);
                var second = NewProject("p", "Second");
                store.Save(second, path);
                Require(File.Exists(path + ".bak"), "QSDB backup was not created.");

                File.WriteAllText(path, "<broken", Encoding.UTF8);
                var recovered = store.LoadWithBackupFallback(path);
                Require(recovered.RecoveredFromBackup, "Corrupt primary QSDB did not fall back to backup.");
                Require(recovered.Project.Name == "First", "Backup recovery loaded the wrong project generation.");
                Require(recovered.SourcePath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase), "Backup recovery source was not reported.");
                Require(!string.IsNullOrWhiteSpace(recovered.PrimaryFailureMessage), "Primary load failure was not preserved.");
            }
            finally { Delete(path); Delete(path + ".bak"); Delete(path + ".tmp"); }
        }

        private static void MissingPrimaryBackupRecovery()
        {
            var path = Temp("missing-primary", ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(NewProject("p", "First"), path);
                store.Save(NewProject("p", "Second"), path);
                Delete(path);
                var recovered = store.LoadWithBackupFallback(path);
                Require(recovered.RecoveredFromBackup, "Missing primary QSDB did not fall back to backup.");
                Require(recovered.Project.Name == "First", "Missing-primary recovery loaded the wrong backup generation.");
            }
            finally { Delete(path); Delete(path + ".bak"); Delete(path + ".tmp"); }
        }

        private static void DuplicateIdRejected()
        {
            var path = Temp("duplicate", ".qsdb");
            try
            {
                File.WriteAllText(path,
                    "<qs3d schema=\"2\" projectId=\"p\" name=\"Duplicate\" updatedUtc=\"2026-08-10T00:00:00.0000000Z\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"z\" activeFloorId=\"f\">" +
                    "<metadata/><zones><zone id=\"z\" name=\"A\"/><zone id=\"z\" name=\"B\"/></zones>" +
                    "<floors><floor id=\"f\" name=\"Floor\" elevationM=\"0\"/></floors><families/><elements/></qs3d>", Encoding.UTF8);
                var rejected = false;
                try { new QsdbProjectStore().Load(path); }
                catch (InvalidDataException) { rejected = true; }
                Require(rejected, "Duplicate QSDB ids were accepted.");
            }
            finally { Delete(path); }
        }

        private static void InvalidNumericRejected()
        {
            var path = Temp("invalid-number", ".qsdb");
            try
            {
                File.WriteAllText(path,
                    "<qs3d schema=\"2\" projectId=\"p\" name=\"Invalid\" updatedUtc=\"2026-08-10T00:00:00.0000000Z\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"z\" activeFloorId=\"f\">" +
                    "<metadata/><zones><zone id=\"z\" name=\"A\"/></zones>" +
                    "<floors><floor id=\"f\" name=\"Floor\" elevationM=\"not-a-number\"/></floors><families/><elements/></qs3d>", Encoding.UTF8);
                var rejected = false;
                try { new QsdbProjectStore().Load(path); }
                catch (InvalidDataException) { rejected = true; }
                Require(rejected, "Invalid QSDB numeric data was silently converted to zero.");
            }
            finally { Delete(path); }
        }

        private static void DirtyStateRoundtrip()
        {
            var path = Temp("dirty", ".qsdb");
            try
            {
                var project = NewProject("p", "Dirty");
                var family = new ProjectFamily("wall", "Tường 200", ElementCategory.ArchitecturalWall);
                project.Families.Add(family);
                var element = new ProjectElement("W1", ElementCategory.ArchitecturalWall, family.Id, "f", "z");
                element.SetQuantity("NetVolumeM3", 3d);
                element.MarkClean(ElementDirtyFlags.All);
                element.SetProperty("LengthM", "5.5");
                var expectedDirty = element.Dirty;
                project.Elements.Add(element);

                var store = new QsdbProjectStore();
                store.Save(project, path);
                var restored = store.Load(path).Elements.Single();
                Require(restored.Dirty == expectedDirty, "Element dirty flags were lost across QSDB save/reload.");
                Require((restored.Dirty & ElementDirtyFlags.Quantity) != 0, "Stale quantity state was incorrectly restored as clean.");
                Require(restored.UpdatedUtc.Kind == DateTimeKind.Utc, "Element updatedUtc did not roundtrip as UTC.");
            }
            finally { Delete(path); Delete(path + ".bak"); Delete(path + ".tmp"); }
        }

        private static void StableIdQuantityGrouping()
        {
            var project = new ProjectState("grouping", "Grouping");
            project.Zones.Add(new ZoneDefinition("z", "Vùng-1"));
            project.Floors.Add(new FloorDefinition("f1", "Tầng Trệt", 0d));
            project.Floors.Add(new FloorDefinition("f2", "Tầng Trệt", 3.6d));
            var familyA = new ProjectFamily("fa", "Tường 200", ElementCategory.ArchitecturalWall);
            var familyB = new ProjectFamily("fb", "Tường 200", ElementCategory.ArchitecturalWall);
            project.Families.Add(familyA);
            project.Families.Add(familyB);
            var a = new ProjectElement("A", ElementCategory.ArchitecturalWall, familyA.Id, "f1", "z"); a.SetQuantity("NetVolumeM3", 1d); a.MarkClean(ElementDirtyFlags.All);
            var b = new ProjectElement("B", ElementCategory.ArchitecturalWall, familyB.Id, "f2", "z"); b.SetQuantity("NetVolumeM3", 2d); b.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(a); project.Elements.Add(b);
            var rows = ProjectQuantityReportBuilder.Group(project);
            Require(rows.Count == 2, "BQ merged different Floor/Family IDs because their display names matched.");
        }

        private static void ExportHeadersAndWorksheetUx()
        {
            var path = Temp("export", ".xlsx");
            try
            {
                var rows = new[]
                {
                    new QuantityReportRow
                    {
                        Floor = "Nền 0.00", Category = "ArchitecturalWall", FamilyName = "Tường 200", Count = 1,
                        BottomAreaM2 = 1.1, TopAreaM2 = 1.2, OtherAreaM2 = 1.3
                    }
                };
                XlsxQuantityExporter.Export(path, rows);
                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new Exception("Missing worksheet XML.");
                    using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                    {
                        var xml = reader.ReadToEnd();
                        Require(xml.Contains("DT đáy (m²)"), "Excel bottom-area header missing.");
                        Require(xml.Contains("DT đỉnh (m²)"), "Excel top-area header missing.");
                        Require(xml.Contains("DT khác (m²)"), "Excel other-area header missing.");
                        Require(xml.Contains("CAD Handle (hex)"), "Excel CAD Handle header missing.");
                        Require(xml.Contains("QS3D Drawing Fingerprint"), "Excel drawing-fingerprint header missing.");
                        Require(!xml.Contains("Đỉnh cửa"), "Stale mismatched Excel header is still present.");
                        Require(xml.Contains("state=\"frozen\""), "Excel header row is not frozen.");
                        Require(xml.Contains("<autoFilter ref=\"A1:T2\"/>"), "Excel autofilter range is missing.");
                    }
                }
            }
            finally { Delete(path); }
        }

        private static void HealthRecoveryStates()
        {
            var project = NewProject("p", "Health");
            project.Metadata["QS3D.RecoveredFromBackup"] = "true";
            var issues = new ModelHealthService().Inspect(project);
            Require(issues.Any(x => x.Code == "PROJECT_RECOVERED_BACKUP"), "Recovered-backup health warning missing.");

            project.Metadata.Remove("QS3D.RecoveredFromBackup");
            project.Metadata["QS3D.ReadOnlyRecoveryRequired"] = "true";
            project.Metadata["QS3D.LoadWarning"] = "invalid xml";
            issues = new ModelHealthService().Inspect(project);
            Require(issues.Any(x => x.Code == "PROJECT_LOAD_FAILED" && x.Severity == HealthSeverity.Error), "Protected-load health error missing.");
        }

        private static ProjectState NewProject(string id, string name)
        {
            var project = new ProjectState(id, name);
            project.Zones.Add(new ZoneDefinition("z", "Vùng-1"));
            project.Floors.Add(new FloorDefinition("f", "Nền 0.00", 0));
            project.ActiveZoneId = "z";
            project.ActiveFloorId = "f";
            return project;
        }

        private static string Temp(string prefix, string extension) => Path.Combine(Path.GetTempPath(), "qs3d-" + prefix + "-" + Guid.NewGuid().ToString("N") + extension);
        private static void Delete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    }
}
