using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticTagHandleIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NumericAliasDuplicateFailsVisible();
            SourceAliasFailsVisible();
            DistinctHandlesStayDistinct();
            PrefixedHexRemainsInvalid();
        }

        private static void NumericAliasDuplicateFailsVisible()
        {
            var setup = Create("DUPLICATE", "A;0A");
            var issues = Inspect(setup);
            Require(issues, setup.Element.Id, "SEMANTIC_TAG_HANDLE_DUPLICATE", HealthSeverity.Error);
        }

        private static void SourceAliasFailsVisible()
        {
            var setup = Create("SOURCE", "0A");
            setup.Element.SourceHandles.Add("A");
            var issues = Inspect(setup);
            Require(issues, setup.Element.Id, "SEMANTIC_TAG_HANDLE_IN_SOURCE", HealthSeverity.Error);
        }

        private static void DistinctHandlesStayDistinct()
        {
            var setup = Create("DISTINCT", "A;B");
            var issues = Inspect(setup);
            Forbid(issues, setup.Element.Id, "SEMANTIC_TAG_HANDLE_DUPLICATE");
        }

        private static void PrefixedHexRemainsInvalid()
        {
            var setup = Create("PREFIX", "0xA");
            var issues = Inspect(setup);
            Require(issues, setup.Element.Id, "SEMANTIC_TAG_HANDLE_INVALID", HealthSeverity.Error);
            Forbid(issues, setup.Element.Id, "SEMANTIC_TAG_HANDLE_DUPLICATE");
        }

        private static Setup Create(string suffix, string handles)
        {
            var project = new ProjectState("P-SEMANTIC-TAG-HANDLE-" + suffix, "Semantic Tag handle identity");
            var element = new ProjectElement("E-SEMANTIC-TAG-HANDLE-" + suffix, ElementCategory.Beam);
            element.Properties[GeneratedSemanticTagHealthService.HandlesKey] = handles;
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(Setup setup) =>
            new GeneratedSemanticTagHealthService().Inspect(setup.Project);

        private static void Require(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code, HealthSeverity severity)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == severity &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("SemanticTagHandleIdentitySmoke expected issue was not reported: " + code + ".");
        }

        private static void Forbid(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("SemanticTagHandleIdentitySmoke reported unexpected issue: " + code + ".");
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
