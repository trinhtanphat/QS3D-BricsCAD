using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamStirrupCoreMetadataCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LeadingZeroCountFailsVisible();
            AlternateDiameterSpellingFailsVisible();
            ModeAliasFailsVisible();
            CountMismatchKeepsMismatchPrecedence();
            InvalidDiameterKeepsInvalidPrecedence();
            UnsupportedModeKeepsInvalidPrecedence();
            CanonicalCoreMetadataRemainsClean();
        }

        private static void LeadingZeroCountFailsVisible()
        {
            var setup = Create("COUNT");
            setup.Element.Properties["GeneratedBeamStirrupCount"] = "01";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_COUNT_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "BEAM_STIRRUP_GENERATED_COUNT_MISMATCH", "Parsed leading-zero count must still match one valid handle.");
        }

        private static void AlternateDiameterSpellingFailsVisible()
        {
            var setup = Create("DIAMETER");
            setup.Element.Properties["GeneratedBeamStirrupDiameterMm"] = "10.0";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_DIAMETER_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "BEAM_STIRRUP_GENERATED_DIAMETER_INVALID", "Alternate positive diameter spelling remains numerically valid.");
        }

        private static void ModeAliasFailsVisible()
        {
            var setup = Create("MODE");
            setup.Element.Properties["GeneratedBeamStirrupMode"] = " beam.line.rectangularclosedloop ";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_MODE_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "BEAM_STIRRUP_GENERATED_MODE_INVALID", "Recognized mode aliases must retain semantic mode meaning.");
        }

        private static void CountMismatchKeepsMismatchPrecedence()
        {
            var setup = Create("COUNT-MISMATCH");
            setup.Element.Properties["GeneratedBeamStirrupCount"] = "2";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning);
            EnsureAbsent(issues, "BEAM_STIRRUP_GENERATED_COUNT_NON_CANONICAL", "A genuine count mismatch must not receive canonicality noise.");
        }

        private static void InvalidDiameterKeepsInvalidPrecedence()
        {
            var setup = Create("DIAMETER-INVALID");
            setup.Element.Properties["GeneratedBeamStirrupDiameterMm"] = "NaN";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_DIAMETER_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "BEAM_STIRRUP_GENERATED_DIAMETER_NON_CANONICAL", "Invalid diameter must fail before canonicality.");
        }

        private static void UnsupportedModeKeepsInvalidPrecedence()
        {
            var setup = Create("MODE-INVALID");
            setup.Element.Properties["GeneratedBeamStirrupMode"] = "Beam.Line.Unknown";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_MODE_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "BEAM_STIRRUP_GENERATED_MODE_NON_CANONICAL", "Unsupported mode must remain invalid rather than canonicality-only.");
        }

        private static void CanonicalCoreMetadataRemainsClean()
        {
            var setup = Create("CANONICAL");
            var issues = Inspect(setup);
            EnsureAbsent(issues, "BEAM_STIRRUP_GENERATED_COUNT_NON_CANONICAL", "Exact invariant count must remain canonical.");
            EnsureAbsent(issues, "BEAM_STIRRUP_GENERATED_DIAMETER_NON_CANONICAL", "Exact round-trip diameter must remain canonical.");
            EnsureAbsent(issues, "BEAM_STIRRUP_GENERATED_MODE_NON_CANONICAL", "Exact writer-owned mode must remain canonical.");
        }

        private static Setup Create(string suffix)
        {
            var project = new ProjectState("P-Beam-Stirrup-Core-" + suffix, "Beam Stirrup core metadata canonicality smoke");
            var element = new ProjectElement("Beam-Stirrup-Core-" + suffix, ElementCategory.Beam);
            element.Properties["GeneratedBeamStirrupHandles"] = "A";
            element.Properties["GeneratedBeamStirrupCount"] = "1";
            element.Properties["GeneratedBeamStirrupDiameterMm"] = "10";
            element.Properties["GeneratedBeamStirrupMode"] = "Beam.Line.RectangularClosedLoop";
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
            throw new InvalidOperationException("Expected Beam Stirrup core metadata issue was not reported: " + code + ".");
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
