using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.SmokeTests
{
    internal static class ComprehensiveHealthNullProviderSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var target = new List<ModelHealthIssue>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            Func<IEnumerable<ModelHealthIssue>> provider = () => new ModelHealthIssue[] { null! };

            var addSafely = typeof(ComprehensiveModelHealthService).GetMethod("AddSafely", BindingFlags.NonPublic | BindingFlags.Static);
            if (addSafely == null) throw new Exception("Comprehensive health provider isolation helper was not found.");
            addSafely.Invoke(null, new object[] { target, seen, "NullIssueProvider", provider });

            var failure = target.Single(x => string.Equals(x.Code, "HEALTH_PROVIDER_FAILED", StringComparison.Ordinal));
            if (failure.Severity != HealthSeverity.Error)
                throw new Exception("Null provider issue must fail visible as an Error.");
            if (failure.Message.IndexOf("NullIssueProvider", StringComparison.Ordinal) < 0)
                throw new Exception("Null provider issue failure must retain provider identity.");
        }
    }
}
