using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class TieRebarCoverModeHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NonFiniteCoverFailsVisible();
            NegativeCoverFailsVisible();
            AlternateCoverSpellingFailsVisible();
            MissingModeFailsVisible();
            UnsupportedModeFailsVisible();
            ModeAliasFailsVisible();
            CanonicalCoverAndModeRemainClean();
        }

        private static void NonFiniteCoverFailsVisible()
        {
            var setup = Create("NAN");
            setup.Element.Properties["GeneratedTieRebarCoverM"] = "NaN";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_COVER_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_COVER_NON_CANONICAL", "Invalid cover must fail before canonicality.");
        }

        private static void NegativeCoverFailsVisible()
        {
            var setup = Create("NEG");
            setup.Element.Properties["GeneratedTieRebarCoverM"] = "-0.01";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_COVER_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_COVER_NON_CANONICAL", "Negative cover must fail domain validation before canonicality.");
        }

        private static void AlternateCoverSpellingFailsVisible()
        {
            var setup = Create("COVER-ALIAS");
            setup.Element.Properties["GeneratedTieRebarCoverM"] = "0.050";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_COVER_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_COVER_INVALID", "Alternate positive cover spelling remains numerically valid.");
        }

        private static void MissingModeFailsVisible()
        {
            var setup = Create("MODE-MISSING");
            setup.Element.Properties.Remove("GeneratedTieRebarMode");
            RequireIssue(Inspect(setup), setup.Element.Id, "TIE_REBAR_GENERATED_MODE_INVALID", HealthSeverity.Warning);
        }

        private static void UnsupportedModeFailsVisible()
        {
            var setup = Create("MODE-INVALID");
            setup.Element.Properties["GeneratedTieRebarMode"] = "ColumnCircularTies";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_MODE_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_MODE_NON_CANONICAL", "Unsupported mode must stay invalid rather than canonicality-only.");
        }

        private static void ModeAliasFailsVisible()
        {
            var setup = Create("MODE-ALIAS");
            setup.Element.Properties["GeneratedTieRebarMode"] = " columnrectangularties ";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_MODE_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_MODE_INVALID", "Recognized mode aliases retain semantic mode meaning.");
        }

        private static void CanonicalCoverAndModeRemainClean()
        {
            var setup = Create("CANONICAL");
            var issues = Inspect(setup);
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_COVER_INVALID", "Writer-owned cover must remain valid.");
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_COVER_NON_CANONICAL", "Writer-owned cover must remain canonical.");
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_MODE_INVALID", "Writer-owned mode must remain valid.");
            EnsureAbsent(issues, "TIE_REBAR_GENERATED_MODE_NON_CANONICAL", "Writer-owned mode must remain canonical.");
        }

        private static Setup Create(string suffix)
        {
            var project = new ProjectState("P-Tie-Cover-Mode-" + suffix, "Tie Rebar cover/mode health smoke");
            var element = new ProjectElement("Tie-Cover-Mode-" + suffix, ElementCategory.Column);
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
            throw new InvalidOperationException("Expected Tie Rebar cover/mode issue was not reported: " + code + ".");
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
