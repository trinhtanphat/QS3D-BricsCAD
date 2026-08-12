using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedGridAnnotationSizingCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedRadiusFailsVisible();
            TrailingZeroTextHeightFailsVisible();
            ScientificRadiusFailsVisible();
            InvalidRadiusKeepsInvalidPrecedence();
            CanonicalRatioViolationKeepsRatioDiagnostic();
            CanonicalSizingDoesNotEmitCanonicalityErrors();
        }

        private static void PaddedRadiusFailsVisible()
        {
            var setup = Create("RADIUS-PAD", " 0.25 ", "0.18");
            RequireIssue(Inspect(setup), setup.Element.Id, "GRID_ANNOTATION_BUBBLE_RADIUS_NON_CANONICAL");
        }

        private static void TrailingZeroTextHeightFailsVisible()
        {
            var setup = Create("TEXT-ZERO", "0.25", "0.180");
            RequireIssue(Inspect(setup), setup.Element.Id, "GRID_ANNOTATION_TEXT_HEIGHT_NON_CANONICAL");
        }

        private static void ScientificRadiusFailsVisible()
        {
            var setup = Create("RADIUS-SCI", "2.5E-1", "0.18");
            RequireIssue(Inspect(setup), setup.Element.Id, "GRID_ANNOTATION_BUBBLE_RADIUS_NON_CANONICAL");
        }

        private static void InvalidRadiusKeepsInvalidPrecedence()
        {
            var setup = Create("RADIUS-INVALID", "NaN", "0.18");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_BUBBLE_RADIUS_INVALID");
            EnsureAbsent(issues, "GRID_ANNOTATION_BUBBLE_RADIUS_NON_CANONICAL", "Invalid radius must not produce canonicality evidence before numeric validity is established.");
        }

        private static void CanonicalRatioViolationKeepsRatioDiagnostic()
        {
            var setup = Create("RATIO", "0.25", "0.46");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_TEXT_TOO_LARGE");
            EnsureAbsent(issues, "GRID_ANNOTATION_BUBBLE_RADIUS_NON_CANONICAL", "Canonical radius must stay canonical during ratio validation.");
            EnsureAbsent(issues, "GRID_ANNOTATION_TEXT_HEIGHT_NON_CANONICAL", "Canonical text height must stay canonical during ratio validation.");
        }

        private static void CanonicalSizingDoesNotEmitCanonicalityErrors()
        {
            var setup = Create("CANONICAL", "0.25", "0.18");
            var issues = Inspect(setup);
            EnsureAbsent(issues, "GRID_ANNOTATION_BUBBLE_RADIUS_NON_CANONICAL", "Canonical bubble radius must not produce canonicality evidence.");
            EnsureAbsent(issues, "GRID_ANNOTATION_TEXT_HEIGHT_NON_CANONICAL", "Canonical text height must not produce canonicality evidence.");
        }

        private static Setup Create(string suffix, string radius, string textHeight)
        {
            var project = new ProjectState("P-Grid-Size-" + suffix, "Grid Annotation sizing canonicality smoke");
            var element = new ProjectElement("Grid-Size-" + suffix, ElementCategory.Grid);
            element.Properties["GeneratedGridAnnotationHandles"] = "A;B;C;D;E;F";
            element.Properties[GridNamingService.GridLabelKey] = "G1";
            element.Properties["GeneratedGridAnnotationLabel"] = "G1";
            element.Properties["GeneratedGridAnnotationOwnerProjectId"] = project.ProjectId;
            element.Properties["GeneratedGridAnnotationOwnerElementId"] = element.Id;
            element.Properties["GeneratedGridAnnotationOwnershipVersion"] = "1";
            element.Properties["GridBubbleRadiusM"] = radius;
            element.Properties["GridTextHeightM"] = textHeight;
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
            throw new InvalidOperationException("Expected Grid Annotation sizing health issue was not reported: " + code + ".");
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
