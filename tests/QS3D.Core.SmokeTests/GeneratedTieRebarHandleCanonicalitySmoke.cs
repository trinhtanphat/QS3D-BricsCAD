using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedTieRebarHandleCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedHandleFailsVisibleButKeepsLiveLookup();
            LowercaseCanonicalHandleRemainsAccepted();
            EmptyDelimiterTokenRemainsInvalid();
        }

        private static void PaddedHandleFailsVisibleButKeepsLiveLookup()
        {
            var setup = Create("PAD", " A ", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new GeneratedTieRebarHealthService().Inspect(setup.Project, live);

            RequireIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_HANDLE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_SOLID_MISSING");
            ForbidIssue(issues, setup.Element.Id, "INVALID_TIE_REBAR_GENERATED_HANDLE");
        }

        private static void LowercaseCanonicalHandleRemainsAccepted()
        {
            var setup = Create("LOWER", "a", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new GeneratedTieRebarHealthService().Inspect(setup.Project, live);

            ForbidIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_HANDLE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "INVALID_TIE_REBAR_GENERATED_HANDLE");
            ForbidIssue(issues, setup.Element.Id, "TIE_REBAR_GENERATED_SOLID_MISSING");
        }

        private static void EmptyDelimiterTokenRemainsInvalid()
        {
            var setup = Create("EMPTY", "A;;B", "2");
            var issues = new GeneratedTieRebarHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "INVALID_TIE_REBAR_GENERATED_HANDLE");
        }

        private static Setup Create(string suffix, string handles, string count)
        {
            var project = new ProjectState("P-TIE-CANON-" + suffix, "Generated Tie Rebar handle canonicality");
            var element = new ProjectElement("E-TIE-CANON-" + suffix, ElementCategory.Column);
            element.Properties["GeneratedTieRebarHandles"] = handles;
            element.Properties["GeneratedTieRebarCount"] = count;
            element.Properties["GeneratedTieRebarDiameterMm"] = "10";
            element.Properties["GeneratedTieRebarActualSpacingM"] = "0.2";
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
            throw new InvalidOperationException("GeneratedTieRebarHandleCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedTieRebarHandleCanonicalitySmoke unexpected issue was reported: " + code + ".");
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
