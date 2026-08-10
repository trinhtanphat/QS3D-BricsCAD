using System;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedGeometryStaleSmoke
    {
        public static void Run()
        {
            GeneratedOutputsBecomeStaleAfterSemanticEdit();
            ReplacedHandleAutoResolvesOnlyItsOwnStaleKind();
            ExplicitClearPreservesOtherStaleKinds();
            StaleHealthReportsAllGeneratedKinds();
            ElementsWithoutGeneratedOutputsRemainFresh();
        }

        private static void GeneratedOutputsBecomeStaleAfterSemanticEdit()
        {
            var element = Element();
            SeedAllGeneratedOutputs(element);
            element.MarkGeneratedGeometryStale("Thickness changed");

            True(element.IsGeneratedSolidStale());
            True(element.IsGeneratedRebarStale());
            True(element.IsGeneratedShapeRebarStale());
            True(element.IsGeneratedTieRebarStale());
            True(element.IsGeneratedBeamStirrupStale());
            True(element.IsGeneratedSlabMeshStale());
            True(element.IsGeneratedWallMeshStale());
            True(element.IsGeneratedFoundationMeshStale());
            True(element.IsGeneratedCurtainFrameStale());
            True(element.IsGeneratedGeometryStale());
            Equal("Thickness changed", element.Properties[ProjectElement.GeneratedGeometryStaleReasonKey]);
        }

        private static void ReplacedHandleAutoResolvesOnlyItsOwnStaleKind()
        {
            var element = Element();
            SeedAllGeneratedOutputs(element);
            element.MarkGeneratedGeometryStale("Family changed");

            element.Properties["GeneratedRebarHandles"] = "BD";
            False(element.IsGeneratedRebarStale());
            True(element.IsGeneratedSolidStale());
            True(element.IsGeneratedShapeRebarStale());
            True(element.IsGeneratedTieRebarStale());
            True(element.IsGeneratedBeamStirrupStale());
            True(element.IsGeneratedSlabMeshStale());
            True(element.IsGeneratedWallMeshStale());
            True(element.IsGeneratedFoundationMeshStale());
            True(element.IsGeneratedCurtainFrameStale());
            True(element.IsGeneratedGeometryStale());

            element.Properties["GeneratedSolidHandle"] = "AD";
            element.Properties["GeneratedShapeRebarHandles"] = "CD";
            element.Properties["GeneratedTieRebarHandles"] = "DE";
            element.Properties["GeneratedBeamStirrupHandles"] = "EF";
            element.Properties["GeneratedSlabMeshHandles"] = "FG";
            element.Properties["GeneratedWallMeshHandles"] = "GH";
            element.Properties["GeneratedFoundationMeshHandles"] = "HI";
            True(element.IsGeneratedCurtainFrameStale());
            element.Properties["GeneratedCurtainFrameHandles"] = "IJ";
            False(element.IsGeneratedGeometryStale());
            False(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStateKey));
        }

        private static void ExplicitClearPreservesOtherStaleKinds()
        {
            var element = Element();
            SeedAllGeneratedOutputs(element);
            element.MarkGeneratedGeometryStale("Instance changed");

            element.ClearGeneratedRebarStale();
            False(element.IsGeneratedRebarStale());
            True(element.IsGeneratedSolidStale());
            True(element.IsGeneratedShapeRebarStale());
            True(element.IsGeneratedTieRebarStale());
            True(element.IsGeneratedBeamStirrupStale());
            True(element.IsGeneratedSlabMeshStale());
            True(element.IsGeneratedWallMeshStale());
            True(element.IsGeneratedFoundationMeshStale());
            True(element.IsGeneratedCurtainFrameStale());

            element.ClearGeneratedSolidStale();
            element.ClearGeneratedShapeRebarStale();
            element.ClearGeneratedTieRebarStale();
            element.ClearGeneratedBeamStirrupStale();
            element.ClearGeneratedSlabMeshStale();
            element.ClearGeneratedWallMeshStale();
            element.ClearGeneratedFoundationMeshStale();
            True(element.IsGeneratedCurtainFrameStale());
            element.ClearGeneratedCurtainFrameStale();
            False(element.IsGeneratedGeometryStale());
        }

        private static void StaleHealthReportsAllGeneratedKinds()
        {
            var project = new ProjectState("STALE", "Generated stale lifecycle");
            var element = Element();
            project.Elements.Add(element);
            SeedAllGeneratedOutputs(element);
            element.MarkGeneratedGeometryStale("source moved");
            var issues = new GeneratedGeometryStaleHealthService().Inspect(project);
            Equal(9, issues.Count);
            Contains(issues, "GENERATED_SOLID_STALE");
            Contains(issues, "REBAR_GENERATED_STALE");
            Contains(issues, "SHAPE_REBAR_GENERATED_STALE");
            Contains(issues, "TIE_REBAR_GENERATED_STALE");
            Contains(issues, "BEAM_STIRRUP_GENERATED_STALE");
            Contains(issues, "SLAB_MESH_GENERATED_STALE");
            Contains(issues, "WALL_MESH_GENERATED_STALE");
            Contains(issues, "FOUNDATION_MESH_GENERATED_STALE");
            Contains(issues, "CURTAIN_FRAME_GENERATED_STALE");
        }

        private static void ElementsWithoutGeneratedOutputsRemainFresh()
        {
            var element = Element();
            element.MarkGeneratedGeometryStale("No generated geometry");
            False(element.IsGeneratedGeometryStale());
            False(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStateKey));
        }

        private static void SeedAllGeneratedOutputs(ProjectElement element)
        {
            element.Properties["GeneratedSolidHandle"] = "AA";
            element.Properties["GeneratedRebarHandles"] = "BB;BC";
            element.Properties["GeneratedShapeRebarHandles"] = "CC";
            element.Properties["GeneratedTieRebarHandles"] = "DD";
            element.Properties["GeneratedBeamStirrupHandles"] = "EE";
            element.Properties["GeneratedSlabMeshHandles"] = "FF";
            element.Properties["GeneratedWallMeshHandles"] = "GG";
            element.Properties["GeneratedFoundationMeshHandles"] = "HH";
            element.Properties["GeneratedCurtainFrameHandles"] = "II";
        }

        private static void Contains(System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues, string code)
        {
            foreach (var issue in issues) if (string.Equals(issue.Code, code, StringComparison.Ordinal)) return;
            throw new Exception("Expected issue code " + code + ".");
        }

        private static ProjectElement Element() => new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void False(bool value) { if (value) throw new Exception("Expected false."); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
    }
}
