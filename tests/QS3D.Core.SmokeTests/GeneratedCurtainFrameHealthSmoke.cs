using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainFrameHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            LaterGeneratedOwnerStillConflictsWithCurtainFrames();
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
    }
}
