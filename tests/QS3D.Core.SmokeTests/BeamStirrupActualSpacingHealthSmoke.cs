using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamStirrupActualSpacingHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NonFiniteSpacingFailsVisible();
            NegativeSpacingFailsVisible();
            PaddedSpacingFailsCanonicality();
            ZeroSpacingRemainsValid();
            PositiveCanonicalSpacingRemainsValid();
            MissingLegacySpacingRemainsCompatible();
        }

        private static void NonFiniteSpacingFailsVisible()
        {
            var setup = Create("NAN", "NaN");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "BEAM_STIRRUP_ACTUAL_SPACING_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "BEAM_STIRRUP_ACTUAL_SPACING_NON_CANONICAL", "Invalid spacing must not produce canonicality noise.");
        }

        private static void NegativeSpacingFailsVisible()
        {
            var setup = Create("NEG", "-0.01");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "BEAM_STIRRUP_ACTUAL_SPACING_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "BEAM_STIRRUP_ACTUAL_SPACING_NON_CANONICAL", "Negative spacing must fail domain validation before canonicality.");
        }

        private static void PaddedSpacingFailsCanonicality()
        {
            var setup = Create("PAD", " 0.2 ");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "BEAM_STIRRUP_ACTUAL_SPACING_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "BEAM_STIRRUP_ACTUAL_SPACING_INVALID", "Padded positive spacing remains numerically valid.");
        }

        private static void ZeroSpacingRemainsValid()
        {
            var setup = Create("ZERO", "0");
            var issues = Inspect(setup);
            EnsureAbsent(issues, "BEAM_STIRRUP_ACTUAL_SPACING_INVALID", "A single-stirrup layout may have zero actual spacing.");
            EnsureAbsent(issues, "BEAM_STIRRUP_ACTUAL_SPACING_NON_CANONICAL", "Writer-owned zero spelling must be canonical.");
        }

        private static void PositiveCanonicalSpacingRemainsValid()
        {
            var setup = Create("POS", "0.2");
            var issues = Inspect(setup);
            EnsureAbsent(issues, "BEAM_STIRRUP_ACTUAL_SPACING_INVALID", "Positive writer-owned spacing must remain valid.");
            EnsureAbsent(issues, "BEAM_STIRRUP_ACTUAL_SPACING_NON_CANONICAL", "Positive round-trip spacing must remain canonical.");
        }

        private static void MissingLegacySpacingRemainsCompatible()
        {
            var setup = Create("LEGACY", null);
            var issues = Inspect(setup);
            EnsureAbsent(issues, "BEAM_STIRRUP_ACTUAL_SPACING_INVALID", "Legacy generated metadata without actual spacing must retain compatibility.");
            EnsureAbsent(issues, "BEAM_STIRRUP_ACTUAL_SPACING_NON_CANONICAL", "Missing legacy spacing must not receive canonicality evidence.");
        }

        private static Setup Create(string suffix, string? spacing)
        {
            var project = new ProjectState("P-Beam-Stirrup-Spacing-" + suffix, "Beam Stirrup actual spacing health smoke");
            var element = new ProjectElement("Beam-Stirrup-Spacing-" + suffix, ElementCategory.Beam);
            element.Properties["GeneratedBeamStirrupHandles"] = "A";
            element.Properties["GeneratedBeamStirrupCount"] = "1";
            element.Properties["GeneratedBeamStirrupDiameterMm"] = "10";
            if (spacing != null)
                element.Properties["GeneratedBeamStirrupActualSpacingM"] = spacing;
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(Setup setup) =>
            new GeneratedBeamStirrupHealthService().Inspect(setup.Project);

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code, HealthSeverity severity)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == severity &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Beam Stirrup actual-spacing health issue was not reported: " + code + ".");
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
