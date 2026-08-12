using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainPanelPositiveFloatCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AliasesFailVisible();
            CanonicalValuesRemainAccepted();
            InvalidValuesKeepInvalidPrecedence();
        }

        private static void AliasesFailVisible()
        {
            RequireAlias("DEPTH", "GeneratedCurtainPanelDepthM", "0.0120", "CURTAIN_PANEL_DEPTH_INVALID");
            RequireAlias("LENGTH", "GeneratedCurtainPanelSourceLengthM", "+1", "CURTAIN_PANEL_SOURCE_LENGTH_INVALID");
            RequireAlias("HEIGHT", "GeneratedCurtainPanelHeightM", " 1 ", "CURTAIN_PANEL_HEIGHT_INVALID");
        }

        private static void RequireAlias(string suffix, string key, string value, string invalidCode)
        {
            var setup = Create(suffix);
            setup.Element.Properties[key] = value;
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_FLOAT_METADATA_NON_CANONICAL", HealthSeverity.Error);
            ForbidIssue(issues, setup.Element.Id, invalidCode);
        }

        private static void CanonicalValuesRemainAccepted()
        {
            var setup = Create("CANONICAL");
            var issues = Inspect(setup);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_FLOAT_METADATA_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_DEPTH_INVALID");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_SOURCE_LENGTH_INVALID");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_HEIGHT_INVALID");
        }

        private static void InvalidValuesKeepInvalidPrecedence()
        {
            var cases = new[]
            {
                new[] { "DEPTH-ZERO", "GeneratedCurtainPanelDepthM", "0", "CURTAIN_PANEL_DEPTH_INVALID" },
                new[] { "LENGTH-NAN", "GeneratedCurtainPanelSourceLengthM", "NaN", "CURTAIN_PANEL_SOURCE_LENGTH_INVALID" },
                new[] { "HEIGHT-INF", "GeneratedCurtainPanelHeightM", "Infinity", "CURTAIN_PANEL_HEIGHT_INVALID" }
            };
            foreach (var item in cases)
            {
                var setup = Create(item[0]);
                setup.Element.Properties[item[1]] = item[2];
                var issues = Inspect(setup);
                RequireIssue(issues, setup.Element.Id, item[3], HealthSeverity.Warning);
                ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_FLOAT_METADATA_NON_CANONICAL");
            }
        }

        private static Setup Create(string suffix)
        {
            var project = new ProjectState("P-CURTAIN-PANEL-FLOAT-" + suffix, "Curtain Panel positive float canonicality");
            var element = new ProjectElement("E-CURTAIN-PANEL-FLOAT-" + suffix, ElementCategory.GlassWall);
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
            element.Properties["GeneratedCurtainPanelMode"] = "LinePanelSolids";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(Setup setup) =>
            new GeneratedCurtainPanelHealthService().Inspect(setup.Project);

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code, HealthSeverity severity)
        {
            if (issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && x.Severity == severity && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedCurtainPanelPositiveFloatCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedCurtainPanelPositiveFloatCanonicalitySmoke reported unexpected issue: " + code + ".");
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
