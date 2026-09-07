using System;
using System.Collections.Generic;
using System.Reflection;
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
            SeedSnapshotProperty(element, "GeneratedCurtainFrameHandles", "A1;A2;A3;A4");
            SeedSnapshotProperty(element, "GeneratedCurtainFrameCount", "4");
            SeedSnapshotProperty(element, "GeneratedCurtainFrameColumns", "1");
            SeedSnapshotProperty(element, "GeneratedCurtainFrameRows", "1");
            SeedSnapshotProperty(element, "GeneratedCurtainFrameDepthM", "0.05");
            SeedSnapshotProperty(element, "GeneratedCurtainFrameSourceLengthM", "1");
            SeedSnapshotProperty(element, "GeneratedCurtainFrameHeightM", "1");
            SeedSnapshotProperty(element, "GeneratedCurtainFrameMode", "LineFrameOverlay");
            SeedSnapshotProperty(element, "LengthM", "1");
            SeedSnapshotProperty(element, "HeightM", "1");
            AssertSnapshotBehavior(element, () => new GeneratedCurtainFrameHealthService().Inspect(project), "CURTAIN_FRAME_GENERATED_STALE");
        }

        private static void ColumnTiesUseSnapshotState()
        {
            var project = Project(ElementCategory.Column, out var element);
            SeedSnapshotProperty(element, "GeneratedTieRebarHandles", "B1");
            SeedSnapshotProperty(element, "GeneratedTieRebarCount", "1");
            SeedSnapshotProperty(element, "GeneratedTieRebarDiameterMm", "8");
            SeedSnapshotProperty(element, "GeneratedTieRebarActualSpacingM", "0.15");
            AssertSnapshotBehavior(element, () => new GeneratedTieRebarHealthService().Inspect(project), "TIE_REBAR_GENERATED_STALE");
        }

        private static void BeamStirrupsUseSnapshotState()
        {
            var project = Project(ElementCategory.Beam, out var element);
            SeedSnapshotProperty(element, "GeneratedBeamStirrupHandles", "C1");
            SeedSnapshotProperty(element, "GeneratedBeamStirrupCount", "1");
            SeedSnapshotProperty(element, "GeneratedBeamStirrupDiameterMm", "8");
            AssertSnapshotBehavior(element, () => new GeneratedBeamStirrupHealthService().Inspect(project), "BEAM_STIRRUP_GENERATED_STALE");
        }

        private static void SlabMeshUsesSnapshotState()
        {
            var project = Project(ElementCategory.Slab, out var element);
            SeedSnapshotProperty(element, "GeneratedSlabMeshHandles", "D1");
            SeedSnapshotProperty(element, "GeneratedSlabMeshCount", "1");
            SeedSnapshotProperty(element, "GeneratedSlabMeshXDiameterMm", "10");
            SeedSnapshotProperty(element, "GeneratedSlabMeshYDiameterMm", "10");
            SeedSnapshotProperty(element, "GeneratedSlabMeshXActualSpacingM", "0.2");
            SeedSnapshotProperty(element, "GeneratedSlabMeshYActualSpacingM", "0.2");
            SeedSnapshotProperty(element, "GeneratedSlabMeshCoverM", "0.025");
            SeedSnapshotProperty(element, "GeneratedSlabMeshFaces", "Bottom");
            SeedSnapshotProperty(element, "GeneratedSlabMeshMode", "SlabMeshXY");
            AssertSnapshotBehavior(element, () => new GeneratedSlabMeshHealthService().Inspect(project), "SLAB_MESH_GENERATED_STALE");
        }

        private static void WallMeshUsesSnapshotState()
        {
            var project = Project(ElementCategory.StructuralWall, out var element);
            SeedSnapshotProperty(element, "GeneratedWallMeshHandles", "E1");
            SeedSnapshotProperty(element, "GeneratedWallMeshCount", "1");
            SeedSnapshotProperty(element, "GeneratedWallMeshHorizontalDiameterMm", "10");
            SeedSnapshotProperty(element, "GeneratedWallMeshVerticalDiameterMm", "10");
            SeedSnapshotProperty(element, "GeneratedWallMeshHorizontalActualSpacingM", "0.2");
            SeedSnapshotProperty(element, "GeneratedWallMeshVerticalActualSpacingM", "0.2");
            SeedSnapshotProperty(element, "GeneratedWallMeshCoverM", "0.025");
            SeedSnapshotProperty(element, "GeneratedWallMeshFaces", "Near");
            SeedSnapshotProperty(element, "GeneratedWallMeshMode", "StructuralWallMesh");
            AssertSnapshotBehavior(element, () => new GeneratedWallMeshHealthService().Inspect(project), "WALL_MESH_GENERATED_STALE");
        }

        private static void FoundationMeshUsesSnapshotState()
        {
            var project = Project(ElementCategory.Foundation, out var element);
            SeedSnapshotProperty(element, "GeneratedFoundationMeshHandles", "F1");
            SeedSnapshotProperty(element, "GeneratedFoundationMeshCount", "1");
            SeedSnapshotProperty(element, "GeneratedFoundationMeshXDiameterMm", "16");
            SeedSnapshotProperty(element, "GeneratedFoundationMeshYDiameterMm", "12");
            SeedSnapshotProperty(element, "GeneratedFoundationMeshXActualSpacingM", "0.2");
            SeedSnapshotProperty(element, "GeneratedFoundationMeshYActualSpacingM", "0.15");
            SeedSnapshotProperty(element, "GeneratedFoundationMeshCoverM", "0.05");
            SeedSnapshotProperty(element, "GeneratedFoundationMeshFaces", "Bottom");
            SeedSnapshotProperty(element, "GeneratedFoundationMeshMode", "FoundationMeshXY");
            AssertSnapshotBehavior(element, () => new GeneratedFoundationMeshHealthService().Inspect(project), "FOUNDATION_MESH_GENERATED_STALE");
        }

        private static ProjectState Project(ElementCategory category, out ProjectElement element)
        {
            var project = new ProjectState("HEALTH-STALE", "Generated health stale smoke");
            element = new ProjectElement("E-" + category, category, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(element);
            return project;
        }

        private static void SeedSnapshotProperty(ProjectElement element, string key, string value)
        {
            var field = typeof(ProjectElement).GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Generated-output snapshot fixture could not locate the property backing dictionary.");
            var backing = field.GetValue(element) as Dictionary<string, string>
                ?? throw new InvalidOperationException("Generated-output snapshot fixture property backing dictionary has an unexpected type.");
            backing[key] = value;
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
