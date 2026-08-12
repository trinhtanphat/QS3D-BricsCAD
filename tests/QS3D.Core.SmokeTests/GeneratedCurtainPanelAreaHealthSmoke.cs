using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainPanelAreaHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalPositiveAreaRemainsAccepted();
            CanonicalZeroAreaRemainsAccepted();
            MissingAreaFailsVisible();
            MalformedAreaFailsVisible();
            NonFiniteAreaFailsVisible();
            NegativeAreaFailsVisible();
            NonCanonicalAliasesFailVisible();
        }

        private static void CanonicalPositiveAreaRemainsAccepted()
        {
            var setup = Create("POSITIVE", "0.5");
            var issues = Inspect(setup);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_INVALID");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_NON_CANONICAL");
        }

        private static void CanonicalZeroAreaRemainsAccepted()
        {
            var setup = Create("ZERO", "0");
            var issues = Inspect(setup);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_INVALID");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_NON_CANONICAL");
        }

        private static void MissingAreaFailsVisible()
        {
            var setup = Create("MISSING", "0");
            setup.Element.Properties.Remove("GeneratedCurtainPanelAreaM2");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_NON_CANONICAL");
        }

        private static void MalformedAreaFailsVisible()
        {
            var setup = Create("MALFORMED", "not-a-number");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_NON_CANONICAL");
        }

        private static void NonFiniteAreaFailsVisible()
        {
            foreach (var area in new[] { "NaN", "Infinity", "-Infinity" })
            {
                var setup = Create("NONFINITE-" + area.Replace("-", "NEG-"), area);
                var issues = Inspect(setup);
                RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_INVALID", HealthSeverity.Warning);
                ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_NON_CANONICAL");
            }
        }

        private static void NegativeAreaFailsVisible()
        {
            var setup = Create("NEGATIVE", "-1");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_NON_CANONICAL");
        }

        private static void NonCanonicalAliasesFailVisible()
        {
            foreach (var area in new[] { "+0.5", " 0.5 ", "0.50" })
            {
                var setup = Create("ALIAS-" + area.Replace("+", "PLUS").Replace(" ", "PAD").Replace(".", "DOT"), area);
                var issues = Inspect(setup);
                RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_NON_CANONICAL", HealthSeverity.Error);
                ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_AREA_INVALID");
            }
        }

        private static Setup Create(string suffix, string area)
        {
            var project = new ProjectState("P-CURTAIN-PANEL-AREA-" + suffix, "Curtain Panel area health");
            var element = new ProjectElement("E-CURTAIN-PANEL-AREA-" + suffix, ElementCategory.GlassWall);
            element.Properties[GeneratedCurtainPanelHealthService.BuildStateKey] = GeneratedCurtainPanelHealthService.BuildCompleteValue;
            element.Properties["GeneratedCurtainPanelCount"] = "0";
            element.Properties["GeneratedCurtainPanelBaseCount"] = "1";
            element.Properties["GeneratedCurtainPanelColumns"] = "1";
            element.Properties["GeneratedCurtainPanelRows"] = "1";
            element.Properties["GeneratedCurtainPanelOpeningCount"] = "0";
            element.Properties["GeneratedCurtainPanelDepthM"] = "0.012";
            element.Properties["GeneratedCurtainPanelSourceLengthM"] = "1";
            element.Properties["GeneratedCurtainPanelHeightM"] = "1";
            element.Properties["GeneratedCurtainPanelAreaM2"] = area;
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
            throw new InvalidOperationException("GeneratedCurtainPanelAreaHealthSmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedCurtainPanelAreaHealthSmoke reported unexpected issue: " + code + ".");
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
