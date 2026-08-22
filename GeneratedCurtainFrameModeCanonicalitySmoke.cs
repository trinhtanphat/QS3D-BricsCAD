using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainFrameModeCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedCaseVariantLineModeFailsVisible();
            CaseVariantPathOpeningModeFailsVisibleWithoutSemanticMismatch();
            InvalidModeKeepsInvalidDiagnostic();
            CanonicalLineModeDoesNotEmitCanonicality();
            CanonicalOpeningAwareModeKeepsOpeningMismatch();
        }

        private static void PaddedCaseVariantLineModeFailsVisible()
        {
            var setup = Create("LINE-ALIAS", " lineframeoverlay ", "0");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_MODE_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "CURTAIN_FRAME_MODE_INVALID", "A line-mode alias must retain valid semantic mode meaning.");
            EnsureAbsent(issues, "CURTAIN_FRAME_OPENING_MODE_MISMATCH", "A non-opening line-mode alias with zero openings must not become an opening mismatch.");
        }

        private static void CaseVariantPathOpeningModeFailsVisibleWithoutSemanticMismatch()
        {
            var setup = Create("PATH-ALIAS", "pathframeoverlay.openingaware", "1");
            setup.Element.Properties["GeneratedCurtainFramePathSegmentCount"] = "1";
            setup.Element.Properties["GeneratedCurtainFrameMappedFrameCount"] = "1";
            setup.Element.Properties["GeneratedCurtainFrameSourceKind"] = "OpenPolyline";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_MODE_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "CURTAIN_FRAME_MODE_INVALID", "A path opening-aware alias must retain valid semantic mode meaning.");
            EnsureAbsent(issues, "CURTAIN_FRAME_OPENING_MODE_MISMATCH", "A path opening-aware alias with one opening must remain semantically opening-aware.");
        }

        private static void InvalidModeKeepsInvalidDiagnostic()
        {
            var setup = Create("INVALID", "Bogus", "0");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_MODE_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "CURTAIN_FRAME_MODE_NON_CANONICAL", "Unsupported mode values must stay invalid rather than being mislabeled as canonical aliases.");
        }

        private static void CanonicalLineModeDoesNotEmitCanonicality()
        {
            var setup = Create("CANONICAL", "LineFrameOverlay", "0");
            EnsureAbsent(Inspect(setup), "CURTAIN_FRAME_MODE_NON_CANONICAL", "Exact writer-owned line mode must not produce canonicality evidence.");
        }

        private static void CanonicalOpeningAwareModeKeepsOpeningMismatch()
        {
            var setup = Create("OPEN-MISMATCH", "LineFrameOverlay.OpeningAware", "0");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_OPENING_MODE_MISMATCH", HealthSeverity.Warning);
            EnsureAbsent(issues, "CURTAIN_FRAME_MODE_NON_CANONICAL", "Exact writer-owned opening-aware mode must remain canonical while exposing opening-count mismatch.");
        }

        private static Setup Create(string suffix, string mode, string openingCount)
        {
            var project = new ProjectState("P-Curtain-Mode-" + suffix, "Curtain Frame mode canonicality smoke");
            var element = new ProjectElement("Curtain-Mode-" + suffix, ElementCategory.GlassWall);
            element.Properties["GeneratedCurtainFrameHandles"] = "A";
            element.Properties["GeneratedCurtainFrameCount"] = "1";
            element.Properties["GeneratedCurtainFrameOpeningCount"] = openingCount;
            element.Properties["GeneratedCurtainFrameMode"] = mode;
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(Setup setup) =>
            new GeneratedCurtainFrameHealthService().Inspect(setup.Project);

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code, HealthSeverity severity)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == severity &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Curtain Frame mode health issue was not reported: " + code + ".");
        }

        private static void EnsureAbsent(IReadOnlyList<ModelHealthIssue> issues, string code, string message)
        {
            if (issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new InvalidOperationException(message);
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
