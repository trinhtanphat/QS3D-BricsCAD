using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedGridAnnotationHandleListCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedHandleTokenFailsVisible();
            CanonicalHandleListDoesNotEmitCanonicalityError();
            EmptyTokenKeepsInvalidPrecedence();
            PaddedDuplicateKeepsDuplicateVisible();
            LowercaseHexDoesNotEmitSpacingCanonicality();
        }

        private static void PaddedHandleTokenFailsVisible()
        {
            var setup = Create("PAD", "A; B;C;D;E;F");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_HANDLE_LIST_NON_CANONICAL");
            EnsureAbsent(issues, "GRID_ANNOTATION_HANDLE_INVALID", "Padded valid handle tokens must remain valid after trimming.");
        }

        private static void CanonicalHandleListDoesNotEmitCanonicalityError()
        {
            var setup = Create("CANONICAL", "A;B;C;D;E;F");
            EnsureAbsent(Inspect(setup), "GRID_ANNOTATION_HANDLE_LIST_NON_CANONICAL", "Canonical handle lists must not produce canonicality errors.");
        }

        private static void EmptyTokenKeepsInvalidPrecedence()
        {
            var setup = Create("EMPTY", "A;;C;D;E;F");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_HANDLE_INVALID");
            EnsureAbsent(issues, "GRID_ANNOTATION_HANDLE_LIST_NON_CANONICAL", "Empty handle tokens must keep existing invalid-token precedence without canonicality noise.");
        }

        private static void PaddedDuplicateKeepsDuplicateVisible()
        {
            var setup = Create("DUP", "A; A;C;D;E;F");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_HANDLE_LIST_NON_CANONICAL");
            RequireIssue(issues, setup.Element.Id, "GRID_ANNOTATION_HANDLE_DUPLICATE");
        }

        private static void LowercaseHexDoesNotEmitSpacingCanonicality()
        {
            var setup = Create("LOWER", "a;B;C;D;E;F");
            EnsureAbsent(Inspect(setup), "GRID_ANNOTATION_HANDLE_LIST_NON_CANONICAL", "Handle-list canonicality must not impose hex-letter casing beyond the writer-owned spacing contract.");
        }

        private static Setup Create(string suffix, string handles)
        {
            var project = new ProjectState("P-Grid-Handles-" + suffix, "Grid Annotation handle-list canonicality smoke");
            var element = new ProjectElement("Grid-Handles-" + suffix, ElementCategory.Grid);
            element.Properties["GeneratedGridAnnotationHandles"] = handles;
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
            throw new InvalidOperationException("Expected Grid Annotation handle-list health issue was not reported: " + code + ".");
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
