using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainPanelTokenCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedLineModeFailsVisible();
            CaseVariedPathModeFailsVisible();
            PathSourceKindAliasFailsVisible();
            CanonicalTokensRemainAccepted();
            InvalidModeKeepsInvalidPrecedence();
            InvalidSourceKindKeepsInvalidPrecedence();
        }

        private static void PaddedLineModeFailsVisible()
        {
            var setup = Create("LINE-ALIAS", " linepanelsolids ", string.Empty);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_MODE_NON_CANONICAL", HealthSeverity.Error);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_MODE_INVALID");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_OPENING_MODE_MISMATCH");
        }

        private static void CaseVariedPathModeFailsVisible()
        {
            var setup = Create("PATH-ALIAS", "PATHPANELSOLIDS", "OpenPolyline");
            AddPathMetadata(setup.Element);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_MODE_NON_CANONICAL", HealthSeverity.Error);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_MODE_INVALID");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SOURCE_KIND_INVALID");
        }

        private static void PathSourceKindAliasFailsVisible()
        {
            var setup = Create("SOURCE-ALIAS", "PathPanelSolids", " openpolyline ");
            AddPathMetadata(setup.Element);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SOURCE_KIND_NON_CANONICAL", HealthSeverity.Error);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SOURCE_KIND_INVALID");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_MODE_NON_CANONICAL");
        }

        private static void CanonicalTokensRemainAccepted()
        {
            var line = Create("LINE-CANONICAL", "LinePanelSolids", string.Empty);
            var lineIssues = Inspect(line);
            ForbidIssue(lineIssues, line.Element.Id, "CURTAIN_PANEL_MODE_NON_CANONICAL");
            ForbidIssue(lineIssues, line.Element.Id, "CURTAIN_PANEL_MODE_INVALID");

            var path = Create("PATH-CANONICAL", "PathPanelSolids", "OpenPolyline");
            AddPathMetadata(path.Element);
            var pathIssues = Inspect(path);
            ForbidIssue(pathIssues, path.Element.Id, "CURTAIN_PANEL_MODE_NON_CANONICAL");
            ForbidIssue(pathIssues, path.Element.Id, "CURTAIN_PANEL_PATH_SOURCE_KIND_NON_CANONICAL");
            ForbidIssue(pathIssues, path.Element.Id, "CURTAIN_PANEL_PATH_SOURCE_KIND_INVALID");
        }

        private static void InvalidModeKeepsInvalidPrecedence()
        {
            var setup = Create("MODE-INVALID", "Unsupported", string.Empty);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_MODE_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_MODE_NON_CANONICAL");
        }

        private static void InvalidSourceKindKeepsInvalidPrecedence()
        {
            var setup = Create("SOURCE-INVALID", "PathPanelSolids", "Polyline");
            AddPathMetadata(setup.Element);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SOURCE_KIND_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_PATH_SOURCE_KIND_NON_CANONICAL");
        }

        private static Setup Create(string suffix, string mode, string sourceKind)
        {
            var project = new ProjectState("P-CURTAIN-PANEL-TOKEN-" + suffix, "Curtain Panel token canonicality");
            var element = new ProjectElement("E-CURTAIN-PANEL-TOKEN-" + suffix, ElementCategory.GlassWall);
            element.Properties[GeneratedCurtainPanelHealthService.BuildStateKey] = GeneratedCurtainPanelHealthService.BuildCompleteValue;
            element.Properties["GeneratedCurtainPanelCount"] = "0";
            element.Properties["GeneratedCurtainPanelBaseCount"] = "1";
            element.Properties["GeneratedCurtainPanelColumns"] = "1";
            element.Properties["GeneratedCurtainPanelRows"] = "1";
            element.Properties["GeneratedCurtainPanelOpeningCount"] = "0";
            element.Properties["GeneratedCurtainPanelDepthM"] = "0.012";
            element.Properties["GeneratedCurtainPanelSourceLengthM"] = "1";
            element.Properties["GeneratedCurtainPanelHeightM"] = "1";
            element.Properties["GeneratedCurtainPanelConfigFingerprint"] = new string('a', 64);
            element.Properties["GeneratedCurtainPanelMode"] = mode;
            if (sourceKind.Length > 0) element.Properties["GeneratedCurtainPanelSourceKind"] = sourceKind;
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void AddPathMetadata(ProjectElement element)
        {
            element.Properties["GeneratedCurtainPanelPathSegmentCount"] = "1";
            element.Properties["GeneratedCurtainPanelMappedCount"] = "0";
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
            throw new InvalidOperationException("GeneratedCurtainPanelTokenCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedCurtainPanelTokenCanonicalitySmoke reported unexpected issue: " + code + ".");
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
