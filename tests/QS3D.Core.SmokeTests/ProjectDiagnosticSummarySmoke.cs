using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectDiagnosticSummarySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SummaryContainsCountsWithoutProjectPayload();
            NullIssuesFailClosedWithoutReplacingExport();
            ExportReplacesAtomically();
        }

        private static void SummaryContainsCountsWithoutProjectPayload()
        {
            var project = new ProjectState("SECRET-PROJECT-ID", "Customer Tower");
            project.DrawingPath = @"C:\Customer\Secret\tower.dwg";
            project.DrawingFingerprint = "PRIVATE-DWG-FINGERPRINT";
            project.Zones.Add(new ZoneDefinition("Z-SECRET", "VIP Zone"));
            project.Floors.Add(new FloorDefinition("F-SECRET", "Executive Floor", 0d));
            var family = new ProjectFamily("FAM-SECRET", "Private Beam Family", ElementCategory.Beam);
            family.Properties["Material"] = "SecretConcrete";
            project.Families.Add(family);
            var element = new ProjectElement("E-SECRET", ElementCategory.Beam, family.Id, "F-SECRET", "Z-SECRET");
            element.DrawingFingerprint = "ELEMENT-FINGERPRINT";
            element.SourceHandles.Add("DEADBEEF");
            element.Properties["Mark"] = "PRIVATE-MARK";
            element.SetQuantity("NetVolumeM3", 123.456d);
            project.Elements.Add(element);

            var json = ProjectDiagnosticSummaryExporter.Build(project, new[]
            {
                new ModelHealthIssue("missing_material", HealthSeverity.Warning, "Sensitive detail DEADBEEF E-SECRET", "E-SECRET"),
                new ModelHealthIssue("MISSING_MATERIAL", HealthSeverity.Warning, "Another sensitive detail", "E-OTHER"),
                new ModelHealthIssue("BAD_GEOMETRY", HealthSeverity.Error, "Customer geometry detail", "E-SECRET")
            });

            Require(json, "\"format\":\"QS3D.DiagnosticSummary\"");
            Require(json, "\"elements\":1");
            Require(json, "\"category\":\"Beam\"");
            Require(json, "\"code\":\"MISSING_MATERIAL\",\"count\":2");
            Require(json, "\"code\":\"BAD_GEOMETRY\",\"count\":1");

            Forbid(json, "SECRET-PROJECT-ID");
            Forbid(json, "Customer Tower");
            Forbid(json, "tower.dwg");
            Forbid(json, "PRIVATE-DWG-FINGERPRINT");
            Forbid(json, "Z-SECRET");
            Forbid(json, "Executive Floor");
            Forbid(json, "FAM-SECRET");
            Forbid(json, "Private Beam Family");
            Forbid(json, "SecretConcrete");
            Forbid(json, "E-SECRET");
            Forbid(json, "DEADBEEF");
            Forbid(json, "PRIVATE-MARK");
            Forbid(json, "123.456");
            Forbid(json, "Sensitive detail");
            Forbid(json, "Customer geometry detail");
        }

        private static void NullIssuesFailClosedWithoutReplacingExport()
        {
            var project = new ProjectState("P-NULL", "Diagnostic Null");
            var malformed = new ModelHealthIssue[]
            {
                new ModelHealthIssue("VALID", HealthSeverity.Info, "valid"),
                null!
            };

            try
            {
                _ = ProjectDiagnosticSummaryExporter.Build(project, malformed);
                throw new Exception("Diagnostic summary Build must reject a null health issue.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("null health issue", StringComparison.Ordinal) < 0)
                    throw new Exception("Diagnostic summary Build rejected a null issue for the wrong reason.", ex);
            }

            var path = Path.Combine(Path.GetTempPath(), "qs3d-diagnostic-null-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(path, "old");
                try
                {
                    ProjectDiagnosticSummaryExporter.Export(path, project, malformed);
                    throw new Exception("Diagnostic summary Export must reject a null health issue.");
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message.IndexOf("null health issue", StringComparison.Ordinal) < 0)
                        throw new Exception("Diagnostic summary Export rejected a null issue for the wrong reason.", ex);
                }
                var persisted = File.ReadAllText(path);
                if (!string.Equals(persisted, "old", StringComparison.Ordinal))
                    throw new Exception("Malformed diagnostic export must not replace the existing destination.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }

        private static void ExportReplacesAtomically()
        {
            var project = new ProjectState("P", "Diagnostic");
            var path = Path.Combine(Path.GetTempPath(), "qs3d-diagnostic-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(path, "old");
                ProjectDiagnosticSummaryExporter.Export(path, project, Array.Empty<ModelHealthIssue>());
                var json = File.ReadAllText(path);
                Require(json, "QS3D.DiagnosticSummary");
                Forbid(json, "old");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }

        private static void Require(string text, string token)
        {
            if (!text.Contains(token)) throw new Exception("Expected diagnostic token: " + token);
        }

        private static void Forbid(string text, string token)
        {
            if (text.Contains(token)) throw new Exception("Diagnostic summary leaked private project payload: " + token);
        }
    }
}
