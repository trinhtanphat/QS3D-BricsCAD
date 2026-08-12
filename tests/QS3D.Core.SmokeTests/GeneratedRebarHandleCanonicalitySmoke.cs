using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedRebarHandleCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedLongitudinalHandleFailsVisibleButKeepsLiveLookup();
            PaddedShapeHandleFailsVisibleButKeepsLiveLookup();
            LowercaseCanonicalHandlesRemainAccepted();
            EmptyDelimiterTokenRemainsInvalid();
        }

        private static void PaddedLongitudinalHandleFailsVisibleButKeepsLiveLookup()
        {
            var setup = CreateLongitudinal("PAD-LONG", " A ", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new GeneratedRebarHealthService().Inspect(setup.Project, live);

            RequireIssue(issues, setup.Element.Id, "REBAR_GENERATED_HANDLE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "REBAR_GENERATED_SOLID_MISSING");
            ForbidIssue(issues, setup.Element.Id, "INVALID_REBAR_GENERATED_HANDLE");
        }

        private static void PaddedShapeHandleFailsVisibleButKeepsLiveLookup()
        {
            var project = new ProjectState("P-REBAR-CANON-PAD-SHAPE", "Generated shape rebar handle canonicality");
            var element = new ProjectElement("E-PAD-SHAPE", ElementCategory.Beam);
            element.Properties["GeneratedShapeRebarHandles"] = " b ";
            element.Properties["GeneratedShapeRebarCount"] = "1";
            project.Elements.Add(element);
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "B" };

            var issues = new GeneratedRebarHealthService().InspectShape(project, live);

            RequireIssue(issues, element.Id, "SHAPE_REBAR_GENERATED_HANDLE_NON_CANONICAL");
            ForbidIssue(issues, element.Id, "SHAPE_REBAR_GENERATED_SOLID_MISSING");
            ForbidIssue(issues, element.Id, "INVALID_SHAPE_REBAR_GENERATED_HANDLE");
        }

        private static void LowercaseCanonicalHandlesRemainAccepted()
        {
            var setup = CreateLongitudinal("LOWER", "a", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new GeneratedRebarHealthService().Inspect(setup.Project, live);

            ForbidIssue(issues, setup.Element.Id, "REBAR_GENERATED_HANDLE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "INVALID_REBAR_GENERATED_HANDLE");
            ForbidIssue(issues, setup.Element.Id, "REBAR_GENERATED_SOLID_MISSING");
        }

        private static void EmptyDelimiterTokenRemainsInvalid()
        {
            var setup = CreateLongitudinal("EMPTY", "A;;B", "2");
            var issues = new GeneratedRebarHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "INVALID_REBAR_GENERATED_HANDLE");
        }

        private static Setup CreateLongitudinal(string suffix, string handles, string count)
        {
            var project = new ProjectState("P-REBAR-CANON-" + suffix, "Generated rebar handle canonicality");
            var element = new ProjectElement("E-" + suffix, ElementCategory.Beam);
            element.Properties["GeneratedRebarHandles"] = handles;
            element.Properties["GeneratedRebarCount"] = count;
            element.Properties["GeneratedRebarDiameterMm"] = "16";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedRebarHandleCanonicalitySmoke expected health issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedRebarHandleCanonicalitySmoke unexpected health issue was reported: " + code + ".");
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
