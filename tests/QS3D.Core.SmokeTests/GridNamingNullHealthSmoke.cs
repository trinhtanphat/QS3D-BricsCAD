using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingNullHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NullElementFailsVisible();
            NonGridStillIgnored();
            InvalidSequenceStillErrors();
        }

        private static void NullElementFailsVisible()
        {
            var project = new ProjectState("health-grid-null", "Grid null health");
            project.Elements.Add(null!);

            try
            {
                new GridNamingHealthService().Inspect(project);
            }
            catch (InvalidOperationException)
            {
                var composite = new ComprehensiveModelHealthService().Inspect(project);
                if (composite.Any(issue =>
                    string.Equals(issue.Code, "HEALTH_PROVIDER_FAILED", StringComparison.Ordinal) &&
                    issue.Severity == HealthSeverity.Error &&
                    issue.Message.StartsWith("GridNamingHealthService ", StringComparison.Ordinal)))
                    return;
                throw new InvalidOperationException("Composite health must surface the Grid Naming provider failure instead of hiding malformed project state.");
            }

            throw new InvalidOperationException("Grid Naming health must reject null semantic elements instead of silently skipping them.");
        }

        private static void NonGridStillIgnored()
        {
            var project = new ProjectState("health-grid-nongrid", "Grid non-grid health");
            var element = new ProjectElement("E-BEAM", ElementCategory.Beam);
            element.Properties[GridNamingService.GridSequenceIndexKey] = "bad";
            project.Elements.Add(element);

            if (new GridNamingHealthService().Inspect(project).Count != 0)
                throw new InvalidOperationException("Grid Naming health must continue to ignore non-Grid elements.");
        }

        private static void InvalidSequenceStillErrors()
        {
            var project = new ProjectState("health-grid-valid", "Grid valid diagnostics");
            var element = new ProjectElement("E-GRID", ElementCategory.Grid);
            element.Properties[GridNamingService.GridSequenceIndexKey] = "0";
            project.Elements.Add(element);

            var issues = new GridNamingHealthService().Inspect(project);
            if (!issues.Any(issue =>
                string.Equals(issue.Code, "GRID_SEQUENCE_INVALID", StringComparison.Ordinal) &&
                issue.Severity == HealthSeverity.Error &&
                string.Equals(issue.ElementId, element.Id, StringComparison.Ordinal)))
                throw new InvalidOperationException("Existing invalid Grid sequence diagnostics regressed.");
        }
    }
}
