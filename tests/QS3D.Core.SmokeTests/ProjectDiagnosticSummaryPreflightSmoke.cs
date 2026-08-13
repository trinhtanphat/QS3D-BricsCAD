using System;
using System.Collections;
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
            IssueInputIsBoundedAndEnumeratedOnce();
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

        private static void IssueInputIsBoundedAndEnumeratedOnce()
        {
            var project = new ProjectState("P-DIAG-SUMMARY-BOUND", "Diagnostic summary issue bound");
            var issue = new ModelHealthIssue("BOUNDED_WARNING", HealthSeverity.Warning, "Bounded diagnostic");
            var accepted = new SingleUseIssueSequence(ProjectDiagnosticSummaryExporter.MaxIssueCount, issue);
            var json = ProjectDiagnosticSummaryExporter.Build(project, accepted);
            True(json.Contains("\"code\":\"BOUNDED_WARNING\",\"count\":" + ProjectDiagnosticSummaryExporter.MaxIssueCount));
            Equal(1, accepted.EnumerationCount);
            Equal(ProjectDiagnosticSummaryExporter.MaxIssueCount, accepted.YieldedCount);

            var root = UniqueRoot("BOUND");
            var path = Path.Combine(root, "summary.json");
            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(path, "old");
                var excessive = new SingleUseIssueSequence(ProjectDiagnosticSummaryExporter.MaxIssueCount + 1, issue);
                ExpectThrows<InvalidOperationException>(() =>
                    ProjectDiagnosticSummaryExporter.Export(path, project, excessive));
                Equal(1, excessive.EnumerationCount);
                Equal(ProjectDiagnosticSummaryExporter.MaxIssueCount + 1, excessive.YieldedCount);
                Equal("old", File.ReadAllText(path));
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

        private sealed class SingleUseIssueSequence : IEnumerable<ModelHealthIssue>
        {
            private readonly int _count;
            private readonly ModelHealthIssue _issue;

            public SingleUseIssueSequence(int count, ModelHealthIssue issue)
            {
                _count = count;
                _issue = issue;
            }

            public int EnumerationCount { get; private set; }
            public int YieldedCount { get; private set; }

            public IEnumerator<ModelHealthIssue> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount > 1)
                    throw new InvalidOperationException("Diagnostic issue input was enumerated more than once.");
                return Enumerate().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<ModelHealthIssue> Enumerate()
            {
                for (var index = 0; index < _count; index++)
                {
                    YieldedCount++;
                    yield return _issue;
                }
            }
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

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }
    }
}
