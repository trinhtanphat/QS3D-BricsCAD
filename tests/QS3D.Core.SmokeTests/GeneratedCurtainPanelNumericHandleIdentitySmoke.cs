using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainPanelNumericHandleIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NumericEquivalentLiveHandleIsAccepted();
            NumericEquivalentDuplicateSpellingsAreRejected();
            TrulyMissingLiveHandleIsRejected();
        }

        private static void NumericEquivalentLiveHandleIsAccepted()
        {
            var setup = Create("LIVE", "000A", "1");
            var beforeVersion = setup.Project.ChangeVersion;
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };

            var issues = new GeneratedCurtainPanelHealthService().Inspect(setup.Project, live);

            Equal(beforeVersion, setup.Project.ChangeVersion);
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_GENERATED_SOLID_MISSING");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_GENERATED_OWNERSHIP_CONFLICT");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_COUNT_MISMATCH");
        }

        private static void NumericEquivalentDuplicateSpellingsAreRejected()
        {
            var setup = Create("DUP", "A;000A", "1");
            var beforeVersion = setup.Project.ChangeVersion;
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "0xA" };

            var issues = new GeneratedCurtainPanelHealthService().Inspect(setup.Project, live);

            Equal(beforeVersion, setup.Project.ChangeVersion);
            RequireIssue(issues, setup.Element.Id, "DUPLICATE_CURTAIN_PANEL_GENERATED_HANDLE");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_GENERATED_SOLID_MISSING");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_PANEL_COUNT_MISMATCH");
        }

        private static void TrulyMissingLiveHandleIsRejected()
        {
            var setup = Create("MISSING", "000A", "1");
            var beforeVersion = setup.Project.ChangeVersion;
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "B" };

            var issues = new GeneratedCurtainPanelHealthService().Inspect(setup.Project, live);

            Equal(beforeVersion, setup.Project.ChangeVersion);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_PANEL_GENERATED_SOLID_MISSING");
        }

        private static Setup Create(string suffix, string handles, string count)
        {
            var project = new ProjectState("P-CURTAIN-NUMERIC-" + suffix, "Generated Curtain Panel numeric handle identity");
            var element = new ProjectElement("E-CURTAIN-NUMERIC-" + suffix, ElementCategory.GlassWall);
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
            element.Properties["GeneratedCurtainPanelAreaM2"] = "1";
            element.Properties["GeneratedCurtainPanelConfigFingerprint"] = new string('a', 64);
            element.Properties["GeneratedCurtainPanelMode"] = "LinePanelSolids";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Curtain Panel issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Unexpected Curtain Panel issue was reported: " + code + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
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
