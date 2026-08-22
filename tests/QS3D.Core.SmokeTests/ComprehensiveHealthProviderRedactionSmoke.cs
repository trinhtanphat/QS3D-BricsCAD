using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.SmokeTests
{
    internal static class ComprehensiveHealthProviderRedactionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            const string sentinel = "PRIVATE_PROVIDER_FAILURE_SENTINEL";
            var target = new List<ModelHealthIssue>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            Func<IEnumerable<ModelHealthIssue>> provider = () => throw new FormatException(sentinel);

            var addSafely = typeof(ComprehensiveModelHealthService).GetMethod("AddSafely", BindingFlags.NonPublic | BindingFlags.Static);
            if (addSafely == null) throw new Exception("Comprehensive health provider isolation helper was not found.");
            addSafely.Invoke(null, new object[] { target, seen, "SentinelProvider", provider });

            var failure = target.Single(x => string.Equals(x.Code, "HEALTH_PROVIDER_FAILED", StringComparison.Ordinal));
            if (failure.Severity != HealthSeverity.Error)
                throw new Exception("Provider data failure must remain an Error.");
            if (failure.Message.IndexOf("SentinelProvider", StringComparison.Ordinal) < 0)
                throw new Exception("Provider failure must retain provider identity.");
            if (failure.Message.IndexOf(sentinel, StringComparison.Ordinal) >= 0)
                throw new Exception("Provider failure must not expose raw exception detail.");
        }
    }
}
