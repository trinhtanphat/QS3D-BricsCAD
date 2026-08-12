using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainPanelFingerprintCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            UppercaseFingerprintFailsVisible();
            PaddedFingerprintFailsVisible();
            CanonicalLowercaseFingerprintRemainsAccepted();
            InvalidShapeKeepsInvalidPrecedence();
            MissingFingerprintKeepsInvalidPrecedence();
        }

        private static void UppercaseFingerprintFailsVisible()
        {
            var setup = Create("UPPER", new string('A', 64));
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_CONFIG_FINGERPRINT_NON_CANONICAL", HealthSeverity.Error);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_CONFIG_FINGERPRINT_INVALID");
        }

        private static void PaddedFingerprintFailsVisible()
        {
            var setup = Create("PAD", " " + new string('a', 64) + " ");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_CONFIG_FINGERPRINT_NON_CANONICAL", HealthSeverity.Error);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_CONFIG_FINGERPRINT_INVALID");
        }

        private static void CanonicalLowercaseFingerprintRemainsAccepted()
        {
            var setup = Create("CANONICAL", new string('a', 64));
            var issues = Inspect(setup);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_CONFIG_FINGERPRINT_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_CONFIG_FINGERPRINT_INVALID");
        }

        private static void InvalidShapeKeepsInvalidPrecedence()
        {
            var setup = Create("INVALID", "xyz");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_CONFIG_FINGERPRINT_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_CONFIG_FINGERPRINT_NON_CANONICAL");
        }

        private static void MissingFingerprintKeepsInvalidPrecedence()
        {
            var setup = Create("MISSING", new string('a', 64));
            setup.Element.Properties.Remove("GeneratedCurtainPanelConfigFingerprint");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_CONFIG_FINGERPRINT_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_CONFIG_FINGERPRINT_NON_CANONICAL");
        }

        private static Setup Create(string suffix, string fingerprint)
        {
            var project = new ProjectState("P-CURTAIN-PANEL-FP-" + suffix, "Curtain Panel fingerprint canonicality");
            var element = new ProjectElement("E-CURTAIN-PANEL-FP-" + suffix, ElementCategory.GlassWall);
            element.Properties[GeneratedCurtainPanelHealthService.BuildStateKey] = GeneratedCurtainPanelHealthService.BuildCompleteValue;
            element.Properties["GeneratedCurtainPanelCount"] = "0";
            element.Properties["GeneratedCurtainPanelBaseCount"] = "1";
            element.Properties["GeneratedCurtainPanelColumns"] = "1";
            element.Properties["GeneratedCurtainPanelRows"] = "1";
            element.Properties["GeneratedCurtainPanelOpeningCount"] = "0";
            element.Properties["GeneratedCurtainPanelDepthM"] = "0.012";
            element.Properties["GeneratedCurtainPanelSourceLengthM"] = "1";
            element.Properties["GeneratedCurtainPanelHeightM"] = "1";
            element.Properties["GeneratedCurtainPanelConfigFingerprint"] = fingerprint;
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
            throw new InvalidOperationException("GeneratedCurtainPanelFingerprintCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedCurtainPanelFingerprintCanonicalitySmoke reported unexpected issue: " + code + ".");
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
