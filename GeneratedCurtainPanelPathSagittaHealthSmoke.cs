using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainPanelPathSagittaHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalPathSagittaRemainsAccepted();
            MissingPathSagittaFailsVisible();
            BelowMinimumPathSagittaFailsVisible();
            NonFinitePathSagittaFailsVisible();
            PathSagittaAliasesFailVisible();
            LineModeDoesNotRequirePathSagitta();
        }

        private static void CanonicalPathSagittaRemainsAccepted()
        {
            var setup = CreatePath("CANONICAL", "0.002");
            var issues = Inspect(setup);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SAGITTA_INVALID");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SAGITTA_NON_CANONICAL");
        }

        private static void MissingPathSagittaFailsVisible()
        {
            var setup = CreatePath("MISSING", "0.002");
            setup.Element.Properties.Remove("GeneratedCurtainPanelPathSagittaM");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SAGITTA_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SAGITTA_NON_CANONICAL");
        }

        private static void BelowMinimumPathSagittaFailsVisible()
        {
            var setup = CreatePath("BELOW-MIN", "0.0000009");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SAGITTA_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SAGITTA_NON_CANONICAL");
        }

        private static void NonFinitePathSagittaFailsVisible()
        {
            foreach (var value in new[] { "NaN", "Infinity", "-Infinity" })
            {
                var setup = CreatePath("NONFINITE-" + value.Replace("-", "NEG-"), value);
                var issues = Inspect(setup);
                RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SAGITTA_INVALID", HealthSeverity.Warning);
                ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SAGITTA_NON_CANONICAL");
            }
        }

        private static void PathSagittaAliasesFailVisible()
        {
            foreach (var value in new[] { "+0.002", " 0.002 ", "0.0020" })
            {
                var setup = CreatePath("ALIAS-" + value.Replace("+", "PLUS").Replace(" ", "PAD").Replace(".", "DOT"), value);
                var issues = Inspect(setup);
                RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SAGITTA_NON_CANONICAL", HealthSeverity.Error);
                ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SAGITTA_INVALID");
            }
        }

        private static void LineModeDoesNotRequirePathSagitta()
        {
            var setup = CreateBase("LINE");
            setup.Element.Properties["GeneratedCurtainPanelMode"] = "LinePanelSolids";
            var issues = Inspect(setup);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SAGITTA_INVALID");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SAGITTA_NON_CANONICAL");
        }

        private static Setup CreatePath(string suffix, string sagitta)
        {
            var setup = CreateBase(suffix);
            setup.Element.Properties["GeneratedCurtainPanelMode"] = "PathPanelSolids";
            setup.Element.Properties["GeneratedCurtainPanelSourceKind"] = "OpenPolyline";
            setup.Element.Properties["GeneratedCurtainPanelPathSegmentCount"] = "1";
            setup.Element.Properties["GeneratedCurtainPanelMappedCount"] = "0";
            setup.Element.Properties["GeneratedCurtainPanelPathSagittaM"] = sagitta;
            return setup;
        }

        private static Setup CreateBase(string suffix)
        {
            var project = new ProjectState("P-CURTAIN-PANEL-SAGITTA-" + suffix, "Curtain Panel path sagitta health");
            var element = new ProjectElement("E-CURTAIN-PANEL-SAGITTA-" + suffix, ElementCategory.GlassWall);
            element.Properties[GeneratedCurtainPanelHealthService.BuildStateKey] = GeneratedCurtainPanelHealthService.BuildCompleteValue;
            element.Properties["GeneratedCurtainPanelCount"] = "0";
            element.Properties["GeneratedCurtainPanelBaseCount"] = "1";
            element.Properties["GeneratedCurtainPanelColumns"] = "1";
            element.Properties["GeneratedCurtainPanelRows"] = "1";
            element.Properties["GeneratedCurtainPanelOpeningCount"] = "0";
            element.Properties["GeneratedCurtainPanelDepthM"] = "0.012";
            element.Properties["GeneratedCurtainPanelSourceLengthM"] = "1";
            element.Properties["GeneratedCurtainPanelHeightM"] = "1";
            element.Properties["GeneratedCurtainPanelAreaM2"] = "0";
            element.Properties["GeneratedCurtainPanelConfigFingerprint"] = new string('a', 64);
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
            throw new InvalidOperationException("GeneratedCurtainPanelPathSagittaHealthSmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedCurtainPanelPathSagittaHealthSmoke reported unexpected issue: " + code + ".");
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
