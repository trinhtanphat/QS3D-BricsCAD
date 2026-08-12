using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainFrameGeometrySnapshotCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedDepthFailsVisible();
            TrailingZeroLengthFailsVisible();
            ScientificHeightFailsVisible();
            InvalidDepthKeepsInvalidPrecedence();
            CanonicalStaleLengthKeepsStaleDiagnostic();
            CanonicalSnapshotsDoNotEmitCanonicality();
        }

        private static void PaddedDepthFailsVisible()
        {
            var setup = Create("DEPTH-PAD", " 0.05 ", "1", "2", "1", "2");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_DEPTH_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "CURTAIN_FRAME_DEPTH_INVALID", "A padded positive depth must remain numerically valid.");
        }

        private static void TrailingZeroLengthFailsVisible()
        {
            var setup = Create("LENGTH-ZERO", "0.05", "1.000", "2", "1", "2");
            RequireIssue(Inspect(setup), setup.Element.Id, "CURTAIN_FRAME_SOURCE_LENGTH_NON_CANONICAL", HealthSeverity.Error);
        }

        private static void ScientificHeightFailsVisible()
        {
            var setup = Create("HEIGHT-SCI", "0.05", "1", "2E0", "1", "2");
            RequireIssue(Inspect(setup), setup.Element.Id, "CURTAIN_FRAME_HEIGHT_NON_CANONICAL", HealthSeverity.Error);
        }

        private static void InvalidDepthKeepsInvalidPrecedence()
        {
            var setup = Create("DEPTH-INVALID", "NaN", "1", "2", "1", "2");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_DEPTH_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "CURTAIN_FRAME_DEPTH_NON_CANONICAL", "Invalid depth must not produce canonicality evidence before numeric validity is established.");
        }

        private static void CanonicalStaleLengthKeepsStaleDiagnostic()
        {
            var setup = Create("STALE", "0.05", "1", "2", "2", "2");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_SOURCE_LENGTH_STALE", HealthSeverity.Warning);
            EnsureAbsent(issues, "CURTAIN_FRAME_SOURCE_LENGTH_NON_CANONICAL", "Canonical stored length must remain canonical while stale comparison uses its parsed value.");
        }

        private static void CanonicalSnapshotsDoNotEmitCanonicality()
        {
            var setup = Create("CANONICAL", "0.05", "1", "2", "1", "2");
            var issues = Inspect(setup);
            EnsureAbsent(issues, "CURTAIN_FRAME_DEPTH_NON_CANONICAL", "Canonical generated depth must not produce canonicality evidence.");
            EnsureAbsent(issues, "CURTAIN_FRAME_SOURCE_LENGTH_NON_CANONICAL", "Canonical generated source length must not produce canonicality evidence.");
            EnsureAbsent(issues, "CURTAIN_FRAME_HEIGHT_NON_CANONICAL", "Canonical generated height must not produce canonicality evidence.");
        }

        private static Setup Create(string suffix, string depth, string storedLength, string storedHeight, string currentLength, string currentHeight)
        {
            var project = new ProjectState("P-Curtain-Geometry-" + suffix, "Curtain Frame geometry snapshot canonicality smoke");
            var element = new ProjectElement("Curtain-Geometry-" + suffix, ElementCategory.GlassWall);
            element.Properties["GeneratedCurtainFrameHandles"] = "A";
            element.Properties["GeneratedCurtainFrameCount"] = "1";
            element.Properties["GeneratedCurtainFrameOpeningCount"] = "0";
            element.Properties["GeneratedCurtainFrameMode"] = "LineFrameOverlay";
            element.Properties["GeneratedCurtainFrameDepthM"] = depth;
            element.Properties["GeneratedCurtainFrameSourceLengthM"] = storedLength;
            element.Properties["GeneratedCurtainFrameHeightM"] = storedHeight;
            element.Properties["LengthM"] = currentLength;
            element.Properties["HeightM"] = currentHeight;
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
            throw new InvalidOperationException("Expected Curtain Frame geometry snapshot issue was not reported: " + code + ".");
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
