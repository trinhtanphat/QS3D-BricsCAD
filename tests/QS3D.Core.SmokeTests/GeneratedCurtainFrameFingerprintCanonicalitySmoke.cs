using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainFrameFingerprintCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            UppercaseFingerprintFailsVisibleWithoutStale();
            PaddedFingerprintFailsVisibleWithoutStale();
            DifferentFingerprintKeepsStaleDiagnostic();
            MissingFingerprintKeepsMissingPrecedence();
            CanonicalFingerprintStaysCanonical();
        }

        private static void UppercaseFingerprintFailsVisibleWithoutStale()
        {
            var setup = Create("UPPER");
            setup.Element.Properties["GeneratedCurtainFrameConfigFingerprint"] = setup.Fingerprint.ToUpperInvariant();
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_CONFIG_FINGERPRINT_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "CURTAIN_FRAME_CONFIG_STALE", "Case-only fingerprint alias must still represent the current config.");
        }

        private static void PaddedFingerprintFailsVisibleWithoutStale()
        {
            var setup = Create("PAD");
            setup.Element.Properties["GeneratedCurtainFrameConfigFingerprint"] = " " + setup.Fingerprint + " ";
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_CONFIG_FINGERPRINT_NON_CANONICAL", HealthSeverity.Error);
            EnsureAbsent(issues, "CURTAIN_FRAME_CONFIG_STALE", "Padded fingerprint alias must still represent the current config after normalization.");
        }

        private static void DifferentFingerprintKeepsStaleDiagnostic()
        {
            var setup = Create("STALE");
            var first = setup.Fingerprint[0] == '0' ? '1' : '0';
            setup.Element.Properties["GeneratedCurtainFrameConfigFingerprint"] = first + setup.Fingerprint.Substring(1);
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_CONFIG_STALE", HealthSeverity.Warning);
            EnsureAbsent(issues, "CURTAIN_FRAME_CONFIG_FINGERPRINT_NON_CANONICAL", "A genuinely different canonical-shaped digest must stay stale rather than being mislabeled as an alias.");
        }

        private static void MissingFingerprintKeepsMissingPrecedence()
        {
            var setup = Create("MISSING");
            setup.Element.Properties.Remove("GeneratedCurtainFrameConfigFingerprint");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_CONFIG_FINGERPRINT_MISSING", HealthSeverity.Warning);
            EnsureAbsent(issues, "CURTAIN_FRAME_CONFIG_FINGERPRINT_NON_CANONICAL", "Missing fingerprint must retain missing precedence.");
        }

        private static void CanonicalFingerprintStaysCanonical()
        {
            var setup = Create("CANONICAL");
            var issues = Inspect(setup);
            EnsureAbsent(issues, "CURTAIN_FRAME_CONFIG_FINGERPRINT_NON_CANONICAL", "Exact lowercase writer-owned fingerprint must not produce canonicality evidence.");
            EnsureAbsent(issues, "CURTAIN_FRAME_CONFIG_STALE", "Exact current fingerprint must not be stale.");
        }

        private static Setup Create(string suffix)
        {
            var project = new ProjectState("P-Curtain-Fingerprint-" + suffix, "Curtain Frame fingerprint canonicality smoke");
            var element = new ProjectElement("Curtain-Fingerprint-" + suffix, ElementCategory.GlassWall);
            element.Properties["GeneratedCurtainFrameHandles"] = "A";
            element.Properties["GeneratedCurtainFrameCount"] = "1";
            element.Properties["GeneratedCurtainFrameOpeningCount"] = "0";
            element.Properties["GeneratedCurtainFrameMode"] = "LineFrameOverlay";
            element.Properties["GeneratedCurtainFrameDepthM"] = "0.05";
            element.Properties["GeneratedCurtainFrameSourceLengthM"] = "1";
            element.Properties["GeneratedCurtainFrameHeightM"] = "2";
            element.Properties["LengthM"] = "1";
            element.Properties["HeightM"] = "2";

            var fingerprint = CurtainWallFrameFingerprint.Compute(new CurtainWallFrameFingerprintInput
            {
                LengthM = 1d,
                HeightM = 2d,
                BottomOffsetM = 0d,
                MaxPanelWidthM = 1.2d,
                MaxPanelHeightM = 1.5d,
                PerimeterFrameWidthM = 0.05d,
                MullionWidthM = 0.05d,
                TransomWidthM = 0.05d,
                FrameDepthM = 0.05d
            });
            element.Properties["GeneratedCurtainFrameConfigFingerprint"] = fingerprint;
            project.Elements.Add(element);
            return new Setup(project, element, fingerprint);
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
            throw new InvalidOperationException("Expected Curtain Frame fingerprint issue was not reported: " + code + ".");
        }

        private static void EnsureAbsent(IReadOnlyList<ModelHealthIssue> issues, string code, string message)
        {
            if (issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new InvalidOperationException(message);
        }

        private sealed class Setup
        {
            public Setup(ProjectState project, ProjectElement element, string fingerprint)
            {
                Project = project;
                Element = element;
                Fingerprint = fingerprint;
            }

            public ProjectState Project { get; }
            public ProjectElement Element { get; }
            public string Fingerprint { get; }
        }
    }
}
