using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectDiagnosticSummaryUnicodeSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsMalformedDiagnosticCodes();
            PreservesSupplementaryUnicodeOnExport();
        }

        private static void RejectsMalformedDiagnosticCodes()
        {
            var project = new ProjectState("P-DIAG-UNICODE", "Diagnostic Unicode");
            Throws<EncoderFallbackException>(() => ProjectDiagnosticSummaryExporter.Build(
                project,
                new[] { new ModelHealthIssue("BAD-\ud800", HealthSeverity.Warning, "high surrogate") }));
            Throws<EncoderFallbackException>(() => ProjectDiagnosticSummaryExporter.Build(
                project,
                new[] { new ModelHealthIssue("BAD-\udc00", HealthSeverity.Warning, "low surrogate") }));
        }

        private static void PreservesSupplementaryUnicodeOnExport()
        {
            var project = new ProjectState("P-DIAG-UNICODE-VALID", "Diagnostic Unicode Valid");
            var code = "DIAG-\U0001F600";
            var issues = new[] { new ModelHealthIssue(code, HealthSeverity.Info, "valid supplementary Unicode") };
            var built = ProjectDiagnosticSummaryExporter.Build(project, issues);
            True(built.Contains(code), "Diagnostic summary build must preserve valid supplementary Unicode.");

            var path = Path.Combine(Path.GetTempPath(), "qs3d-diagnostic-unicode-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                ProjectDiagnosticSummaryExporter.Export(path, project, issues);
                var persisted = File.ReadAllText(path, new UTF8Encoding(false, true));
                True(persisted.Contains(code), "Diagnostic summary export must preserve valid supplementary Unicode as strict UTF-8.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch { }
            }
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
            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
