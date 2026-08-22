using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamStirrupAdvancedNumericCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PositiveSnapshotAliasesFailVisible();
            NonNegativeSnapshotAliasesFailVisible();
            InvalidCenterlineKeepsInvalidPrecedence();
            CanonicalTotalMismatchKeepsMismatch();
            CanonicalAdvancedMetadataRemainsClean();
        }

        private static void PositiveSnapshotAliasesFailVisible()
        {
            RequireSingleAlias("CENTERLINE", "GeneratedBeamStirrupCenterlineLengthM", "4.0");
            RequireSingleAlias("TOTAL", "GeneratedBeamStirrupTotalCenterlineLengthM", "4.0");
            RequireSingleAlias("POLYLINE", "GeneratedBeamStirrupPolylineLengthM", "4.0");
        }

        private static void NonNegativeSnapshotAliasesFailVisible()
        {
            RequireSingleAlias("BEND", "GeneratedBeamStirrupBendRadiusM", "0.0");
            RequireSingleAlias("HOOK", "GeneratedBeamStirrupHookLengthM", "0.0");
            RequireSingleAlias("ANGLE", "GeneratedBeamStirrupHookTailAngleDeg", "0.0");
        }

        private static void RequireSingleAlias(string suffix, string key, string value)
        {
            var setup = Create(suffix);
            setup.Element.Properties[key] = value;
            var issues = Inspect(setup);
            var count = issues.Count(x => string.Equals(x.Code, "BEAM_STIRRUP_GENERATED_METADATA_NON_CANONICAL", StringComparison.Ordinal));
            if (count != 1)
                throw new InvalidOperationException("Expected exactly one advanced numeric canonicality issue for " + key + ", got " + count + ".");
            EnsureAbsent(issues, "BEAM_STIRRUP_GENERATED_METADATA_INVALID", "A numerically valid alias must not become invalid: " + key + ".");
        }

        private static void InvalidCenterlineKeepsInvalidPrecedence()
        {
            var setup = Create("INVALID");
            setup.Element.Properties["GeneratedBeamStirrupCenterlineLengthM"] = "NaN";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_METADATA_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "BEAM_STIRRUP_GENERATED_METADATA_NON_CANONICAL", "Invalid centerline must fail before canonicality.");
        }

        private static void CanonicalTotalMismatchKeepsMismatch()
        {
            var setup = Create("MISMATCH");
            setup.Element.Properties["GeneratedBeamStirrupTotalCenterlineLengthM"] = "5";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_LENGTH_MISMATCH", HealthSeverity.Warning);
            EnsureAbsent(issues, "BEAM_STIRRUP_GENERATED_METADATA_NON_CANONICAL", "Canonical total length must remain canonical while mismatch uses its parsed value.");
        }

        private static void CanonicalAdvancedMetadataRemainsClean()
        {
            var setup = Create("CANONICAL");
            EnsureAbsent(Inspect(setup), "BEAM_STIRRUP_GENERATED_METADATA_NON_CANONICAL", "Exact writer-owned advanced numeric strings must remain canonical.");
        }

        private static Setup Create(string suffix)
        {
            var project = new ProjectState("P-Beam-Stirrup-Advanced-" + suffix, "Beam Stirrup advanced numeric canonicality smoke");
            var element = new ProjectElement("Beam-Stirrup-Advanced-" + suffix, ElementCategory.Beam);
            element.Properties["GeneratedBeamStirrupHandles"] = "A";
            element.Properties["GeneratedBeamStirrupCount"] = "1";
            element.Properties["GeneratedBeamStirrupDiameterMm"] = "10";
            element.Properties["GeneratedBeamStirrupMode"] = "Beam.Line.RectangularClosedLoop";
            element.Properties["GeneratedBeamStirrupCenterlineLengthM"] = "4";
            element.Properties["GeneratedBeamStirrupTotalCenterlineLengthM"] = "4";
            element.Properties["GeneratedBeamStirrupPolylineLengthM"] = "4";
            element.Properties["GeneratedBeamStirrupBendRadiusM"] = "0";
            element.Properties["GeneratedBeamStirrupHookLengthM"] = "0";
            element.Properties["GeneratedBeamStirrupHookTailAngleDeg"] = "0";
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
            throw new InvalidOperationException("Expected Beam Stirrup advanced numeric issue was not reported: " + code + ".");
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
