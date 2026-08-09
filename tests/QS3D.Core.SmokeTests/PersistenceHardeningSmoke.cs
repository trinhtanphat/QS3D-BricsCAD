using System;
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
            DuplicateIdRejected();
            ExportHeadersAndWorksheetUx();
            HealthRecoveryStates();
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
                        Require(!xml.Contains("Đỉnh cửa"), "Stale mismatched Excel header is still present.");
                        Require(xml.Contains("state=\"frozen\""), "Excel header row is not frozen.");
                        Require(xml.Contains("<autoFilter ref=\"A1:P2\"/>"), "Excel autofilter range is missing.");
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
