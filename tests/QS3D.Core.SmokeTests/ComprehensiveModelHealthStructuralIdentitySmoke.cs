using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.SmokeTests
{
    internal static class ComprehensiveModelHealthStructuralIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NewlineCollisionIssuesRemainDistinct();
            StaleMessageChangesRemainDeduplicated();
        }

        private static void NewlineCollisionIssuesRemainDistinct()
        {
            var first = new ModelHealthIssue("AGGREGATE_COLLISION", HealthSeverity.Error, "Tail\nMessage", "E");
            var second = new ModelHealthIssue("AGGREGATE_COLLISION", HealthSeverity.Error, "Message", "E\nTail");

            var merged = Merge(first, second);

            Equal(2, merged.Count);
            True(merged.Any(x => string.Equals(x.ElementId, first.ElementId, StringComparison.Ordinal) && string.Equals(x.Message, first.Message, StringComparison.Ordinal)));
            True(merged.Any(x => string.Equals(x.ElementId, second.ElementId, StringComparison.Ordinal) && string.Equals(x.Message, second.Message, StringComparison.Ordinal)));
        }

        private static void StaleMessageChangesRemainDeduplicated()
        {
            var first = new ModelHealthIssue("OUTPUT_STALE", HealthSeverity.Warning, "Before\nmessage", "E\nSTALE");
            var second = new ModelHealthIssue("output_stale", HealthSeverity.Warning, "After\nmessage", "e\nstale");

            var merged = Merge(first, second);

            Equal(1, merged.Count);
        }

        private static IReadOnlyList<ModelHealthIssue> Merge(params ModelHealthIssue[] issues)
        {
            var add = typeof(ComprehensiveModelHealthService).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Static);
            if (add == null) throw new InvalidOperationException("ComprehensiveModelHealthService.Add was not found.");
            var target = new List<ModelHealthIssue>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            add.Invoke(null, new object[] { target, seen, issues });
            return target.AsReadOnly();
        }

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
