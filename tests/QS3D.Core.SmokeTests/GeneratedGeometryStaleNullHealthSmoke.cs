using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedGeometryStaleNullHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NullElementFailsVisible();
            ValidStaleElementStillWarns();
        }

        private static void NullElementFailsVisible()
        {
            var project = new ProjectState("health-stale-null", "Stale null health");
            project.Elements.Add(null!);

            try
            {
                new GeneratedGeometryStaleHealthService().Inspect(project);
            }
            catch (InvalidOperationException)
            {
                var composite = new ComprehensiveModelHealthService().Inspect(project);
                if (composite.Any(issue =>
                    string.Equals(issue.Code, "HEALTH_PROVIDER_FAILED", StringComparison.Ordinal) &&
                    issue.Severity == HealthSeverity.Error &&
                    issue.Message.StartsWith("GeneratedGeometryStaleHealthService ", StringComparison.Ordinal)))
                    return;
                throw new InvalidOperationException("Composite health must surface the stale-geometry provider failure instead of hiding malformed project state.");
            }

            throw new InvalidOperationException("Generated-geometry stale diagnostics must reject null semantic elements instead of silently skipping them.");
        }

        private static void ValidStaleElementStillWarns()
        {
            var project = new ProjectState("health-stale-valid", "Stale valid health");
            var element = new ProjectElement("E-STALE", ElementCategory.Beam);
            element.Properties["GeneratedSolidHandle"] = "ABCD";
            element.MarkGeneratedGeometryStale("smoke");
            project.Elements.Add(element);

            var issues = new GeneratedGeometryStaleHealthService().Inspect(project);
            if (!issues.Any(issue =>
                string.Equals(issue.Code, "GENERATED_SOLID_STALE", StringComparison.Ordinal) &&
                issue.Severity == HealthSeverity.Warning &&
                string.Equals(issue.ElementId, element.Id, StringComparison.Ordinal)))
                throw new InvalidOperationException("Valid generated-solid stale warning behavior regressed.");
        }
    }
}
