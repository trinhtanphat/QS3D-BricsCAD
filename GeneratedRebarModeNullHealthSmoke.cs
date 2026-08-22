using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedRebarModeNullHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NullElementFailsVisible();
            ValidMissingModeStillWarns();
        }

        private static void NullElementFailsVisible()
        {
            var project = new ProjectState("health-rebar-mode-null", "Rebar mode null health");
            project.Elements.Add(null!);

            try
            {
                new GeneratedRebarModeHealthService().Inspect(project);
            }
            catch (InvalidOperationException)
            {
                var composite = new ComprehensiveModelHealthService().Inspect(project);
                if (composite.Any(issue =>
                    string.Equals(issue.Code, "HEALTH_PROVIDER_FAILED", StringComparison.Ordinal) &&
                    issue.Severity == HealthSeverity.Error &&
                    issue.Message.StartsWith("GeneratedRebarModeHealthService ", StringComparison.Ordinal)))
                    return;
                throw new InvalidOperationException("Composite health must surface the rebar-mode provider failure instead of hiding malformed project state.");
            }

            throw new InvalidOperationException("Generated-rebar mode diagnostics must reject null semantic elements instead of silently skipping them.");
        }

        private static void ValidMissingModeStillWarns()
        {
            var project = new ProjectState("health-rebar-mode-valid", "Rebar mode valid health");
            var element = new ProjectElement("E-REBAR-MODE", ElementCategory.Column);
            element.Properties["GeneratedRebarHandles"] = "ABCD";
            project.Elements.Add(element);

            var issues = new GeneratedRebarModeHealthService().Inspect(project);
            if (!issues.Any(issue =>
                string.Equals(issue.Code, "GENERATED_REBAR_MODE_MISSING", StringComparison.Ordinal) &&
                issue.Severity == HealthSeverity.Warning &&
                string.Equals(issue.ElementId, element.Id, StringComparison.Ordinal)))
                throw new InvalidOperationException("Valid GeneratedRebarMode missing-mode warning behavior regressed.");
        }
    }
}
