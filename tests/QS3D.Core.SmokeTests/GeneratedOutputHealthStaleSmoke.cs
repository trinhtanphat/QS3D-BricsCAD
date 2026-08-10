using System;
using System.Collections.Generic;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedOutputHealthStaleSmoke
    {
        public static void Run()
        {
            CurtainFramesUseSnapshotState();
            ColumnTiesUseSnapshotState();
            BeamStirrupsUseSnapshotState();
            SlabMeshUsesSnapshotState();
            WallMeshUsesSnapshotState();
            FoundationMeshUsesSnapshotState();
        }

        private static void CurtainFramesUseSnapshotState()
        {
            var project = Project(ElementCategory.GlassWall, out var element);
            element.Properties["GeneratedCurtainFrameHandles"] = "A1;A2;A3;A4";
            element.Properties["GeneratedCurtainFrameCount"] = "4";
            element.Properties["GeneratedCurtainFrameColumns"] = "1";
            element.Properties["GeneratedCurtainFrameRows"] = "1";
            element.Properties["GeneratedCurtainFrameDepthM"] = "0.05";
            element.Properties["GeneratedCurtainFrameSourceLengthM"] = "1";
            element.Properties["GeneratedCurtainFrameHeightM"] = "1";
            element.Properties["GeneratedCurtainFrameMode"] = "LineFrameOverlay";
            element.Properties["LengthM"] = "1";
            element.Properties["HeightM"] = "1";
            AssertSnapshotBehavior(element, () => new GeneratedCurtainFrameHealthService().Inspect(project), "CURTAIN_FRAME_GENERATED_STALE");
        }

        private static void ColumnTiesUseSnapshotState()
        {
            var project = Project(ElementCategory.Column, out var element);
            element.Properties["GeneratedTieRebarHandles"] = "B1";
            element.Properties["GeneratedTieRebarCount"] = "1";
            element.Properties["GeneratedTieRebarDiameterMm"] = "8";
            element.Properties["GeneratedTieRebarActualSpacingM"] = "0.15";
            AssertSnapshotBehavior(element, () => new GeneratedTieRebarHealthService().Inspect(project), "TIE_REBAR_GENERATED_STALE");
        }

        private static void BeamStirrupsUseSnapshotState()
        {
            var project = Project(ElementCategory.Beam, out var element);
            element.Properties["GeneratedBeamStirrupHandles"] = "C1";
            element.Properties["GeneratedBeamStirrupCount"] = "1";
            element.Properties["GeneratedBeamStirrupDiameterMm"] = "8";
            AssertSnapshotBehavior(element, () => new GeneratedBeamStirrupHealthService().Inspect(project), "BEAM_STIRRUP_GENERATED_STALE");
        }

        private static void SlabMeshUsesSnapshotState()
        {
            var project = Project(ElementCategory.Slab, out var element);
            element.Properties["GeneratedSlabMeshHandles"] = "D1";
            element.Properties["GeneratedSlabMeshCount"] = "1";
            element.Properties["GeneratedSlabMeshXDiameterMm"] = "10";
            element.Properties["GeneratedSlabMeshYDiameterMm"] = "10";
            element.Properties["GeneratedSlabMeshXActualSpacingM"] = "0.2";
            element.Properties["GeneratedSlabMeshYActualSpacingM"] = "0.2";
            element.Properties["GeneratedSlabMeshCoverM"] = "0.025";
            element.Properties["GeneratedSlabMeshFaces"] = "Bottom";
            element.Properties["GeneratedSlabMeshMode"] = "SlabMeshXY";
            AssertSnapshotBehavior(element, () => new GeneratedSlabMeshHealthService().Inspect(project), "SLAB_MESH_GENERATED_STALE");
        }

        private static void WallMeshUsesSnapshotState()
        {
            var project = Project(ElementCategory.StructuralWall, out var element);
            element.Properties["GeneratedWallMeshHandles"] = "E1";
            element.Properties["GeneratedWallMeshCount"] = "1";
            element.Properties["GeneratedWallMeshHorizontalDiameterMm"] = "10";
            element.Properties["GeneratedWallMeshVerticalDiameterMm"] = "10";
            element.Properties["GeneratedWallMeshHorizontalActualSpacingM"] = "0.2";
            element.Properties["GeneratedWallMeshVerticalActualSpacingM"] = "0.2";
            element.Properties["GeneratedWallMeshCoverM"] = "0.025";
            element.Properties["GeneratedWallMeshFaces"] = "Near";
            element.Properties["GeneratedWallMeshMode"] = "StructuralWallMesh";
            AssertSnapshotBehavior(element, () => new GeneratedWallMeshHealthService().Inspect(project), "WALL_MESH_GENERATED_STALE");
        }

        private static void FoundationMeshUsesSnapshotState()
        {
            var project = Project(ElementCategory.Foundation, out var element);
            element.Properties["GeneratedFoundationMeshHandles"] = "F1";
            element.Properties["GeneratedFoundationMeshCount"] = "1";
            element.Properties["GeneratedFoundationMeshXDiameterMm"] = "16";
            element.Properties["GeneratedFoundationMeshYDiameterMm"] = "12";
            element.Properties["GeneratedFoundationMeshXActualSpacingM"] = "0.2";
            element.Properties["GeneratedFoundationMeshYActualSpacingM"] = "0.15";
            element.Properties["GeneratedFoundationMeshCoverM"] = "0.05";
            element.Properties["GeneratedFoundationMeshFaces"] = "Bottom";
            element.Properties["GeneratedFoundationMeshMode"] = "FoundationMeshXY";
            AssertSnapshotBehavior(element, () => new GeneratedFoundationMeshHealthService().Inspect(project), "FOUNDATION_MESH_GENERATED_STALE");
        }

        private static ProjectState Project(ElementCategory category, out ProjectElement element)
        {
            var project = new ProjectState("HEALTH-STALE", "Generated health stale smoke");
            element = new ProjectElement("E-" + category, category, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(element);
            return project;
        }

        private static void AssertSnapshotBehavior(ProjectElement element, Func<IReadOnlyList<ModelHealthIssue>> inspect, string code)
        {
            NotContains(inspect(), code);
            element.MarkGeneratedGeometryStale("semantic edit");
            Contains(inspect(), code);
        }

        private static void Contains(IReadOnlyList<ModelHealthIssue> issues, string code)
        {
            foreach (var issue in issues) if (string.Equals(issue.Code, code, StringComparison.Ordinal)) return;
            throw new Exception("Expected issue code " + code + ".");
        }

        private static void NotContains(IReadOnlyList<ModelHealthIssue> issues, string code)
        {
            foreach (var issue in issues)
                if (string.Equals(issue.Code, code, StringComparison.Ordinal))
                    throw new Exception("Unexpected dirty-only stale issue code " + code + ".");
        }
    }
}
