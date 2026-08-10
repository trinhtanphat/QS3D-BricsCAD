using System;
using System.Collections.Generic;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainFrameOpeningHealthSmoke
    {
        public static void Run()
        {
            OpeningFragmentsMayDifferFromBaseGridCount();
            OpeningModeMismatchIsReported();
            BaseGridCountMismatchIsReported();
        }

        private static void OpeningFragmentsMayDifferFromBaseGridCount()
        {
            var project = Project(out var wall);
            SeedOpeningAware(wall);
            var issues = new GeneratedCurtainFrameHealthService().Inspect(project);
            NotContains(issues, "CURTAIN_FRAME_COUNT_MISMATCH");
            NotContains(issues, "CURTAIN_FRAME_GRID_COUNT_MISMATCH");
            NotContains(issues, "CURTAIN_FRAME_MODE_INVALID");
            NotContains(issues, "CURTAIN_FRAME_OPENING_MODE_MISMATCH");
        }

        private static void OpeningModeMismatchIsReported()
        {
            var project = Project(out var wall);
            SeedOpeningAware(wall);
            wall.Properties["GeneratedCurtainFrameMode"] = "LineFrameOverlay";
            var issues = new GeneratedCurtainFrameHealthService().Inspect(project);
            Contains(issues, "CURTAIN_FRAME_OPENING_MODE_MISMATCH");
        }

        private static void BaseGridCountMismatchIsReported()
        {
            var project = Project(out var wall);
            SeedOpeningAware(wall);
            wall.Properties["GeneratedCurtainFrameBaseCount"] = "5";
            var issues = new GeneratedCurtainFrameHealthService().Inspect(project);
            Contains(issues, "CURTAIN_FRAME_GRID_COUNT_MISMATCH");
        }

        private static ProjectState Project(out ProjectElement wall)
        {
            var project = new ProjectState("CURTAIN-OPENING-HEALTH", "Curtain opening health");
            wall = new ProjectElement("GW1", ElementCategory.GlassWall, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(wall);
            return project;
        }

        private static void SeedOpeningAware(ProjectElement wall)
        {
            wall.Properties["GeneratedCurtainFrameHandles"] = "A1;A2;A3;A4;A5";
            wall.Properties["GeneratedCurtainFrameCount"] = "5";
            wall.Properties["GeneratedCurtainFrameBaseCount"] = "4";
            wall.Properties["GeneratedCurtainFrameOpeningCount"] = "1";
            wall.Properties["GeneratedCurtainFrameColumns"] = "1";
            wall.Properties["GeneratedCurtainFrameRows"] = "1";
            wall.Properties["GeneratedCurtainFrameDepthM"] = "0.05";
            wall.Properties["GeneratedCurtainFrameSourceLengthM"] = "1";
            wall.Properties["GeneratedCurtainFrameHeightM"] = "1";
            wall.Properties["GeneratedCurtainFrameMode"] = "LineFrameOverlay.OpeningAware";
            wall.Properties["LengthM"] = "1";
            wall.Properties["HeightM"] = "1";
        }

        private static void Contains(IReadOnlyList<ModelHealthIssue> issues, string code)
        {
            foreach (var issue in issues)
                if (string.Equals(issue.Code, code, StringComparison.Ordinal)) return;
            throw new Exception("Expected issue code " + code + ".");
        }

        private static void NotContains(IReadOnlyList<ModelHealthIssue> issues, string code)
        {
            foreach (var issue in issues)
                if (string.Equals(issue.Code, code, StringComparison.Ordinal))
                    throw new Exception("Unexpected issue code " + code + ".");
        }
    }
}
