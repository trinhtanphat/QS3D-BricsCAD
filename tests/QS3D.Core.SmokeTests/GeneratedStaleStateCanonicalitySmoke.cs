using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedStaleStateCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedStaleStateFailsVisibleWithSnapshot();
            ExactStaleStateKeepsExistingWarning();
            PaddedStaleStateWithoutSnapshotKeepsMalformedError();
        }

        private static void PaddedStaleStateFailsVisibleWithSnapshot()
        {
            var setup = Create("PAD", " stale ", true);
            var issues = new GeneratedGeometryStaleHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "GENERATED_STALE_STATE_NON_CANONICAL", HealthSeverity.Error);
            ForbidIssue(issues, setup.Element.Id, "GENERATED_STALE_METADATA_INVALID");
        }

        private static void ExactStaleStateKeepsExistingWarning()
        {
            var setup = Create("EXACT", "stale", true);
            var issues = new GeneratedGeometryStaleHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "GENERATED_SOLID_STALE", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "GENERATED_STALE_STATE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "GENERATED_STALE_METADATA_INVALID");
        }

        private static void PaddedStaleStateWithoutSnapshotKeepsMalformedError()
        {
            var setup = Create("MISSING", " stale ", false);
            var issues = new GeneratedGeometryStaleHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "GENERATED_STALE_STATE_NON_CANONICAL", HealthSeverity.Error);
            RequireIssue(issues, setup.Element.Id, "GENERATED_STALE_METADATA_INVALID", HealthSeverity.Error);
        }

        private static Setup Create(string suffix, string state, bool includeSnapshot)
        {
            var project = new ProjectState("P-STALE-STATE-CANON-" + suffix, "Generated stale state canonicality");
            var element = new ProjectElement("E-STALE-STATE-CANON-" + suffix, ElementCategory.Beam);
            element.Properties["GeneratedSolidHandle"] = "A";
            element.Properties[ProjectElement.GeneratedSolidStateKey] = state;
            if (includeSnapshot)
                element.Properties[ProjectElement.GeneratedSolidStaleSnapshotKey] = "A";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code, HealthSeverity severity)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == severity &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedStaleStateCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedStaleStateCanonicalitySmoke unexpected issue was reported: " + code + ".");
        }

        private sealed class Setup
        {
            public Setup(ProjectState project, ProjectElement element)
            {
                Project = project;
                Element = element;
            }

            public ProjectState Project { get; }
            public ProjectElement Element { get; }
        }
    }
}
