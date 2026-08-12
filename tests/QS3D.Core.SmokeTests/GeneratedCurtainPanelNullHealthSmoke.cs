using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainPanelNullHealthSmoke
    {
        public static void Run()
        {
            DirectInspectionFailsClosed();
            ComprehensiveHealthSurfacesProviderFailure();
            EmptyProjectRemainsHealthy();
        }

        private static void DirectInspectionFailsClosed()
        {
            var project = ProjectWithNullElement("DIRECT");
            var rejected = false;
            try
            {
                new GeneratedCurtainPanelHealthService().Inspect(project);
            }
            catch (InvalidOperationException error)
            {
                rejected = error.Message.IndexOf("null semantic element", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!rejected)
                throw new Exception("Curtain Panel health did not fail closed on a null semantic element.");
        }

        private static void ComprehensiveHealthSurfacesProviderFailure()
        {
            var project = ProjectWithNullElement("COMPOSITE");
            var issues = new ComprehensiveModelHealthService().Inspect(project);
            var surfaced = issues.Any(issue =>
                issue.Code == "HEALTH_PROVIDER_FAILED" &&
                issue.Severity == HealthSeverity.Error &&
                issue.Message.IndexOf("GeneratedCurtainPanelHealthService", StringComparison.Ordinal) >= 0);
            if (!surfaced)
                throw new Exception("Comprehensive health did not surface the Curtain Panel provider failure.");
        }

        private static void EmptyProjectRemainsHealthy()
        {
            var project = new ProjectState("P-CURTAIN-PANEL-NULL-EMPTY", "Curtain panel empty health");
            var issues = new GeneratedCurtainPanelHealthService().Inspect(project);
            if (issues.Count != 0)
                throw new Exception("Empty project unexpectedly produced Curtain Panel health issues.");
        }

        private static ProjectState ProjectWithNullElement(string suffix)
        {
            var project = new ProjectState("P-CURTAIN-PANEL-NULL-" + suffix, "Curtain panel null health");
            project.Elements.Add(null!);
            return project;
        }
    }
}
