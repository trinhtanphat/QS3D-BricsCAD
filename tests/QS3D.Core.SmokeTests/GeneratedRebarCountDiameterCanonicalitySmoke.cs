using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedRebarCountDiameterCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LongitudinalCountAliasFailsVisible();
            ShapeCountAliasFailsVisible();
            DiameterAliasFailsVisible();
            LongitudinalCountMismatchKeepsPrecedence();
            ShapeCountMismatchKeepsPrecedence();
            InvalidDiameterKeepsPrecedence();
            CanonicalWriterValuesRemainClean();
        }

        private static void LongitudinalCountAliasFailsVisible()
        {
            var setup = CreateLongitudinal("COUNT-ALIAS");
            setup.Element.Properties["GeneratedRebarCount"] = "01";
            var issues = Inspect(setup.Project);
            RequireIssue(issues, setup.Element.Id, "REBAR_GENERATED_COUNT_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "REBAR_GENERATED_COUNT_MISMATCH", "Parsed longitudinal count alias must still equal one valid handle.");
        }

        private static void ShapeCountAliasFailsVisible()
        {
            var setup = CreateShape("SHAPE-COUNT-ALIAS");
            setup.Element.Properties["GeneratedShapeRebarCount"] = "01";
            var issues = InspectShape(setup.Project);
            RequireIssue(issues, setup.Element.Id, "SHAPE_REBAR_GENERATED_COUNT_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "SHAPE_REBAR_GENERATED_COUNT_MISMATCH", "Parsed shape count alias must still equal one valid handle.");
        }

        private static void DiameterAliasFailsVisible()
        {
            var setup = CreateLongitudinal("DIAMETER-ALIAS");
            setup.Element.Properties["GeneratedRebarDiameterMm"] = "10.0";
            var issues = Inspect(setup.Project);
            RequireIssue(issues, setup.Element.Id, "REBAR_GENERATED_DIAMETER_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "REBAR_GENERATED_DIAMETER_INVALID", "Alternate positive diameter spelling must remain numerically valid.");
        }

        private static void LongitudinalCountMismatchKeepsPrecedence()
        {
            var setup = CreateLongitudinal("COUNT-MISMATCH");
            setup.Element.Properties["GeneratedRebarCount"] = "2";
            var issues = Inspect(setup.Project);
            RequireIssue(issues, setup.Element.Id, "REBAR_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning);
            EnsureAbsent(issues, "REBAR_GENERATED_COUNT_NON_CANONICAL", "A genuine longitudinal count mismatch must not receive canonicality noise.");
        }

        private static void ShapeCountMismatchKeepsPrecedence()
        {
            var setup = CreateShape("SHAPE-MISMATCH");
            setup.Element.Properties["GeneratedShapeRebarCount"] = "2";
            var issues = InspectShape(setup.Project);
            RequireIssue(issues, setup.Element.Id, "SHAPE_REBAR_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning);
            EnsureAbsent(issues, "SHAPE_REBAR_GENERATED_COUNT_NON_CANONICAL", "A genuine shape count mismatch must not receive canonicality noise.");
        }

        private static void InvalidDiameterKeepsPrecedence()
        {
            var setup = CreateLongitudinal("DIAMETER-INVALID");
            setup.Element.Properties["GeneratedRebarDiameterMm"] = "NaN";
            var issues = Inspect(setup.Project);
            RequireIssue(issues, setup.Element.Id, "REBAR_GENERATED_DIAMETER_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "REBAR_GENERATED_DIAMETER_NON_CANONICAL", "Invalid diameter must fail before canonicality.");
        }

        private static void CanonicalWriterValuesRemainClean()
        {
            var longitudinal = CreateLongitudinal("LONG-CANONICAL");
            var longitudinalIssues = Inspect(longitudinal.Project);
            EnsureAbsent(longitudinalIssues, "REBAR_GENERATED_COUNT_NON_CANONICAL", "Exact invariant longitudinal count must remain canonical.");
            EnsureAbsent(longitudinalIssues, "REBAR_GENERATED_DIAMETER_NON_CANONICAL", "Exact round-trip longitudinal diameter must remain canonical.");

            var shape = CreateShape("SHAPE-CANONICAL");
            EnsureAbsent(InspectShape(shape.Project), "SHAPE_REBAR_GENERATED_COUNT_NON_CANONICAL", "Exact invariant shape count must remain canonical.");
        }

        private static Setup CreateLongitudinal(string suffix)
        {
            var project = new ProjectState("P-Rebar-Canonical-" + suffix, "Generated rebar count/diameter canonicality smoke");
            var element = new ProjectElement("Rebar-Canonical-" + suffix, ElementCategory.Column);
            element.Properties["GeneratedRebarHandles"] = "A";
            element.Properties["GeneratedRebarCount"] = "1";
            element.Properties["GeneratedRebarDiameterMm"] = "10";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static Setup CreateShape(string suffix)
        {
            var project = new ProjectState("P-Shape-Rebar-Canonical-" + suffix, "Generated shape rebar count canonicality smoke");
            var element = new ProjectElement("Shape-Rebar-Canonical-" + suffix, ElementCategory.Wall);
            element.Properties["GeneratedShapeRebarHandles"] = "A";
            element.Properties["GeneratedShapeRebarCount"] = "1";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project) =>
            new GeneratedRebarHealthService().Inspect(project);

        private static IReadOnlyList<ModelHealthIssue> InspectShape(ProjectState project) =>
            new GeneratedRebarHealthService().InspectShape(project);

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code, HealthSeverity severity)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == severity &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected generated rebar canonicality issue was not reported: " + code + ".");
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
