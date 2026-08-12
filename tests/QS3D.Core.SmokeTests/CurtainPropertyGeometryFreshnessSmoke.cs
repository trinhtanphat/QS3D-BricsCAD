using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainPropertyGeometryFreshnessSmoke
    {
        private static readonly string[] CurtainGeometryKeys =
        {
            "CurtainMaxPanelWidthM",
            "CurtainMaxPanelHeightM",
            "CurtainPerimeterFrameWidthM",
            "CurtainMullionWidthM",
            "CurtainTransomWidthM"
        };

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            foreach (var key in CurtainGeometryKeys)
                GlassWallLayoutEditMarksGeometryAndCurtainOutputStale(key);

            CurtainKeyDoesNotDirtyUnrelatedGeneratedCategory();
            CurtainFrameMaterialRemainsOutputOnly();
        }

        private static void GlassWallLayoutEditMarksGeometryAndCurtainOutputStale(string key)
        {
            var wall = new ProjectElement("GW-" + key, ElementCategory.GlassWall);
            wall.Properties["GeneratedCurtainFrameHandles"] = "CF1";
            wall.Properties["GeneratedCurtainPanelHandles"] = "CP1";
            wall.MarkClean(ElementDirtyFlags.All);

            wall.SetProperty(key, "1.25");

            if ((wall.Dirty & ElementDirtyFlags.Geometry) == 0)
                throw new InvalidOperationException(key + " must dirty GlassWall geometry.");
            if (!wall.IsGeneratedCurtainFrameStale())
                throw new InvalidOperationException(key + " must stale generated curtain frame output.");
            if (!wall.IsGeneratedCurtainPanelStale())
                throw new InvalidOperationException(key + " must stale generated curtain panel output.");
        }

        private static void CurtainKeyDoesNotDirtyUnrelatedGeneratedCategory()
        {
            var beam = new ProjectElement("B1", ElementCategory.Beam);
            beam.Properties["GeneratedSolidHandle"] = "S1";
            beam.MarkClean(ElementDirtyFlags.All);

            beam.SetProperty("CurtainMaxPanelWidthM", "1.25");

            if ((beam.Dirty & ElementDirtyFlags.Geometry) != 0)
                throw new InvalidOperationException("Curtain layout keys must not dirty unrelated generated categories.");
            if (beam.IsGeneratedSolidStale())
                throw new InvalidOperationException("Curtain layout keys must not stale unrelated generated solid output.");
        }

        private static void CurtainFrameMaterialRemainsOutputOnly()
        {
            var wall = new ProjectElement("GW-MAT", ElementCategory.GlassWall);
            wall.Properties["GeneratedCurtainFrameHandles"] = "CF1";
            wall.MarkClean(ElementDirtyFlags.All);

            wall.SetProperty("CurtainFrameMaterial", "Nhôm");

            if ((wall.Dirty & ElementDirtyFlags.Geometry) != 0)
                throw new InvalidOperationException("CurtainFrameMaterial must remain generated-output-only.");
            if (!wall.IsGeneratedCurtainFrameStale())
                throw new InvalidOperationException("CurtainFrameMaterial must stale existing curtain frame output.");
        }
    }
}
