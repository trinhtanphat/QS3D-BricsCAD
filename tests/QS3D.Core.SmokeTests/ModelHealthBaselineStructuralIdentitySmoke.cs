using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthBaselineStructuralIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CapturePreservesNewlineCollisionIssues();
            CompareSeparatesNewlineCollisionIssues();
            StaleMessageChangesRemainPersistent();
        }

        private static void CapturePreservesNewlineCollisionIssues()
        {
            var project = new ProjectState("P-BASELINE-STRUCTURAL-CAPTURE", "Baseline structural capture");
            var first = FirstCollisionIssue();
            var second = SecondCollisionIssue();

            var baseline = new ModelHealthBaselineService().Capture(project, new[] { first, second });

            Equal(2, baseline.Issues.Count);
            True(baseline.Issues.Any(x => string.Equals(x.ElementId, first.ElementId, StringComparison.Ordinal) && string.Equals(x.Message, first.Message, StringComparison.Ordinal)));
            True(baseline.Issues.Any(x => string.Equals(x.ElementId, second.ElementId, StringComparison.Ordinal) && string.Equals(x.Message, second.Message, StringComparison.Ordinal)));
        }

        private static void CompareSeparatesNewlineCollisionIssues()
        {
            var project = new ProjectState("P-BASELINE-STRUCTURAL-COMPARE", "Baseline structural compare");
            var service = new ModelHealthBaselineService();
            var before = service.Capture(project, new[] { FirstCollisionIssue() });
            var after = service.Capture(project, new[] { SecondCollisionIssue() });

            var diff = service.Compare(before, after);

            Equal(1, diff.NewIssues.Count);
            Equal(1, diff.ResolvedIssues.Count);
            Equal(0, diff.PersistentIssues.Count);
            True(diff.HasRegressions);
            True(diff.HasImprovements);
        }

        private static void StaleMessageChangesRemainPersistent()
        {
            var project = new ProjectState("P-BASELINE-STRUCTURAL-STALE", "Baseline structural stale");
            var service = new ModelHealthBaselineService();
            var before = service.Capture(project, new[]
            {
                new ModelHealthIssue("OUTPUT_STALE", HealthSeverity.Warning, "Before\nmessage", "E\nSTALE")
            });
            var after = service.Capture(project, new[]
            {
                new ModelHealthIssue("output_stale", HealthSeverity.Warning, "After\nmessage", "e\nstale")
            });

            var diff = service.Compare(before, after);

            Equal(0, diff.NewIssues.Count);
            Equal(0, diff.ResolvedIssues.Count);
            Equal(1, diff.PersistentIssues.Count);
        }

        private static ModelHealthIssue FirstCollisionIssue() =>
            new ModelHealthIssue("BASELINE_COLLISION", HealthSeverity.Error, "Tail\nMessage", "E");

        private static ModelHealthIssue SecondCollisionIssue() =>
            new ModelHealthIssue("BASELINE_COLLISION", HealthSeverity.Error, "Message", "E\nTail");

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("Expected condition to be true.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }
    }
}
