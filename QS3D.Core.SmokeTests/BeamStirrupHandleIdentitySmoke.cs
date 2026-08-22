using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamStirrupHandleIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NumericAliasDuplicateCountsOnce();
            SourceAliasFailsVisible();
            LiveAliasIsRecognized();
            CrossOwnerAliasConflicts();
            DistinctHandlesStayDistinct();
            PrefixedHexRemainsInvalid();
        }

        private static void NumericAliasDuplicateCountsOnce()
        {
            var setup = Create("DUPLICATE", "A;0A", "2");
            var issues = Inspect(setup);
            Require(issues, setup.Element.Id, "DUPLICATE_BEAM_STIRRUP_GENERATED_HANDLE", HealthSeverity.Error);
            Require(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning);
        }

        private static void SourceAliasFailsVisible()
        {
            var setup = Create("SOURCE", "0A", "1");
            setup.Element.SourceHandles.Add("A");
            var issues = Inspect(setup);
            Require(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error);
        }

        private static void LiveAliasIsRecognized()
        {
            var setup = Create("LIVE", "0A", "1");
            var issues = Inspect(setup, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" });
            Forbid(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_SOLID_MISSING");
        }

        private static void CrossOwnerAliasConflicts()
        {
            var setup = Create("OWNER", "0A", "1");
            var other = new ProjectElement("E-BEAM-STIRRUP-HANDLE-OWNER-OTHER", ElementCategory.StructuralWall);
            other.SourceHandles.Add("A");
            setup.Project.Elements.Add(other);
            var issues = Inspect(setup);
            Require(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error);
        }

        private static void DistinctHandlesStayDistinct()
        {
            var setup = Create("DISTINCT", "A;B", "2");
            var issues = Inspect(setup);
            Forbid(issues, setup.Element.Id, "DUPLICATE_BEAM_STIRRUP_GENERATED_HANDLE");
            Forbid(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_COUNT_MISMATCH");
        }

        private static void PrefixedHexRemainsInvalid()
        {
            var setup = Create("PREFIX", "0xA", "0");
            var issues = Inspect(setup);
            Require(issues, setup.Element.Id, "INVALID_BEAM_STIRRUP_GENERATED_HANDLE", HealthSeverity.Error);
            Forbid(issues, setup.Element.Id, "BEAM_STIRRUP_GENERATED_COUNT_MISMATCH");
        }

        private static Setup Create(string suffix, string handles, string count)
        {
            var project = new ProjectState("P-BEAM-STIRRUP-HANDLE-" + suffix, "Beam Stirrup handle identity");
            var element = new ProjectElement("E-BEAM-STIRRUP-HANDLE-" + suffix, ElementCategory.Beam);
            element.Properties["GeneratedBeamStirrupHandles"] = handles;
            element.Properties["GeneratedBeamStirrupCount"] = count;
            element.Properties["GeneratedBeamStirrupDiameterMm"] = "10";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(Setup setup, ISet<string>? live = null) =>
            new GeneratedBeamStirrupHealthService().Inspect(setup.Project, live);

        private static void Require(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code, HealthSeverity severity)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == severity &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("BeamStirrupHandleIdentitySmoke expected issue was not reported: " + code + ".");
        }

        private static void Forbid(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("BeamStirrupHandleIdentitySmoke reported unexpected issue: " + code + ".");
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
