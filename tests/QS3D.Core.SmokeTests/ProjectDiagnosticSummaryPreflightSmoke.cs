using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectDiagnosticSummaryPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NullIssueDoesNotCreateDestinationDirectory();
            ThrowingLazyIssuesDoNotCreateDestinationDirectory();
            ValidExportStillWritesSnapshot();
        }

        private static void NullIssueDoesNotCreateDestinationDirectory()
        {
            var root = UniqueRoot("NULL");
            var path = Path.Combine(root, "nested", "summary.json");
            try
            {
                var project = new ProjectState("P-DIAG-SUMMARY-NULL", "Diagnostic summary null issue");
                ExpectThrows<InvalidOperationException>(() =>
                    ProjectDiagnosticSummaryExporter.Export(path, project, new ModelHealthIssue[] { null! }));
                False(Directory.Exists(root));
            }
            finally
            {
                Cleanup(root);
            }
        }

        private static void ThrowingLazyIssuesDoNotCreateDestinationDirectory()
        {
            var root = UniqueRoot("LAZY");
            var path = Path.Combine(root, "nested", "summary.json");
            try
            {
                var project = new ProjectState("P-DIAG-SUMMARY-LAZY", "Diagnostic summary lazy issue");
                ExpectThrows<InvalidOperationException>(() =>
                    ProjectDiagnosticSummaryExporter.Export(path, project, ThrowingIssues()));
                False(Directory.Exists(root));
            }
            finally
            {
                Cleanup(root);
            }
        }

        private static void ValidExportStillWritesSnapshot()
        {
            var root = UniqueRoot("VALID");
            var path = Path.Combine(root, "nested", "summary.json");
            try
            {
                var project = new ProjectState("P-DIAG-SUMMARY-VALID", "Diagnostic summary valid");
                var issues = new[]
                {
                    new ModelHealthIssue("VALID_WARNING", HealthSeverity.Warning, "Valid diagnostic")
                };

                ProjectDiagnosticSummaryExporter.Export(path, project, issues);

                True(File.Exists(path));
                var content = File.ReadAllText(path);
                True(content.Contains("\"format\":\"QS3D.DiagnosticSummary\""));
                True(content.Contains("\"code\":\"VALID_WARNING\""));
            }
            finally
            {
                Cleanup(root);
            }
        }

        private static IEnumerable<ModelHealthIssue> ThrowingIssues()
        {
            yield return new ModelHealthIssue("FIRST", HealthSeverity.Info, "First diagnostic");
            throw new InvalidOperationException("Synthetic lazy diagnostic failure.");
        }

        private static string UniqueRoot(string suffix) =>
            Path.Combine(Path.GetTempPath(), "QS3D-DIAG-SUMMARY-PREFLIGHT-" + suffix + "-" + Guid.NewGuid().ToString("N"));

        private static void Cleanup(string root)
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        private static void ExpectThrows<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected exception: " + typeof(TException).Name + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("Expected condition to be true.");
        }

        private static void False(bool value)
        {
            if (value) throw new InvalidOperationException("Expected condition to be false.");
        }
    }
}
