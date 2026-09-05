using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class LevelReferenceNullHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NullFloorFailsVisible();
            NullElementFailsVisible();
            ValidTopWithoutBottomStillErrors();
        }

        private static void NullFloorFailsVisible()
        {
            var project = new ProjectState("health-level-null-floor", "Level null floor");
            var beforeVersion = project.ChangeVersion;

            try
            {
                project.Floors.Add(null!);
            }
            catch (ArgumentNullException ex)
            {
                if (!string.Equals(ex.ParamName, "item", StringComparison.Ordinal))
                    throw new InvalidOperationException("Null Floor admission failed for the wrong parameter.", ex);

                if (project.ChangeVersion != beforeVersion)
                    throw new InvalidOperationException("Rejected null Floor admission unexpectedly advanced ProjectState.ChangeVersion.");
                if (project.Floors.Count != 0)
                    throw new InvalidOperationException("Rejected null Floor admission unexpectedly mutated the Floor catalog.");
                return;
            }

            throw new InvalidOperationException("Floor catalog must reject null entries at the admission boundary.");
        }

        private static void NullElementFailsVisible()
        {
            var project = new ProjectState("health-level-null-element", "Level null element");
            project.Elements.Add(null!);

            ThrowsDirect(project, "null semantic element");
            HasCompositeProviderFailure(project);
        }

        private static void ValidTopWithoutBottomStillErrors()
        {
            var project = new ProjectState("health-level-valid", "Level valid diagnostics");
            var element = new ProjectElement("E-LEVEL", ElementCategory.Beam);
            element.Properties[ProjectFloorService.TopLevelIdKey] = "L1";
            project.Elements.Add(element);

            var issues = new LevelReferenceHealthService().Inspect(project);
            if (!issues.Any(issue =>
                string.Equals(issue.Code, "TOP_LEVEL_REQUIRES_BOTTOM_LEVEL", StringComparison.Ordinal) &&
                issue.Severity == HealthSeverity.Error &&
                string.Equals(issue.ElementId, element.Id, StringComparison.Ordinal)))
                throw new InvalidOperationException("Existing TopLevel-without-BottomLevel diagnostics regressed.");
        }

        private static void ThrowsDirect(ProjectState project, string expectedMessageToken)
        {
            try
            {
                new LevelReferenceHealthService().Inspect(project);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageToken, StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException("Level Reference health failed for the wrong reason.", ex);
            }
            throw new InvalidOperationException("Level Reference health must reject malformed project entries instead of silently skipping them.");
        }

        private static void HasCompositeProviderFailure(ProjectState project)
        {
            var issues = new ComprehensiveModelHealthService().Inspect(project);
            if (issues.Any(issue =>
                string.Equals(issue.Code, "HEALTH_PROVIDER_FAILED", StringComparison.Ordinal) &&
                issue.Severity == HealthSeverity.Error &&
                issue.Message.StartsWith("LevelReferenceHealthService ", StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Composite health must surface the Level Reference provider failure.");
        }
    }
}