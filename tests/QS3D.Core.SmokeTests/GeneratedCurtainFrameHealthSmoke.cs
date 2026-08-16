using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainFrameHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            LaterGeneratedOwnerStillConflictsWithCurtainFrames();
            ReducedPhysicalFrameCountMatchesNonZeroWidths();
        }

        private static void LaterGeneratedOwnerStillConflictsWithCurtainFrames()
        {
            var project = new ProjectState("curtain-health-order", "Curtain Health Order");
            var curtain = new ProjectElement("CW1", ElementCategory.GlassWall, string.Empty, string.Empty, string.Empty);
            curtain.Properties["GeneratedCurtainFrameHandles"] = "AB;AC;AD;AE";
            curtain.Properties["GeneratedCurtainFrameCount"] = "4";
            curtain.Properties["GeneratedCurtainFrameColumns"] = "1";
            curtain.Properties["GeneratedCurtainFrameRows"] = "1";
            curtain.Properties["GeneratedCurtainFrameDepthM"] = "0.05";
            curtain.Properties["GeneratedCurtainFrameSourceLengthM"] = "1";
            curtain.Properties["GeneratedCurtainFrameHeightM"] = "1";
            curtain.Properties["GeneratedCurtainFrameConfigFingerprint"] = "legacy-smoke";
            curtain.Properties["GeneratedCurtainFrameMode"] = "LineFrameOverlay";
            curtain.Properties["LengthM"] = "1";
            curtain.Properties["HeightM"] = "1";
            project.Elements.Add(curtain);

            var other = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            other.Properties["GeneratedSolidHandle"] = "AB";
            project.Elements.Add(other);

            var issues = new GeneratedCurtainFrameHealthService().Inspect(project);
            if (!issues.Any(x => x.ElementId == "CW1" && x.Code == "CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT"))
                throw new Exception("Curtain health missed a later conflicting GeneratedSolidHandle claim.");
        }

        private static void ReducedPhysicalFrameCountMatchesNonZeroWidths()
        {
            var project = new ProjectState("curtain-health-zero-width", "Curtain Health Zero Width");
            var curtain = new ProjectElement("CW-ZERO-WIDTH", ElementCategory.GlassWall, string.Empty, string.Empty, string.Empty);
            var config = new CurtainWallFrameFingerprintInput
            {
                LengthM = 3d,
                HeightM = 2d,
                BottomOffsetM = 0d,
                MaxPanelWidthM = 1d,
                MaxPanelHeightM = 1d,
                PerimeterFrameWidthM = 0d,
                MullionWidthM = 0.05d,
                TransomWidthM = 0d,
                FrameDepthM = 0.05d
            };

            curtain.Properties["GeneratedCurtainFrameHandles"] = "AB;AC";
            curtain.Properties["GeneratedCurtainFrameCount"] = "2";
            curtain.Properties["GeneratedCurtainFrameColumns"] = "3";
            curtain.Properties["GeneratedCurtainFrameRows"] = "2";
            curtain.Properties["GeneratedCurtainFrameBaseCount"] = "2";
            curtain.Properties["GeneratedCurtainFrameOpeningCount"] = "0";
            curtain.Properties["GeneratedCurtainFrameDepthM"] = "0.05";
            curtain.Properties["GeneratedCurtainFrameSourceLengthM"] = "3";
            curtain.Properties["GeneratedCurtainFrameHeightM"] = "2";
            curtain.Properties["GeneratedCurtainFrameConfigFingerprint"] = CurtainWallFrameFingerprint.Compute(config);
            curtain.Properties["GeneratedCurtainFrameMode"] = "LineFrameOverlay";
            curtain.Properties["LengthM"] = "3";
            curtain.Properties["HeightM"] = "2";
            curtain.Properties["CurtainMaxPanelWidthM"] = "1";
            curtain.Properties["CurtainMaxPanelHeightM"] = "1";
            curtain.Properties["CurtainPerimeterFrameWidthM"] = "0";
            curtain.Properties["CurtainMullionWidthM"] = "0.05";
            curtain.Properties["CurtainTransomWidthM"] = "0";
            curtain.Properties["CurtainFrameDepthM"] = "0.05";
            project.Elements.Add(curtain);

            var issues = new GeneratedCurtainFrameHealthService().Inspect(project);
            if (issues.Any(x => x.ElementId == curtain.Id && x.Code == "CURTAIN_FRAME_BASE_COUNT_INVALID"))
                throw new Exception("Curtain health rejected a physical base-frame count produced by zero-width frame omission.");
            if (issues.Any(x => x.ElementId == curtain.Id && x.Code == "CURTAIN_FRAME_GRID_COUNT_MISMATCH"))
                throw new Exception("Curtain health still assumes every conceptual grid boundary creates a physical frame solid.");
        }
    }
}
