using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarFabricationQualificationNullHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            EnabledQualificationRejectsNullElementDirectly();
            CompositeHealthSurfacesProviderFailure();
            ValidQualifiedProjectWithoutOutputKeepsExistingDiagnosis();
        }

        private static void EnabledQualificationRejectsNullElementDirectly()
        {
            var project = QualifiedProject("FAB-NULL-DIRECT");
            project.Elements.Add(null!);

            try
            {
                new RebarFabricationQualificationHealthService().Inspect(project);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("null semantic element", StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException("Fabrication qualification rejected malformed project state for the wrong reason.", ex);
            }

            throw new InvalidOperationException("Enabled fabrication qualification must reject a null semantic element before output classification.");
        }

        private static void CompositeHealthSurfacesProviderFailure()
        {
            var project = QualifiedProject("FAB-NULL-COMPOSITE");
            project.Elements.Add(null!);

            var issues = new ComprehensiveModelHealthService().Inspect(project);
            if (issues.Any(issue =>
                string.Equals(issue.Code, "HEALTH_PROVIDER_FAILED", StringComparison.Ordinal) &&
                issue.Severity == HealthSeverity.Error &&
                issue.Message.StartsWith("RebarFabricationQualificationHealthService ", StringComparison.Ordinal)))
                return;

            throw new InvalidOperationException("Comprehensive health must surface malformed fabrication qualification state as a provider failure.");
        }

        private static void ValidQualifiedProjectWithoutOutputKeepsExistingDiagnosis()
        {
            var project = QualifiedProject("FAB-NULL-VALID");
            var issues = new RebarFabricationQualificationHealthService().Inspect(project);

            if (issues.Count != 1 ||
                !string.Equals(issues[0].Code, "REBAR_FAB_OUTPUT_MISSING", StringComparison.Ordinal) ||
                issues[0].Severity != HealthSeverity.Error)
                throw new InvalidOperationException("Valid fabrication qualification without generated rebar output changed its existing diagnosis.");
        }

        private static ProjectState QualifiedProject(string id)
        {
            var project = new ProjectState(id, "Fabrication null health smoke");
            project.Metadata[RebarFabricationQualificationHealthService.RequireQualificationMetadataKey] = "true";
            project.Metadata[RebarFabricationQualificationHealthService.StandardCodeMetadataKey] = "STANDARD-X:2026";
            project.Metadata[RebarFabricationQualificationHealthService.DetailingRevisionMetadataKey] = "R1";
            return project;
        }
    }
}
