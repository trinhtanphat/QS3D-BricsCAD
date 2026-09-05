using System;
using System.Globalization;
using System.IO;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeValidationSmoke
    {
        public static void Run()
        {
            ExportedValidSnapshotPasses();
            TimestampCanonicalityFailsClosed();
            NameCanonicalityFailsClosed();
            WrongUnitsFailClosed();
            GeneratedOwnershipSmugglingFailsClosed();
            BrokenDependencyFailsClosed();
            DependencyCycleFailsClosed();
            InvalidUtf8FileFailsClosed();
            OversizeFilePreservesGuardedLimit();
            MissingRequiredCollectionFailsClosed();
            EmptyRequiredNamesFailClosed();
        }

        private static void ExportedValidSnapshotPasses()
        {
            var project = BuildFixture();
            var json = ProjectInterchangeJsonExporter.Build(project);
            var result = ProjectInterchangeJsonValidator.Validate(json);
            if (!result.IsValid)
                throw new Exception("Exporter-produced valid semantic snapshot must validate: " + string.Join(" | ", result.Issues.Select(x => x.Code + ":" + x.Message)));
            if (result.ElementCount != 2 || result.FamilyCount != 1 || result.FloorCount != 1 || result.ZoneCount != 1)
                throw new Exception("Interchange validator summary counts are incorrect.");
            if (!string.Equals(result.Format, ProjectInterchangeJsonExporter.FormatName, StringComparison.Ordinal) || result.FormatVersion != ProjectInterchangeJsonExporter.FormatVersion)
                throw new Exception("Interchange validator did not preserve format identity.");
        }

        private static void TimestampCanonicalityFailsClosed()
        {
            var canonical = ProjectInterchangeJsonExporter.Build(BuildFixture());
            const string writerToken = "2026-08-10T11:00:00.0000000Z";
            if (canonical.IndexOf(writerToken, StringComparison.Ordinal) < 0)
                throw new Exception("Interchange timestamp regression fixture no longer contains the expected canonical writer token.");

            RequireError(
                ProjectInterchangeJsonValidator.Validate(canonical.Replace(writerToken, "2026-08-10T18:00:00.0000000+07:00")),
                "TIMESTAMP_NOT_UTC");
            RequireError(
                ProjectInterchangeJsonValidator.Validate(canonical.Replace(writerToken, "2026-08-10T11:00:00.0000000+00:00")),
                "TIMESTAMP_NOT_UTC");
            RequireError(
                ProjectInterchangeJsonValidator.Validate(canonical.Replace(writerToken, "2026-08-10T11:00:00.0000000")),
                "TIMESTAMP_NOT_UTC");
            RequireError(
                ProjectInterchangeJsonValidator.Validate(canonical.Replace(writerToken, "2026-08-10T11:00:00Z")),
                "TIMESTAMP_NOT_UTC");
            RequireError(
                ProjectInterchangeJsonValidator.Validate(canonical.Replace(writerToken, " 2026-08-10T11:00:00.0000000Z")),
                "TIMESTAMP_INVALID");
        }

        private static void NameCanonicalityFailsClosed()
        {
            var canonical = ProjectInterchangeJsonExporter.Build(BuildFixture());
            RequireError(ProjectInterchangeJsonValidator.Validate(canonical.Replace("\"name\":\"Interchange Validate Smoke\"", "\"name\":\" Interchange Validate Smoke\"")), "NAME_NON_CANONICAL");
            RequireError(ProjectInterchangeJsonValidator.Validate(canonical.Replace("\"name\":\"Zone 1\"", "\"name\":\"Zone 1 \"")), "NAME_NON_CANONICAL");
            RequireError(ProjectInterchangeJsonValidator.Validate(canonical.Replace("\"name\":\"L01\"", "\"name\":\" L01 \"")), "NAME_NON_CANONICAL");
            RequireError(ProjectInterchangeJsonValidator.Validate(canonical.Replace("\"name\":\"B300x500\"", "\"name\":\"B300x500 \"")), "NAME_NON_CANONICAL");
        }

        private static void WrongUnitsFailClosed()
        {
            var json = ProjectInterchangeJsonExporter.Build(BuildFixture()).Replace("\"length\":\"m\"", "\"length\":\"mm\"");
            RequireError(ProjectInterchangeJsonValidator.Validate(json), "UNIT_LENGTH");
        }

        private static void GeneratedOwnershipSmugglingFailsClosed()
        {
            var json = ProjectInterchangeJsonExporter.Build(BuildFixture()).Replace("\"Mark\":\"B-01\"", "\"GeneratedSolidHandle\":\"DEAD\",\"Mark\":\"B-01\"");
            RequireError(ProjectInterchangeJsonValidator.Validate(json), "GENERATED_RUNTIME_PROPERTY");
        }

        private static void BrokenDependencyFailsClosed()
        {
            var json = ProjectInterchangeJsonExporter.Build(BuildFixture()).Replace("\"dependencies\": [\"E-ROOT\"]", "\"dependencies\": [\"E-MISSING\"]");
            RequireError(ProjectInterchangeJsonValidator.Validate(json), "DEPENDENCY_REF_MISSING");
        }

        private static void DependencyCycleFailsClosed()
        {
            var json = ProjectInterchangeJsonExporter.Build(BuildFixture()).Replace("\"dependencies\": []", "\"dependencies\": [\"E-001\"]");
            RequireError(ProjectInterchangeJsonValidator.Validate(json), "DEPENDENCY_CYCLE");
        }

        private static void InvalidUtf8FileFailsClosed()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-interchange-invalid-" + Guid.NewGuid().ToString("N") + ".qs3d.json");
            try
            {
                File.WriteAllBytes(path, new byte[] { (byte)'{', 0xff, (byte)'}' });
                RequireError(ProjectInterchangeJsonValidator.ValidateFile(path), "JSON_UTF8");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }

        private static void OversizeFilePreservesGuardedLimit()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-interchange-oversize-" + Guid.NewGuid().ToString("N") + ".qs3d.json");
            try
            {
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) stream.SetLength(ProjectInterchangeJsonValidator.MaxFileBytes + 1L);
                try
                {
                    ProjectInterchangeJsonValidator.ValidateFile(path);
                }
                catch (InvalidDataException ex)
                {
                    var expected = "Semantic snapshot exceeds the guarded " + ProjectInterchangeJsonValidator.MaxFileBytes.ToString(CultureInfo.InvariantCulture) + " byte limit.";
                    if (!string.Equals(ex.Message, expected, StringComparison.Ordinal)) throw new Exception("Interchange oversize guard changed its public error contract: " + ex.Message);
                    return;
                }
                throw new Exception("Interchange validator must reject files above MaxFileBytes.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }

        private static void MissingRequiredCollectionFailsClosed()
        {
            const string json = "{\"format\":\"QS3D.SemanticSnapshot\",\"formatVersion\":1,\"units\":{\"length\":\"m\",\"area\":\"m2\",\"volume\":\"m3\",\"mass\":\"kg\"},\"project\":{\"id\":\"P\",\"name\":\"N\",\"schemaVersion\":1,\"drawingFingerprint\":\"\",\"updatedUtc\":\"2026-08-10T11:00:00.0000000Z\"},\"floors\":[],\"families\":[],\"elements\":[]}";
            RequireError(ProjectInterchangeJsonValidator.Validate(json), "COLLECTION_MISSING");
        }

        private static void EmptyRequiredNamesFailClosed()
        {
            var json = ProjectInterchangeJsonExporter.Build(BuildFixture()).Replace("\"name\":\"Interchange Validate Smoke\"", "\"name\":\"\"").Replace("\"name\":\"Zone 1\"", "\"name\":\"\"");
            var result = ProjectInterchangeJsonValidator.Validate(json);
            RequireError(result, "PROJECT_NAME_EMPTY");
            RequireError(result, "NAME_EMPTY");
        }

        private static ProjectState BuildFixture()
        {
            var project = new ProjectState("P-VALIDATE", "Interchange Validate Smoke")
            {
                DrawingFingerprint = "DWG-FP"
            };
            project.Zones.Add(new ZoneDefinition("Z-1", "Zone 1"));
            project.Floors.Add(new FloorDefinition("FL-1", "L01", 0d));
            project.Families.Add(new ProjectFamily("FAM-1", "B300x500", ElementCategory.Beam));
            project.UpdatedUtc = new DateTime(2026, 8, 10, 11, 0, 0, DateTimeKind.Utc);

            var root = new ProjectElement("E-ROOT", ElementCategory.Beam, "FAM-1", "FL-1", "Z-1") { DrawingFingerprint = "DWG-FP" };
            root.SourceHandles.Add("100");
            root.SetProperty("Mark", "B-00");
            root.SetQuantity("LengthM", 3d);
            project.Elements.Add(root);

            var child = new ProjectElement("E-001", ElementCategory.Beam, "FAM-1", "FL-1", "Z-1") { DrawingFingerprint = "DWG-FP" };
            child.SourceHandles.Add("101");
            child.DependsOn.Add("E-ROOT");
            child.SetProperty("Mark", "B-01");
            child.SetQuantity("LengthM", 5d);
            project.Elements.Add(child);
            return project;
        }

        private static void RequireError(ProjectInterchangeValidationResult result, string code)
        {
            if (result.IsValid) throw new Exception("Expected interchange validation failure: " + code);
            if (!result.Issues.Any(x => x.Severity == InterchangeValidationSeverity.Error && string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new Exception("Expected interchange validation issue: " + code + "; got " + string.Join(",", result.Issues.Select(x => x.Code)));
        }
    }
}
