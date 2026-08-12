using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticTagOwnershipVersionCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedVersionFailsVisible();
            MissingVersionKeepsInvalidPrecedence();
            UnsupportedVersionKeepsInvalidPrecedence();
            CanonicalVersionRemainsClean();
            NoHandlesDoesNotValidateVersion();
        }

        private static void PaddedVersionFailsVisible()
        {
            var setup = Create("PAD", " 1 ", true);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "SEMANTIC_TAG_OWNERSHIP_VERSION_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "SEMANTIC_TAG_OWNERSHIP_VERSION_INVALID", "Padded current version must retain its existing semantic version meaning.");
        }

        private static void MissingVersionKeepsInvalidPrecedence()
        {
            var setup = Create("MISSING", null, true);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "SEMANTIC_TAG_OWNERSHIP_VERSION_INVALID", HealthSeverity.Error);
            EnsureAbsent(issues, "SEMANTIC_TAG_OWNERSHIP_VERSION_NON_CANONICAL", "Missing ownership version must fail before canonicality.");
        }

        private static void UnsupportedVersionKeepsInvalidPrecedence()
        {
            var setup = Create("UNSUPPORTED", "2", true);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "SEMANTIC_TAG_OWNERSHIP_VERSION_INVALID", HealthSeverity.Error);
            EnsureAbsent(issues, "SEMANTIC_TAG_OWNERSHIP_VERSION_NON_CANONICAL", "Unsupported ownership version must remain invalid.");
        }

        private static void CanonicalVersionRemainsClean()
        {
            var setup = Create("CANONICAL", GeneratedSemanticTagHealthService.OwnershipVersion, true);
            var issues = Inspect(setup);
            EnsureAbsent(issues, "SEMANTIC_TAG_OWNERSHIP_VERSION_INVALID", "Exact writer-owned ownership version must remain valid.");
            EnsureAbsent(issues, "SEMANTIC_TAG_OWNERSHIP_VERSION_NON_CANONICAL", "Exact writer-owned ownership version must remain canonical.");
        }

        private static void NoHandlesDoesNotValidateVersion()
        {
            var setup = Create("NO-HANDLES", "2", false);
            var issues = Inspect(setup);
            EnsureAbsent(issues, "SEMANTIC_TAG_OWNERSHIP_VERSION_INVALID", "Ownership version must not be validated without generated Semantic Tag handles.");
            EnsureAbsent(issues, "SEMANTIC_TAG_OWNERSHIP_VERSION_NON_CANONICAL", "Ownership-version canonicality must not run without generated Semantic Tag handles.");
        }

        private static Setup Create(string suffix, string? version, bool addHandles)
        {
            var project = new ProjectState("P-Semantic-Tag-Version-" + suffix, "Semantic Tag ownership-version canonicality smoke");
            var element = new ProjectElement("Semantic-Tag-Version-" + suffix, ElementCategory.Wall);
            if (addHandles)
            {
                element.Properties[GeneratedSemanticTagHealthService.HandlesKey] = "A";
                element.Properties[GeneratedSemanticTagHealthService.TemplateKey] = "{Id}";
                element.Properties[GeneratedSemanticTagHealthService.TextKey] = element.Id;
                element.Properties[GeneratedSemanticTagHealthService.OwnerProjectKey] = project.ProjectId;
                element.Properties[GeneratedSemanticTagHealthService.OwnerElementKey] = element.Id;
                element.Properties[GeneratedSemanticTagHealthService.TextHeightKey] = "0.18";
                element.Properties[GeneratedSemanticTagHealthService.PositionScopeKey] = GeneratedSemanticTagHealthService.DrawingLocalWcs;
                element.Properties[GeneratedSemanticTagHealthService.PositionXKey] = "0";
                element.Properties[GeneratedSemanticTagHealthService.PositionYKey] = "0";
                element.Properties[GeneratedSemanticTagHealthService.PositionZKey] = "0";
                element.Properties[GeneratedSemanticTagHealthService.RotationKey] = "0";
            }
            if (version != null)
                element.Properties[GeneratedSemanticTagHealthService.OwnershipVersionKey] = version;
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(Setup setup) =>
            new GeneratedSemanticTagHealthService().Inspect(setup.Project);

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code, HealthSeverity severity)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == severity &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Semantic Tag ownership-version issue was not reported: " + code + ".");
        }

        private static void EnsureAbsent(IReadOnlyList<ModelHealthIssue> issues, string code, string message)
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
