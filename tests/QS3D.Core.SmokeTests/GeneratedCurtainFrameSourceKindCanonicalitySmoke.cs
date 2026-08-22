using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainFrameSourceKindCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedPathSourceKindFailsVisible();
            CaseVariantPathSourceKindFailsVisible();
            CanonicalPathSourceKindStaysCanonical();
            InvalidPathSourceKindKeepsInvalidDiagnostic();
            LineModeIgnoresPathSourceKind();
        }

        private static void PaddedPathSourceKindFailsVisible()
        {
            var setup = Create("PAD", "PathFrameOverlay", " OpenPolyline ");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_PATH_SOURCE_KIND_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "CURTAIN_FRAME_PATH_SOURCE_KIND_INVALID", "Padded OpenPolyline alias must remain the same path source kind after normalization.");
        }

        private static void CaseVariantPathSourceKindFailsVisible()
        {
            var setup = Create("CASE", "PathFrameOverlay", "openpolyline");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_PATH_SOURCE_KIND_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "CURTAIN_FRAME_PATH_SOURCE_KIND_INVALID", "Case-only OpenPolyline alias must remain semantically valid.");
        }

        private static void CanonicalPathSourceKindStaysCanonical()
        {
            var setup = Create("CANONICAL", "PathFrameOverlay", "OpenPolyline");
            var issues = Inspect(setup);
            EnsureAbsent(issues, "CURTAIN_FRAME_PATH_SOURCE_KIND_NON_CANONICAL", "Exact writer-owned OpenPolyline token must not produce canonicality evidence.");
            EnsureAbsent(issues, "CURTAIN_FRAME_PATH_SOURCE_KIND_INVALID", "Exact writer-owned OpenPolyline token must remain valid.");
        }

        private static void InvalidPathSourceKindKeepsInvalidDiagnostic()
        {
            var setup = Create("INVALID", "PathFrameOverlay", "Spline");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_PATH_SOURCE_KIND_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "CURTAIN_FRAME_PATH_SOURCE_KIND_NON_CANONICAL", "Unsupported source kinds must stay invalid rather than being mislabeled as canonical aliases.");
        }

        private static void LineModeIgnoresPathSourceKind()
        {
            var setup = Create("LINE", "LineFrameOverlay", " spline ");
            var issues = Inspect(setup);
            EnsureAbsent(issues, "CURTAIN_FRAME_PATH_SOURCE_KIND_NON_CANONICAL", "Line-mode frames must not validate path-only source-kind metadata.");
            EnsureAbsent(issues, "CURTAIN_FRAME_PATH_SOURCE_KIND_INVALID", "Line-mode frames must not validate path-only source-kind metadata.");
        }

        private static Setup Create(string suffix, string mode, string sourceKind)
        {
            var project = new ProjectState("P-Curtain-SourceKind-" + suffix, "Curtain Frame source-kind canonicality smoke");
            var element = new ProjectElement("Curtain-SourceKind-" + suffix, ElementCategory.GlassWall);
            element.Properties["GeneratedCurtainFrameHandles"] = "A";
            element.Properties["GeneratedCurtainFrameCount"] = "1";
            element.Properties["GeneratedCurtainFrameOpeningCount"] = "0";
            element.Properties["GeneratedCurtainFrameMode"] = mode;
            element.Properties["GeneratedCurtainFramePathSegmentCount"] = "1";
            element.Properties["GeneratedCurtainFrameMappedFrameCount"] = "1";
            element.Properties["GeneratedCurtainFrameSourceKind"] = sourceKind;
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
            throw new InvalidOperationException("Expected Curtain Frame source-kind health issue was not reported: " + code + ".");
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
