using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainPathFrameHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("curtain-path-health", "Curtain Path Health");
            var curtain = new ProjectElement("CW-PATH", ElementCategory.GlassWall, string.Empty, string.Empty, string.Empty);
            curtain.Properties["GeneratedCurtainFrameHandles"] = "BA;BB;BC;BD";
            curtain.Properties["GeneratedCurtainFrameCount"] = "4";
            curtain.Properties["GeneratedCurtainFrameColumns"] = "1";
            curtain.Properties["GeneratedCurtainFrameRows"] = "1";
            curtain.Properties["GeneratedCurtainFrameBaseCount"] = "4";
            curtain.Properties["GeneratedCurtainFrameOpeningCount"] = "0";
            curtain.Properties["GeneratedCurtainFrameDepthM"] = "0.05";
            curtain.Properties["GeneratedCurtainFrameSourceLengthM"] = "1";
            curtain.Properties["GeneratedCurtainFrameHeightM"] = "1";
            curtain.Properties["GeneratedCurtainFrameMode"] = "PathFrameOverlay";
            curtain.Properties["GeneratedCurtainFrameSourceKind"] = "OpenPolyline";
            curtain.Properties["GeneratedCurtainFramePathSegmentCount"] = "2";
            curtain.Properties["GeneratedCurtainFrameMappedFrameCount"] = "4";
            curtain.Properties["LengthM"] = "1";
            curtain.Properties["HeightM"] = "1";
            curtain.Properties["GeneratedCurtainFrameConfigFingerprint"] = CurtainWallFrameFingerprint.Compute(new CurtainWallFrameFingerprintInput
            {
                LengthM = 1d,
                HeightM = 1d,
                BottomOffsetM = 0d,
                MaxPanelWidthM = 1.2d,
                MaxPanelHeightM = 1.5d,
                PerimeterFrameWidthM = 0.05d,
                MullionWidthM = 0.05d,
                TransomWidthM = 0.05d,
                FrameDepthM = 0.05d
            });
            project.Elements.Add(curtain);

            var issues = new GeneratedCurtainFrameHealthService().Inspect(project);
            var forbidden = new[]
            {
                "CURTAIN_FRAME_MODE_INVALID",
                "CURTAIN_FRAME_PATH_SEGMENTS_INVALID",
                "CURTAIN_FRAME_MAPPED_COUNT_INVALID",
                "CURTAIN_FRAME_PATH_SOURCE_KIND_INVALID"
            };
            if (issues.Any(x => x.ElementId == curtain.Id && forbidden.Contains(x.Code)))
                throw new Exception("Curtain path frame metadata should be accepted by generated-frame health.");
        }
    }
}
