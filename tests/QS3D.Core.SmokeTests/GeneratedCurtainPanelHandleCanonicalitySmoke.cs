using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainPanelHandleCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedHandleFailsVisibleButKeepsLiveLookup();
            LowercaseOwnerTokenFailsVisibleButKeepsLiveLookup();
            EmptyDelimiterTokenRemainsInvalid();
        }

        private static void PaddedHandleFailsVisibleButKeepsLiveLookup()
        {
            var setup = Create("PAD", " A ", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new GeneratedCurtainPanelHealthService().Inspect(setup.Project, live);

            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_GENERATED_HANDLE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_GENERATED_SOLID_MISSING");
            ForbidIssue(issues, setup.Element.Id, "INVALID_CURTAIN_PANEL_GENERATED_HANDLE");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_GENERATED_OWNERSHIP_CONFLICT");
        }

        private static void LowercaseOwnerTokenFailsVisibleButKeepsLiveLookup()
        {
            var setup = Create("LOWER", "a", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new GeneratedCurtainPanelHealthService().Inspect(setup.Project, live);

            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_GENERATED_HANDLE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "INVALID_CURTAIN_PANEL_GENERATED_HANDLE");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_GENERATED_SOLID_MISSING");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_GENERATED_OWNERSHIP_CONFLICT");
        }

        private static void EmptyDelimiterTokenRemainsInvalid()
        {
            var setup = Create("EMPTY", "A;;B", "2");
            var issues = new GeneratedCurtainPanelHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "INVALID_CURTAIN_PANEL_GENERATED_HANDLE");
        }

        private static Setup Create(string suffix, string handles, string count)
        {
            var project = new ProjectState("P-CURTAIN-PANEL-CANON-" + suffix, "Generated Curtain Panel handle canonicality");
            var element = new ProjectElement("E-CURTAIN-PANEL-CANON-" + suffix, ElementCategory.GlassWall);
            element.Properties[GeneratedCurtainPanelHealthService.HandlesKey] = handles;
            element.Properties[GeneratedCurtainPanelHealthService.BuildStateKey] = GeneratedCurtainPanelHealthService.BuildCompleteValue;
            element.Properties["GeneratedCurtainPanelCount"] = count;
            element.Properties["GeneratedCurtainPanelBaseCount"] = "1";
            element.Properties["GeneratedCurtainPanelColumns"] = "1";
            element.Properties["GeneratedCurtainPanelRows"] = "1";
            element.Properties["GeneratedCurtainPanelOpeningCount"] = "0";
            element.Properties["GeneratedCurtainPanelDepthM"] = "0.02";
            element.Properties["GeneratedCurtainPanelSourceLengthM"] = "1";
            element.Properties["GeneratedCurtainPanelHeightM"] = "1";
            element.Properties["GeneratedCurtainPanelConfigFingerprint"] = new string('A', 64);
            element.Properties["GeneratedCurtainPanelMode"] = "LinePanelSolids";
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
            throw new InvalidOperationException("GeneratedCurtainPanelHandleCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedCurtainPanelHandleCanonicalitySmoke unexpected issue was reported: " + code + ".");
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
