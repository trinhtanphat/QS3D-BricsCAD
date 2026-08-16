using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainFrameIntegerSnapshotCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LeadingZeroCountFailsVisible();
            SignedZeroOpeningCountFailsVisible();
            LeadingZeroColumnsFailsVisible();
            PathSegmentAliasFailsVisible();
            InvalidCountKeepsInvalidPrecedence();
            CanonicalCountMismatchKeepsMismatch();
            CanonicalIntegerSnapshotsStayCanonical();
        }

        private static void LeadingZeroCountFailsVisible()
        {
            var setup = Create("COUNT");
            setup.Element.Properties["GeneratedCurtainFrameCount"] = "01";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_INTEGER_METADATA_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "CURTAIN_FRAME_COUNT_INVALID", "A leading-zero positive count must remain numerically valid.");
            EnsureAbsent(issues, "CURTAIN_FRAME_COUNT_MISMATCH", "Parsed leading-zero count must still match the live generated-handle count.");
        }

        private static void SignedZeroOpeningCountFailsVisible()
        {
            var setup = Create("OPENING");
            setup.Element.Properties["GeneratedCurtainFrameOpeningCount"] = "+0";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_INTEGER_METADATA_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "CURTAIN_FRAME_OPENING_COUNT_INVALID", "Signed zero is in-domain but not writer-canonical.");
        }

        private static void LeadingZeroColumnsFailsVisible()
        {
            var setup = Create("COLUMNS");
            setup.Element.Properties["GeneratedCurtainFrameColumns"] = "01";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_INTEGER_METADATA_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "CURTAIN_FRAME_COLUMNS_INVALID", "Leading-zero columns must remain numerically valid.");
            EnsureAbsent(issues, "CURTAIN_FRAME_GRID_COUNT_MISMATCH", "Parsed columns must continue to drive base-frame arithmetic.");
        }

        private static void PathSegmentAliasFailsVisible()
        {
            var setup = Create("PATH");
            setup.Element.Properties["GeneratedCurtainFrameMode"] = "PathFrameOverlay";
            setup.Element.Properties["GeneratedCurtainFrameSourceKind"] = "OpenPolyline";
            setup.Element.Properties["GeneratedCurtainFramePathSegmentCount"] = "01";
            setup.Element.Properties["GeneratedCurtainFrameMappedFrameCount"] = "1";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_INTEGER_METADATA_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "CURTAIN_FRAME_PATH_SEGMENTS_INVALID", "Leading-zero path segment count must remain numerically valid.");
        }

        private static void InvalidCountKeepsInvalidPrecedence()
        {
            var setup = Create("INVALID");
            setup.Element.Properties["GeneratedCurtainFrameCount"] = "-1";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_COUNT_INVALID", HealthSeverity.Warning);
            EnsureAbsent(issues, "CURTAIN_FRAME_INTEGER_METADATA_NON_CANONICAL", "An out-of-domain negative count must not produce canonicality evidence before validity is established.");
        }

        private static void CanonicalCountMismatchKeepsMismatch()
        {
            var setup = Create("MISMATCH");
            setup.Element.Properties["GeneratedCurtainFrameCount"] = "2";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_COUNT_MISMATCH", HealthSeverity.Warning);
            EnsureAbsent(issues, "CURTAIN_FRAME_INTEGER_METADATA_NON_CANONICAL", "Canonical count text must remain canonical while mismatch uses its parsed value.");
        }

        private static void CanonicalIntegerSnapshotsStayCanonical()
        {
            var setup = Create("CANONICAL");
            EnsureAbsent(Inspect(setup), "CURTAIN_FRAME_INTEGER_METADATA_NON_CANONICAL", "Exact writer-owned generated integer strings must not produce canonicality evidence.");
        }

        private static Setup Create(string suffix)
        {
            var project = new ProjectState("P-Curtain-Integer-" + suffix, "Curtain Frame integer snapshot canonicality smoke");
            var element = new ProjectElement("Curtain-Integer-" + suffix, ElementCategory.GlassWall);
            element.Properties["GeneratedCurtainFrameHandles"] = "A";
            element.Properties["GeneratedCurtainFrameCount"] = "1";
            element.Properties["GeneratedCurtainFrameColumns"] = "1";
            element.Properties["GeneratedCurtainFrameRows"] = "1";
            element.Properties["GeneratedCurtainFrameBaseCount"] = "4";
            element.Properties["GeneratedCurtainFrameOpeningCount"] = "0";
            element.Properties["GeneratedCurtainFrameMode"] = "LineFrameOverlay";
            element.Properties["GeneratedCurtainFrameDepthM"] = "0.05";
            element.Properties["GeneratedCurtainFrameSourceLengthM"] = "1";
            element.Properties["GeneratedCurtainFrameHeightM"] = "2";
            element.Properties["LengthM"] = "1";
            element.Properties["HeightM"] = "2";
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
            throw new InvalidOperationException("Expected Curtain Frame integer snapshot issue was not reported: " + code + ".");
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
