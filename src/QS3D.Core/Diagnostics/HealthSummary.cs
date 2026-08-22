using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Diagnostics
{
    public sealed class HealthSummary
    {
        public HealthSummary(IEnumerable<ModelHealthIssue> issues)
        {
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            Issues = issues.ToList();
        }
        public IReadOnlyList<ModelHealthIssue> Issues { get; }
        public int Errors => Issues.Count(x => x.Severity == HealthSeverity.Error);
        public int Warnings => Issues.Count(x => x.Severity == HealthSeverity.Warning);
        public int Info => Issues.Count(x => x.Severity == HealthSeverity.Info);
        public bool IsHealthy => Errors == 0;
    }
}
