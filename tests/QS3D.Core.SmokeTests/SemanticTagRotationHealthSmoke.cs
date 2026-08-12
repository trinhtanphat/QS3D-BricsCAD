using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticTagRotationHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MissingRotationFailsVisible();
            NonFiniteRotationFailsVisible();
            AlternateRotationSpellingFailsVisible();
            ZeroRotationRemainsCanonical();
            PositiveRotationRemainsCanonical();
            NoHandlesDoesNotValidateRotation();
        }

        private static void MissingRotationFailsVisible()
        {
            var setup = Create("MISSING", null, true);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "SEMANTIC_TAG_ROTATION_INVALID", HealthSeverity.Error);
            EnsureAbsent(issues, "SEMANTIC_TAG_ROTATION_NON_CANONICAL", "Missing rotation must fail before canonicality.");
        }

        private static void NonFiniteRotationFailsVisible()
        {
            var setup = Create("NAN", "NaN", true);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "SEMANTIC_TAG_ROTATION_INVALID", HealthSeverity.Error);
            EnsureAbsent(issues, "SEMANTIC_TAG_ROTATION_NON_CANONICAL", "Non-finite rotation must fail before canonicality.");
        }

        private static void AlternateRotationSpellingFailsVisible()
        {
            var setup = Create("ALIAS", "0.0", true);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "SEMANTIC_TAG_ROTATION_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "SEMANTIC_TAG_ROTATION_INVALID", "Alternate finite rotation spelling must remain numerically valid.");
        }

        private static void ZeroRotationRemainsCanonical()
        {
            var setup = Create("ZERO", "0", true);
            var issues = Inspect(setup);
            EnsureAbsent(issues, "SEMANTIC_TAG_ROTATION_INVALID", "Writer-owned zero rotation must remain valid.");
            EnsureAbsent(issues, "SEMANTIC_TAG_ROTATION_NON_CANONICAL", "Writer-owned zero rotation must remain canonical.");
        }

        private static void PositiveRotationRemainsCanonical()
        {
            var setup = Create("POSITIVE", "1.25", true);
            var issues = Inspect(setup);
            EnsureAbsent(issues, "SEMANTIC_TAG_ROTATION_INVALID", "Finite writer-owned rotation must remain valid.");
            EnsureAbsent(issues, "SEMANTIC_TAG_ROTATION_NON_CANONICAL", "Round-trip writer-owned rotation must remain canonical.");
        }

        private static void NoHandlesDoesNotValidateRotation()
        {
            var setup = Create("NO-HANDLES", "NaN", false);
            var issues = Inspect(setup);
            EnsureAbsent(issues, "SEMANTIC_TAG_ROTATION_INVALID", "Rotation metadata must not be validated without generated Semantic Tag handles.");
            EnsureAbsent(issues, "SEMANTIC_TAG_ROTATION_NON_CANONICAL", "Rotation canonicality must not run without generated Semantic Tag handles.");
        }

        private static Setup Create(string suffix, string? rotation, bool addHandles)
        {
            var project = new ProjectState("P-Semantic-Tag-Rotation-" + suffix, "Semantic Tag rotation health smoke");
            var element = new ProjectElement("Semantic-Tag-Rotation-" + suffix, ElementCategory.Wall);
            if (addHandles)
            {
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
            }
            if (rotation != null)
                element.Properties[GeneratedSemanticTagHealthService.RotationKey] = rotation;
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
            throw new InvalidOperationException("Expected Semantic Tag rotation issue was not reported: " + code + ".");
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
