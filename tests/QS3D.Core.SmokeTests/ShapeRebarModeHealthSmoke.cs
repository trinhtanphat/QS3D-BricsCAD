using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ShapeRebarModeHealthSmoke
    {
        private const string ModeKey = "GeneratedShapeRebarMode";
        private const string Mode = "BBS.ShapePath.SegmentedCylinder";

        [ModuleInitializer]
        internal static void Initialize()
        {
            MissingModeFailsVisible();
            UnsupportedModeFailsVisible();
            ModeAliasFailsVisible();
            CanonicalModeRemainsClean();
            NoHandlesDoesNotValidateMode();
        }

        private static void MissingModeFailsVisible()
        {
            var setup = Create("MISSING", null, true);
            RequireIssue(Inspect(setup), setup.Element.Id, "GENERATED_REBAR_MODE_METADATA_INVALID", HealthSeverity.Warning);
        }

        private static void UnsupportedModeFailsVisible()
        {
            var setup = Create("INVALID", "BBS.ShapePath.Unknown", true);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GENERATED_REBAR_MODE_METADATA_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "GENERATED_REBAR_MODE_METADATA_NON_CANONICAL", "Unsupported Shape mode must remain invalid rather than canonicality-only.");
        }

        private static void ModeAliasFailsVisible()
        {
            var setup = Create("ALIAS", " bbs.shapepath.segmentedcylinder ", true);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "GENERATED_REBAR_MODE_METADATA_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "GENERATED_REBAR_MODE_METADATA_INVALID", "Recognized Shape mode alias must retain semantic mode meaning.");
        }

        private static void CanonicalModeRemainsClean()
        {
            var setup = Create("CANONICAL", Mode, true);
            var issues = Inspect(setup);
            EnsureAbsent(issues, "GENERATED_REBAR_MODE_METADATA_INVALID", "Exact writer-owned Shape mode must remain valid.");
            EnsureAbsent(issues, "GENERATED_REBAR_MODE_METADATA_NON_CANONICAL", "Exact writer-owned Shape mode must remain canonical.");
        }

        private static void NoHandlesDoesNotValidateMode()
        {
            var setup = Create("NO-HANDLES", "Bad", false);
            var issues = Inspect(setup);
            EnsureAbsent(issues, "GENERATED_REBAR_MODE_METADATA_INVALID", "Shape mode metadata must not be validated when no generated Shape handles exist.");
            EnsureAbsent(issues, "GENERATED_REBAR_MODE_METADATA_NON_CANONICAL", "Shape mode canonicality must not run without generated Shape handles.");
        }

        private static Setup Create(string suffix, string? mode, bool addHandles)
        {
            var project = new ProjectState("P-Shape-Mode-" + suffix, "Shape Rebar mode health smoke");
            var element = new ProjectElement("Shape-Mode-" + suffix, ElementCategory.ArchitecturalWall);
            if (addHandles) element.Properties["GeneratedShapeRebarHandles"] = "A";
            if (mode != null) element.Properties[ModeKey] = mode;
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(Setup setup) =>
            new GeneratedRebarModeHealthService().Inspect(setup.Project);

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code, HealthSeverity severity)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == severity &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Shape Rebar mode health issue was not reported: " + code + ".");
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
