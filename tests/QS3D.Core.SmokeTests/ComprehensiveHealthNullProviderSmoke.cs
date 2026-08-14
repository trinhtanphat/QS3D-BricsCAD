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
            Func<IEnumerable<ModelHealthIssue>> provider = () => new ModelHealthIssue[] { null! };

            var providerType = typeof(ComprehensiveModelHealthService).GetNestedType("DiagnosticProvider", BindingFlags.NonPublic);
            if (providerType == null) throw new Exception("Comprehensive health diagnostic provider wrapper was not found.");

            var providerConstructor = providerType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string), typeof(Func<IEnumerable<ModelHealthIssue>>), typeof(bool) },
                modifiers: null);
            if (providerConstructor == null) throw new Exception("Comprehensive health diagnostic provider constructor was not found.");

            var providerInstance = providerConstructor.Invoke(new object[] { "NullIssueProvider", provider, true });
            var executeProvider = typeof(ComprehensiveModelHealthService).GetMethod("ExecuteProvider", BindingFlags.NonPublic | BindingFlags.Static);
            if (executeProvider == null) throw new Exception("Comprehensive health provider execution helper was not found.");

            var result = executeProvider.Invoke(null, new[] { providerInstance });
            if (result == null) throw new Exception("Comprehensive health provider execution returned no result.");

            var issuesProperty = result.GetType().GetProperty("Issues", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var issues = issuesProperty?.GetValue(result) as IEnumerable<ModelHealthIssue>;
            if (issues == null) throw new Exception("Comprehensive health provider result issues were not found.");

            var failure = issues.Single(x => string.Equals(x.Code, "HEALTH_PROVIDER_FAILED", StringComparison.Ordinal));
            if (failure.Severity != HealthSeverity.Error)
                throw new Exception("Null provider issue must fail visible as an Error.");
            if (failure.Message.IndexOf("NullIssueProvider", StringComparison.Ordinal) < 0)
                throw new Exception("Null provider issue failure must retain provider identity.");
        }
    }
}
