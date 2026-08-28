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
            NullIssueEntriesFailClosed();
            UndefinedSeverityFailsAtIssueBoundary();
            ModelHealthBaselineKnownCountContractSmoke.Run();
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

        private static void NullIssueEntriesFailClosed()
        {
            try
            {
                _ = new HealthSummary(new ModelHealthIssue[]
                {
                    null!,
                    new ModelHealthIssue("INFO", HealthSeverity.Info, "info")
                });
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception("Null health issue entries must fail closed instead of producing a false-clean summary.");
        }

        private static void UndefinedSeverityFailsAtIssueBoundary()
        {
            try
            {
                _ = new ModelHealthIssue("CORRUPT", (HealthSeverity)999, "corrupt severity");
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }

            throw new Exception("Undefined health severity must fail closed at issue construction.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}
