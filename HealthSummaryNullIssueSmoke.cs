using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.SmokeTests
{
    internal static class HealthSummaryNullIssueSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NullIssueFailsClosed();
            ReadinessSemanticsRemainStable();
        }

        private static void NullIssueFailsClosed()
        {
            try
            {
                _ = new HealthSummary(new ModelHealthIssue[] { null! });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("null diagnostic issue", StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException("HealthSummary rejected a null issue for the wrong reason.", ex);
            }

            throw new InvalidOperationException("HealthSummary must not convert a null diagnostic stream into a false-clean summary.");
        }

        private static void ReadinessSemanticsRemainStable()
        {
            var empty = new HealthSummary(Array.Empty<ModelHealthIssue>());
            if (!empty.IsHealthy || !empty.IsReleaseReady || empty.Errors != 0 || empty.Warnings != 0 || empty.Info != 0)
                throw new InvalidOperationException("Empty HealthSummary readiness semantics regressed.");

            var info = new HealthSummary(new[] { new ModelHealthIssue("INFO", HealthSeverity.Info, "info") });
            if (!info.IsHealthy || !info.IsReleaseReady || info.Info != 1)
                throw new InvalidOperationException("Info-only HealthSummary readiness semantics regressed.");

            var warning = new HealthSummary(new[] { new ModelHealthIssue("WARN", HealthSeverity.Warning, "warning") });
            if (!warning.IsHealthy || warning.IsReleaseReady || warning.Warnings != 1)
                throw new InvalidOperationException("Warning HealthSummary readiness semantics regressed.");

            var error = new HealthSummary(new[] { new ModelHealthIssue("ERROR", HealthSeverity.Error, "error") });
            if (error.IsHealthy || error.IsReleaseReady || error.Errors != 1)
                throw new InvalidOperationException("Error HealthSummary readiness semantics regressed.");
        }
    }
}
