using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticTagPlacementCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            TextHeightAliasFailsVisible();
            PositionAliasesFailVisible();
            PaddedScopeFailsVisible();
            InvalidTextHeightKeepsPrecedence();
            InvalidPositionKeepsPrecedence();
            InvalidScopeKeepsPrecedence();
            CanonicalPlacementRemainsClean();
        }

        private static void TextHeightAliasFailsVisible()
        {
            var setup = Create("HEIGHT");
            setup.Element.Properties[GeneratedSemanticTagHealthService.TextHeightKey] = "0.180";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "SEMANTIC_TAG_TEXT_HEIGHT_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "SEMANTIC_TAG_TEXT_HEIGHT_INVALID", "Alternate positive text-height spelling must remain numerically valid.");
        }

        private static void PositionAliasesFailVisible()
        {
            RequirePositionAlias("X", GeneratedSemanticTagHealthService.PositionXKey);
            RequirePositionAlias("Y", GeneratedSemanticTagHealthService.PositionYKey);
            RequirePositionAlias("Z", GeneratedSemanticTagHealthService.PositionZKey);
        }

        private static void RequirePositionAlias(string suffix, string key)
        {
            var setup = Create("POS-" + suffix);
            setup.Element.Properties[key] = "0.0";
            var issues = Inspect(setup);
            var count = issues.Count(x => string.Equals(x.Code, "SEMANTIC_TAG_POSITION_NON_CANONICAL", StringComparison.Ordinal));
            if (count != 1)
                throw new InvalidOperationException("Expected exactly one Semantic Tag position canonicality issue for " + key + ", got " + count + ".");
            EnsureAbsent(issues, "SEMANTIC_TAG_POSITION_INVALID", "Alternate finite position spelling must remain numerically valid: " + key + ".");
        }

        private static void PaddedScopeFailsVisible()
        {
            var setup = Create("SCOPE-PAD");
            setup.Element.Properties[GeneratedSemanticTagHealthService.PositionScopeKey] = " DrawingLocalWcs ";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "SEMANTIC_TAG_POSITION_SCOPE_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "SEMANTIC_TAG_POSITION_SCOPE_INVALID", "Padded exact scope must retain its existing semantic meaning.");
        }

        private static void InvalidTextHeightKeepsPrecedence()
        {
            var setup = Create("HEIGHT-INVALID");
            setup.Element.Properties[GeneratedSemanticTagHealthService.TextHeightKey] = "NaN";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "SEMANTIC_TAG_TEXT_HEIGHT_INVALID", HealthSeverity.Error);
            EnsureAbsent(issues, "SEMANTIC_TAG_TEXT_HEIGHT_NON_CANONICAL", "Invalid text height must fail before canonicality.");
        }

        private static void InvalidPositionKeepsPrecedence()
        {
            var setup = Create("POSITION-INVALID");
            setup.Element.Properties[GeneratedSemanticTagHealthService.PositionXKey] = "Infinity";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "SEMANTIC_TAG_POSITION_INVALID", HealthSeverity.Error);
            EnsureAbsent(issues, "SEMANTIC_TAG_POSITION_NON_CANONICAL", "Invalid position must fail before canonicality.");
        }

        private static void InvalidScopeKeepsPrecedence()
        {
            var setup = Create("SCOPE-INVALID");
            setup.Element.Properties[GeneratedSemanticTagHealthService.PositionScopeKey] = "drawinglocalwcs";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "SEMANTIC_TAG_POSITION_SCOPE_INVALID", HealthSeverity.Error);
            EnsureAbsent(issues, "SEMANTIC_TAG_POSITION_SCOPE_NON_CANONICAL", "Existing case-sensitive invalid scope semantics must remain unchanged.");
        }

        private static void CanonicalPlacementRemainsClean()
        {
            var setup = Create("CANONICAL");
            var issues = Inspect(setup);
            EnsureAbsent(issues, "SEMANTIC_TAG_TEXT_HEIGHT_NON_CANONICAL", "Exact writer-owned text height must remain canonical.");
            EnsureAbsent(issues, "SEMANTIC_TAG_POSITION_NON_CANONICAL", "Exact writer-owned position components must remain canonical.");
            EnsureAbsent(issues, "SEMANTIC_TAG_POSITION_SCOPE_NON_CANONICAL", "Exact writer-owned position scope must remain canonical.");
        }

        private static Setup Create(string suffix)
        {
            var project = new ProjectState("P-Semantic-Tag-Placement-" + suffix, "Semantic Tag placement canonicality smoke");
            var element = new ProjectElement("Semantic-Tag-Placement-" + suffix, ElementCategory.ArchitecturalWall);
            element.Properties[GeneratedSemanticTagHealthService.HandlesKey] = "A";
            element.Properties[GeneratedSemanticTagHealthService.TemplateKey] = "{Id}";
            element.Properties[GeneratedSemanticTagHealthService.TextKey] = element.Id;
            element.Properties[GeneratedSemanticTagHealthService.OwnerProjectKey] = project.ProjectId;
            element.Properties[GeneratedSemanticTagHealthService.OwnerElementKey] = element.Id;
            element.Properties[GeneratedSemanticTagHealthService.OwnershipVersionKey] = GeneratedSemanticTagHealthService.OwnershipVersion;
            element.Properties[GeneratedSemanticTagHealthService.TextHeightKey] = "0.18";
            element.Properties[GeneratedSemanticTagHealthService.PositionScopeKey] = GeneratedSemanticTagHealthService.DrawingLocalWcs;
            element.Properties[GeneratedSemanticTagHealthService.PositionXKey] = "0";
            element.Properties[GeneratedSemanticTagHealthService.PositionYKey] = "0";
            element.Properties[GeneratedSemanticTagHealthService.PositionZKey] = "0";
            element.Properties[GeneratedSemanticTagHealthService.RotationKey] = "0";
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
            throw new InvalidOperationException("Expected Semantic Tag placement issue was not reported: " + code + ".");
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
