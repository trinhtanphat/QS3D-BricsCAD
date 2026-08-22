using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainPanelIntegerCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LeadingZeroFailsVisible();
            ExplicitPlusFailsVisible();
            SurroundingWhitespaceFailsVisible();
            PathIntegerAliasFailsVisible();
            CanonicalIntegersRemainAccepted();
        }

        private static void LeadingZeroFailsVisible()
        {
            var setup = CreateLine("LEADING-ZERO");
            setup.Element.Properties["GeneratedCurtainPanelColumns"] = "01";
            RequireCanonicality(setup);
            ForbidIssue(setup, "CURTAIN_PANEL_COLUMNS_INVALID");
        }

        private static void ExplicitPlusFailsVisible()
        {
            var setup = CreateLine("PLUS");
            setup.Element.Properties["GeneratedCurtainPanelCount"] = "+0";
            RequireCanonicality(setup);
            ForbidIssue(setup, "CURTAIN_PANEL_COUNT_INVALID");
            ForbidIssue(setup, "CURTAIN_PANEL_COUNT_MISMATCH");
        }

        private static void SurroundingWhitespaceFailsVisible()
        {
            var setup = CreateLine("WHITESPACE");
            setup.Element.Properties["GeneratedCurtainPanelOpeningCount"] = " 0 ";
            RequireCanonicality(setup);
            ForbidIssue(setup, "CURTAIN_PANEL_OPENING_COUNT_INVALID");
            ForbidIssue(setup, "CURTAIN_PANEL_OPENING_MODE_MISMATCH");
        }

        private static void PathIntegerAliasFailsVisible()
        {
            var setup = CreateLine("PATH");
            setup.Element.Properties["GeneratedCurtainPanelMode"] = "PathPanelSolids";
            setup.Element.Properties["GeneratedCurtainPanelSourceKind"] = "OpenPolyline";
            setup.Element.Properties["GeneratedCurtainPanelPathSegmentCount"] = "01";
            setup.Element.Properties["GeneratedCurtainPanelMappedCount"] = "0";
            RequireCanonicality(setup);
            ForbidIssue(setup, "CURTAIN_PANEL_PATH_SEGMENTS_INVALID");
        }

        private static void CanonicalIntegersRemainAccepted()
        {
            var setup = CreateLine("CANONICAL");
            var issues = Inspect(setup);
            if (issues.Any(x => string.Equals(x.Code, "CURTAIN_PANEL_INTEGER_METADATA_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Canonical Curtain Panel integer snapshots must not report a canonicality error.");
        }

        private static Setup CreateLine(string suffix)
        {
            var project = new ProjectState("P-CURTAIN-PANEL-INT-" + suffix, "Curtain Panel integer canonicality");
            var element = new ProjectElement("E-CURTAIN-PANEL-INT-" + suffix, ElementCategory.GlassWall);
            element.Properties[GeneratedCurtainPanelHealthService.BuildStateKey] = GeneratedCurtainPanelHealthService.BuildCompleteValue;
            element.Properties["GeneratedCurtainPanelCount"] = "0";
            element.Properties["GeneratedCurtainPanelBaseCount"] = "1";
            element.Properties["GeneratedCurtainPanelColumns"] = "1";
            element.Properties["GeneratedCurtainPanelRows"] = "1";
            element.Properties["GeneratedCurtainPanelOpeningCount"] = "0";
            element.Properties["GeneratedCurtainPanelDepthM"] = "0.012";
            element.Properties["GeneratedCurtainPanelSourceLengthM"] = "1";
            element.Properties["GeneratedCurtainPanelHeightM"] = "1";
            element.Properties["GeneratedCurtainPanelConfigFingerprint"] = new string('A', 64);
            element.Properties["GeneratedCurtainPanelMode"] = "LinePanelSolids";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(Setup setup) =>
            new GeneratedCurtainPanelHealthService().Inspect(setup.Project);

        private static void RequireCanonicality(Setup setup)
        {
            var issues = Inspect(setup);
            if (!issues.Any(x =>
                string.Equals(x.Code, "CURTAIN_PANEL_INTEGER_METADATA_NON_CANONICAL", StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, setup.Element.Id, StringComparison.Ordinal)))
                throw new InvalidOperationException("Expected Curtain Panel integer canonicality issue was not reported.");
        }

        private static void ForbidIssue(Setup setup, string code)
        {
            var issues = Inspect(setup);
            if (issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, setup.Element.Id, StringComparison.Ordinal)))
                throw new InvalidOperationException("Unexpected Curtain Panel health issue was reported: " + code + ".");
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
