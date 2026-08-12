using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedGridAnnotationOwnerCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedVersionFailsVisible();
            CaseVariantProjectOwnerFailsVisible();
            PaddedElementOwnerFailsVisible();
            PaddedWrongProjectKeepsMismatchVisible();
            CanonicalOwnersDoNotEmitCanonicalityErrors();
        }

        private static void PaddedVersionFailsVisible()
        {
            var setup = Create("VERSION");
            setup.Element.Properties["GeneratedGridAnnotationOwnershipVersion"] = " 1 ";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_OWNERSHIP_VERSION_NON_CANONICAL");
            EnsureAbsent(issues, "GRID_ANNOTATION_OWNERSHIP_VERSION", "Canonical version alias must not become unsupported-version evidence.");
        }

        private static void CaseVariantProjectOwnerFailsVisible()
        {
            var setup = Create("PROJECT");
            setup.Element.Properties["GeneratedGridAnnotationOwnerProjectId"] = setup.Project.ProjectId.ToLowerInvariant();
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_PROJECT_OWNER_NON_CANONICAL");
            EnsureAbsent(issues, "GRID_ANNOTATION_PROJECT_MISMATCH", "Case-only project alias must remain the same owner while failing canonical spelling.");
        }

        private static void PaddedElementOwnerFailsVisible()
        {
            var setup = Create("ELEMENT");
            setup.Element.Properties["GeneratedGridAnnotationOwnerElementId"] = " " + setup.Element.Id + " ";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_ELEMENT_OWNER_NON_CANONICAL");
            EnsureAbsent(issues, "GRID_ANNOTATION_ELEMENT_MISMATCH", "Padded element alias must remain the same owner while failing canonical spelling.");
        }

        private static void PaddedWrongProjectKeepsMismatchVisible()
        {
            var setup = Create("MISMATCH");
            setup.Element.Properties["GeneratedGridAnnotationOwnerProjectId"] = " WRONG-PROJECT ";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_PROJECT_MISMATCH");
        }

        private static void CanonicalOwnersDoNotEmitCanonicalityErrors()
        {
            var setup = Create("CANONICAL");
            var issues = Inspect(setup);
            if (issues.Any(x =>
                string.Equals(x.Code, "GRID_ANNOTATION_OWNERSHIP_VERSION_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "GRID_ANNOTATION_PROJECT_OWNER_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "GRID_ANNOTATION_ELEMENT_OWNER_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Canonical Grid Annotation owner metadata must not produce canonicality errors.");
        }

        private static Setup Create(string suffix)
        {
            var project = new ProjectState("P-Grid-Owner-" + suffix, "Grid Annotation owner canonicality smoke");
            var element = new ProjectElement("Grid-Owner-" + suffix, ElementCategory.Grid);
            element.Properties["GeneratedGridAnnotationHandles"] = "A;B;C;D;E;F";
            element.Properties[GridNamingService.GridLabelKey] = "G1";
            element.Properties["GeneratedGridAnnotationLabel"] = "G1";
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
            throw new InvalidOperationException("Expected Grid Annotation owner health issue was not reported: " + code + ".");
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
