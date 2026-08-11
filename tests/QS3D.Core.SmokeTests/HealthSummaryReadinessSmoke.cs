using System;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.SmokeTests
{
    internal static class HealthSummaryReadinessSmoke
    {
        public static void Run()
        {
            WarningIsHealthyButNotReleaseReady();
            ErrorBlocksHealthAndRelease();
            InfoOnlyIsReleaseReady();
            NullIssueEntriesAreIgnored();
            UndefinedSeverityFailsClosed();
        }

        private static void WarningIsHealthyButNotReleaseReady()
        {
            var summary = new HealthSummary(new[]
            {
                new ModelHealthIssue("WARN", HealthSeverity.Warning, "warning")
            });
            Require(summary.IsHealthy, "Warning-only summary must preserve existing IsHealthy semantics.");
            Require(!summary.IsReleaseReady, "Warning-only summary must block release readiness.");
            Require(summary.Errors == 0 && summary.Warnings == 1, "Warning counters are inconsistent.");
        }

        private static void ErrorBlocksHealthAndRelease()
        {
            var summary = new HealthSummary(new[]
            {
                new ModelHealthIssue("ERR", HealthSeverity.Error, "error")
            });
            Require(!summary.IsHealthy, "Error must block IsHealthy.");
            Require(!summary.IsReleaseReady, "Error must block release readiness.");
            Require(summary.Errors == 1, "Error counter is inconsistent.");
        }

        private static void InfoOnlyIsReleaseReady()
        {
            var summary = new HealthSummary(new[]
            {
                new ModelHealthIssue("INFO", HealthSeverity.Info, "info")
            });
            Require(summary.IsHealthy, "Info-only summary must be healthy.");
            Require(summary.IsReleaseReady, "Info-only summary must be release-ready.");
            Require(summary.Info == 1, "Info counter is inconsistent.");
        }

        private static void NullIssueEntriesAreIgnored()
        {
            var summary = new HealthSummary(new ModelHealthIssue[]
            {
                null!,
                new ModelHealthIssue("INFO", HealthSeverity.Info, "info")
            });
            Require(summary.Issues.Count == 1, "Null issue entries should not poison a read summary.");
            Require(summary.IsReleaseReady, "Ignoring a null issue entry should preserve the valid info-only readiness result.");
        }

        private static void UndefinedSeverityFailsClosed()
        {
            var corruptIssue = new ModelHealthIssue("CORRUPT", (HealthSeverity)999, "corrupt severity");
            try
            {
                _ = new HealthSummary(new[] { corruptIssue });
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception("Undefined health severity must fail closed before readiness is calculated.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}
