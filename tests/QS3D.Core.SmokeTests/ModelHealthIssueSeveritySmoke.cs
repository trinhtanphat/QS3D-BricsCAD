using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthIssueSeveritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            foreach (HealthSeverity severity in Enum.GetValues(typeof(HealthSeverity)))
            {
                var issue = new ModelHealthIssue("TEST", severity, "test");
                if (issue.Severity != severity)
                    throw new Exception("Defined Model Health severity was not preserved.");
            }

            try
            {
                _ = new ModelHealthIssue("TEST", (HealthSeverity)123, "test");
                throw new Exception("Undefined Model Health severity must be rejected.");
            }
            catch (ArgumentOutOfRangeException ex) when (string.Equals(ex.ParamName, "severity", StringComparison.Ordinal))
            {
            }
        }
    }
}
