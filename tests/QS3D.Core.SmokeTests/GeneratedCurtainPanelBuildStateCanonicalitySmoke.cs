using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainPanelBuildStateCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedCompleteFailsVisible();
            CaseVariedCompleteFailsVisible();
            ExactCompleteRemainsAccepted();
            InvalidStateKeepsInvalidPrecedence();
            MissingStateKeepsInvalidPrecedence();
        }

        private static void PaddedCompleteFailsVisible()
        {
            var setup = Create("PAD", " Complete ");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_BUILD_STATE_NON_CANONICAL", HealthSeverity.Error);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_BUILD_STATE_INVALID");
        }

        private static void CaseVariedCompleteFailsVisible()
        {
            var setup = Create("CASE", "COMPLETE");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_BUILD_STATE_NON_CANONICAL", HealthSeverity.Error);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_BUILD_STATE_INVALID");
        }

        private static void ExactCompleteRemainsAccepted()
        {
            var setup = Create("CANONICAL", GeneratedCurtainPanelHealthService.BuildCompleteValue);
            var issues = Inspect(setup);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_BUILD_STATE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_BUILD_STATE_INVALID");
        }

        private static void InvalidStateKeepsInvalidPrecedence()
        {
            var setup = Create("INVALID", "Done");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_BUILD_STATE_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_BUILD_STATE_NON_CANONICAL");
        }

        private static void MissingStateKeepsInvalidPrecedence()
        {
            var setup = Create("MISSING", GeneratedCurtainPanelHealthService.BuildCompleteValue);
            setup.Element.Properties.Remove(GeneratedCurtainPanelHealthService.BuildStateKey);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_BUILD_STATE_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_BUILD_STATE_NON_CANONICAL");
        }

        private static Setup Create(string suffix, string buildState)
        {
            var project = new ProjectState("P-CURTAIN-PANEL-STATE-" + suffix, "Curtain Panel build-state canonicality");
            var element = new ProjectElement("E-CURTAIN-PANEL-STATE-" + suffix, ElementCategory.GlassWall);
            element.Properties[GeneratedCurtainPanelHealthService.HandlesKey] = "A";
            element.Properties[GeneratedCurtainPanelHealthService.BuildStateKey] = buildState;
            element.Properties["GeneratedCurtainPanelCount"] = "1";
            element.Properties["GeneratedCurtainPanelBaseCount"] = "1";
            element.Properties["GeneratedCurtainPanelColumns"] = "1";
            element.Properties["GeneratedCurtainPanelRows"] = "1";
            element.Properties["GeneratedCurtainPanelOpeningCount"] = "0";
            element.Properties["GeneratedCurtainPanelDepthM"] = "0.012";
            element.Properties["GeneratedCurtainPanelSourceLengthM"] = "1";
            element.Properties["GeneratedCurtainPanelHeightM"] = "1";
            element.Properties["GeneratedCurtainPanelConfigFingerprint"] = new string('a', 64);
            element.Properties["GeneratedCurtainPanelMode"] = "LinePanelSolids";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(Setup setup) =>
            new GeneratedCurtainPanelHealthService().Inspect(setup.Project);

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code, HealthSeverity severity)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == severity &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedCurtainPanelBuildStateCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedCurtainPanelBuildStateCanonicalitySmoke reported unexpected issue: " + code + ".");
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
