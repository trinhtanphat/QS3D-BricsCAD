using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthGeneratedSolidOwnershipCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedVersionFailsVisible();
            PaddedProjectOwnerFailsVisible();
            PaddedElementOwnerFailsVisible();
            PaddedWrongProjectKeepsMismatchVisible();
            CanonicalOwnershipDoesNotEmitCanonicalityErrors();
        }

        private static void PaddedVersionFailsVisible()
        {
            var setup = Create("VERSION-PAD");
            setup.Element.Properties["GeneratedSolidOwnershipVersion"] = " 1 ";
            RequireIssue(setup.Project, setup.Element.Id, "GENERATED_OWNERSHIP_VERSION_NON_CANONICAL");
        }

        private static void PaddedProjectOwnerFailsVisible()
        {
            var setup = Create("PROJECT-PAD");
            setup.Element.Properties["GeneratedSolidOwnerProjectId"] = " " + setup.Project.ProjectId + " ";
            RequireIssue(setup.Project, setup.Element.Id, "GENERATED_PROJECT_OWNER_NON_CANONICAL");
        }

        private static void PaddedElementOwnerFailsVisible()
        {
            var setup = Create("ELEMENT-PAD");
            setup.Element.Properties["GeneratedSolidOwnerElementId"] = " " + setup.Element.Id + " ";
            RequireIssue(setup.Project, setup.Element.Id, "GENERATED_ELEMENT_OWNER_NON_CANONICAL");
        }

        private static void PaddedWrongProjectKeepsMismatchVisible()
        {
            var setup = Create("PROJECT-MISMATCH");
            setup.Element.Properties["GeneratedSolidOwnerProjectId"] = " WRONG-PROJECT ";
            var issues = new ModelHealthService().Inspect(setup.Project);
            RequireIssue(issues, setup.Element.Id, "GENERATED_PROJECT_OWNER_NON_CANONICAL");
            RequireIssue(issues, setup.Element.Id, "GENERATED_PROJECT_MISMATCH");
        }

        private static void CanonicalOwnershipDoesNotEmitCanonicalityErrors()
        {
            var setup = Create("CANONICAL");
            var issues = new ModelHealthService().Inspect(setup.Project);
            if (issues.Any(x =>
                string.Equals(x.Code, "GENERATED_OWNERSHIP_VERSION_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "GENERATED_PROJECT_OWNER_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "GENERATED_ELEMENT_OWNER_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Canonical Generated Solid ownership metadata must not produce canonicality errors.");
        }

        private static Setup Create(string suffix)
        {
            var project = new ProjectState("P-GSOLID-OWNER-" + suffix, "Generated Solid ownership canonicality smoke");
            var element = new ProjectElement("E-GSOLID-OWNER-" + suffix, ElementCategory.Grid);
            element.Properties["GeneratedSolidHandle"] = "A";
            element.Properties["GeneratedSolidCategory"] = ElementCategory.Grid.ToString();
            element.Properties["GeneratedSolidOwnershipVersion"] = "1";
            element.Properties["GeneratedSolidOwnerProjectId"] = project.ProjectId;
            element.Properties["GeneratedSolidOwnerElementId"] = element.Id;
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void RequireIssue(ProjectState project, string elementId, string code)
        {
            RequireIssue(new ModelHealthService().Inspect(project), elementId, code);
        }

        private static void RequireIssue(System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Generated Solid ownership health issue was not reported: " + code + ".");
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
