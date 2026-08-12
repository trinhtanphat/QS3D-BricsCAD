using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedSemanticTagHandleCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedHandleFailsVisible();
            LowercaseCanonicalHandleRemainsAccepted();
            EmptyDelimiterTokenRemainsInvalid();
        }

        private static void PaddedHandleFailsVisible()
        {
            var setup = Create("PAD", " A ");
            var issues = new GeneratedSemanticTagHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "SEMANTIC_TAG_HANDLE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "SEMANTIC_TAG_HANDLE_INVALID");
        }

        private static void LowercaseCanonicalHandleRemainsAccepted()
        {
            var setup = Create("LOWER", "a");
            var issues = new GeneratedSemanticTagHealthService().Inspect(setup.Project);

            ForbidIssue(issues, setup.Element.Id, "SEMANTIC_TAG_HANDLE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "SEMANTIC_TAG_HANDLE_INVALID");
        }

        private static void EmptyDelimiterTokenRemainsInvalid()
        {
            var setup = Create("EMPTY", "A;;B");
            var issues = new GeneratedSemanticTagHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "SEMANTIC_TAG_HANDLE_INVALID");
        }

        private static Setup Create(string suffix, string handles)
        {
            var project = new ProjectState("P-SEM-TAG-CANON-" + suffix, "Generated Semantic Tag handle canonicality");
            var element = new ProjectElement("E-SEM-TAG-CANON-" + suffix, ElementCategory.Beam);
            element.Properties[GeneratedSemanticTagHealthService.HandlesKey] = handles;
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedSemanticTagHandleCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedSemanticTagHandleCanonicalitySmoke unexpected issue was reported: " + code + ".");
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
