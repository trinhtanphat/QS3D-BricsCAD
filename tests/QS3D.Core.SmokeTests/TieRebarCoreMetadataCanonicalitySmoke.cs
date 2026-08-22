using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class TieRebarCoreMetadataCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LeadingZeroCountFailsVisible();
            AlternateDiameterSpellingFailsVisible();
            AlternateSpacingSpellingFailsVisible();
            CountMismatchKeepsMismatchPrecedence();
            InvalidDiameterKeepsInvalidPrecedence();
            InvalidSpacingKeepsInvalidPrecedence();
            ZeroSpacingRemainsCanonical();
            CanonicalCoreMetadataRemainsClean();
        }

        private static void LeadingZeroCountFailsVisible()
        {
            var setup = Create("COUNT");
            setup.Element.Properties["GeneratedTieRebarCount"] = "01";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_COUNT_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_COUNT_MISMATCH", "Parsed leading-zero count must still match one valid handle.");
        }

        private static void AlternateDiameterSpellingFailsVisible()
        {
            var setup = Create("DIAMETER");
            setup.Element.Properties["GeneratedTieRebarDiameterMm"] = "10.0";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_DIAMETER_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_DIAMETER_INVALID", "Alternate positive diameter spelling remains numerically valid.");
        }

        private static void AlternateSpacingSpellingFailsVisible()
        {
            var setup = Create("SPACING");
            setup.Element.Properties["GeneratedTieRebarActualSpacingM"] = "0.200";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_SPACING_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_SPACING_INVALID", "Alternate nonnegative spacing spelling remains numerically valid.");
        }

        private static void CountMismatchKeepsMismatchPrecedence()
        {
            var setup = Create("COUNT-MISMATCH");
            setup.Element.Properties["GeneratedTieRebarCount"] = "2";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_COUNT_NON_CANONICAL", "A genuine count mismatch must not receive canonicality noise.");
        }

        private static void InvalidDiameterKeepsInvalidPrecedence()
        {
            var setup = Create("DIAMETER-INVALID");
            setup.Element.Properties["GeneratedTieRebarDiameterMm"] = "NaN";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_DIAMETER_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_DIAMETER_NON_CANONICAL", "Invalid diameter must fail before canonicality.");
        }

        private static void InvalidSpacingKeepsInvalidPrecedence()
        {
            var setup = Create("SPACING-INVALID");
            setup.Element.Properties["GeneratedTieRebarActualSpacingM"] = "-0.01";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_SPACING_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_SPACING_NON_CANONICAL", "Negative spacing must fail before canonicality.");
        }

        private static void ZeroSpacingRemainsCanonical()
        {
            var setup = Create("ZERO");
            setup.Element.Properties["GeneratedTieRebarActualSpacingM"] = "0";
            var issues = Inspect(setup);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_SPACING_INVALID", "Single-tie zero spacing must remain valid.");
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_SPACING_NON_CANONICAL", "Writer-owned zero spacing must remain canonical.");
        }

        private static void CanonicalCoreMetadataRemainsClean()
        {
            var setup = Create("CANONICAL");
            var issues = Inspect(setup);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_COUNT_NON_CANONICAL", "Exact invariant count must remain canonical.");
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_DIAMETER_NON_CANONICAL", "Exact round-trip diameter must remain canonical.");
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_SPACING_NON_CANONICAL", "Exact round-trip spacing must remain canonical.");
        }

        private static Setup Create(string suffix)
        {
            var project = new ProjectState("P-Tie-Core-" + suffix, "Tie Rebar core metadata canonicality smoke");
            var element = new ProjectElement("Tie-Core-" + suffix, ElementCategory.Column);
            element.Properties["GeneratedTieRebarHandles"] = "A";
            element.Properties["GeneratedTieRebarCount"] = "1";
            element.Properties["GeneratedTieRebarDiameterMm"] = "10";
            element.Properties["GeneratedTieRebarActualSpacingM"] = "0.2";
            element.Properties["GeneratedTieRebarCoverM"] = "0.05";
            element.Properties["GeneratedTieRebarMode"] = "ColumnRectangularTies";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(Setup setup) =>
            new GeneratedTieRebarHealthService().Inspect(setup.Project);

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code, HealthSeverity severity)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == severity &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Tie Rebar core metadata issue was not reported: " + code + ".");
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
