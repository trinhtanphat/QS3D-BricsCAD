using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementGeometryPolicyCurtainFrameMaterialOutputScopeSmoke
    {
        public static void Run()
        {
            GenericMaterialRemainsOutputAffecting();
            CurtainFrameMaterialIsGlassWallOnly();
            CurtainGeometryScopeRemainsGlassWallOnly();
        }

        private static void GenericMaterialRemainsOutputAffecting()
        {
            True(ElementGeometryPolicy.AffectsGeneratedOutput(ElementCategory.GlassWall, "Material"));
            True(ElementGeometryPolicy.AffectsGeneratedOutput(ElementCategory.Beam, "Material"));
            True(ElementGeometryPolicy.AffectsGeneratedOutput(ElementCategory.Slab, "Material"));
        }

        private static void CurtainFrameMaterialIsGlassWallOnly()
        {
            True(ElementGeometryPolicy.AffectsGeneratedOutput(ElementCategory.GlassWall, "CurtainFrameMaterial"));
            False(ElementGeometryPolicy.AffectsGeneratedOutput(ElementCategory.Beam, "CurtainFrameMaterial"));
            False(ElementGeometryPolicy.AffectsGeneratedOutput(ElementCategory.Slab, "CurtainFrameMaterial"));
            False(ElementGeometryPolicy.AffectsGeneratedOutput(ElementCategory.Column, "CurtainFrameMaterial"));
            False(ElementGeometryPolicy.AffectsGeneratedOutput(ElementCategory.ArchitecturalWall, "CurtainFrameMaterial"));
        }

        private static void CurtainGeometryScopeRemainsGlassWallOnly()
        {
            True(ElementGeometryPolicy.AffectsGeneratedGeometry(ElementCategory.GlassWall, "CurtainMaxPanelWidthM"));
            True(ElementGeometryPolicy.AffectsGeneratedOutput(ElementCategory.GlassWall, "CurtainMaxPanelWidthM"));
            False(ElementGeometryPolicy.AffectsGeneratedGeometry(ElementCategory.Beam, "CurtainMaxPanelWidthM"));
            False(ElementGeometryPolicy.AffectsGeneratedOutput(ElementCategory.Beam, "CurtainMaxPanelWidthM"));
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected false.");
        }
    }
}
