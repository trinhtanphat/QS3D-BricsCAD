using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedGridAnnotationBuiltLabelCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedMatchingSnapshotFailsCanonicalityWithoutStale();
            PaddedDifferentSnapshotKeepsStaleVisible();
            CanonicalMatchingSnapshotIsClean();
            CanonicalDifferentSnapshotIsOnlyStale();
        }

        private static void PaddedMatchingSnapshotFailsCanonicalityWithoutStale()
        {
            var setup = Create("PAD-MATCH", " G1 ");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_BUILT_LABEL_NON_CANONICAL");
            EnsureAbsent(issues, "GRID_ANNOTATION_LABEL_STALE", "Matching built label aliases must not become stale after normalization.");
        }

        private static void PaddedDifferentSnapshotKeepsStaleVisible()
        {
            var setup = Create("PAD-STALE", " G2 ");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_BUILT_LABEL_NON_CANONICAL");
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_LABEL_STALE");
        }

        private static void CanonicalMatchingSnapshotIsClean()
        {
            var setup = Create("CANONICAL", "G1");
            var issues = Inspect(setup);
            EnsureAbsent(issues, "GRID_ANNOTATION_BUILT_LABEL_NON_CANONICAL", "Canonical built-label snapshots must not produce canonicality errors.");
            EnsureAbsent(issues, "GRID_ANNOTATION_LABEL_STALE", "Matching canonical built-label snapshots must not be stale.");
        }

        private static void CanonicalDifferentSnapshotIsOnlyStale()
        {
            var setup = Create("CANONICAL-STALE", "G2");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_LABEL_STALE");
            EnsureAbsent(issues, "GRID_ANNOTATION_BUILT_LABEL_NON_CANONICAL", "Canonical but different built labels must remain stale without canonicality noise.");
        }

        private static Setup Create(string suffix, string builtLabel)
        {
            var project = new ProjectState("P-Grid-Label-" + suffix, "Grid Annotation built-label canonicality smoke");
            var element = new ProjectElement("Grid-Label-" + suffix, ElementCategory.Grid);
            element.Properties["GeneratedGridAnnotationHandles"] = "A;B;C;D;E;F";
            element.Properties[GridNamingService.GridLabelKey] = "G1";
            element.Properties["GeneratedGridAnnotationLabel"] = builtLabel;
            element.Properties["GeneratedGridAnnotationOwnerProjectId"] = project.ProjectId;
            element.Properties["GeneratedGridAnnotationOwnerElementId"] = element.Id;
            element.Properties["GeneratedGridAnnotationOwnershipVersion"] = "1";
            element.Properties["GridBubbleRadiusM"] = "0.25";
            element.Properties["GridTextHeightM"] = "0.18";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static System.Collections.Generic.IReadOnlyList<ModelHealthIssue> Inspect(Setup setup) =>
            new GeneratedGridAnnotationHealthService().Inspect(setup.Project);

        private static void RequireIssue(System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Grid Annotation built-label health issue was not reported: " + code + ".");
        }

        private static void EnsureAbsent(System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues, string code, string message)
        {
            if (issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new InvalidOperationException(message);
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
