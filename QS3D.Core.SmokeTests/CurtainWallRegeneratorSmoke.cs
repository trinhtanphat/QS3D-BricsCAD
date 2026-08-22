using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallRegeneratorSmoke
    {
        public static void Run()
        {
            GlassWallProducesCurtainQuantitiesAndOpeningDeduction();
            ArchitecturalWallDoesNotProduceCurtainQuantities();
        }

        private static void GlassWallProducesCurtainQuantitiesAndOpeningDeduction()
        {
            var project = new ProjectState("p", "Curtain smoke");
            var wall = new ProjectElement("glass-1", ElementCategory.GlassWall, "f-glass", "floor", "zone");
            wall.Properties["LengthM"] = "6";
            wall.Properties["HeightM"] = "3";
            wall.Properties["ThicknessM"] = "0.012";
            wall.Properties["CurtainMaxPanelWidthM"] = "1.5";
            wall.Properties["CurtainMaxPanelHeightM"] = "1.5";
            wall.Properties["CurtainPerimeterFrameWidthM"] = "0.05";
            wall.Properties["CurtainMullionWidthM"] = "0.05";
            wall.Properties["CurtainTransomWidthM"] = "0.05";
            project.Elements.Add(wall);

            var door = new ProjectElement("door-1", ElementCategory.Door, "f-door", "floor", "zone");
            door.Properties["HostWallId"] = wall.Id;
            door.Properties["WidthM"] = "1";
            door.Properties["HeightM"] = "2";
            project.Elements.Add(door);

            new WallRegenerator().Regenerate(project, wall);

            Near(8d, Q(wall, "CurtainPanelCount"));
            Near(33d, Q(wall, "CurtainFrameLengthM"));
            Near(16.3875d, Q(wall, "CurtainClearGlassAreaM2"));
            Near(14.3875d, Q(wall, "CurtainNetGlassAreaM2"));
            Near(1.6125d, Q(wall, "CurtainFrameFaceAreaM2"));
            Near(2d, Q(wall, "OpeningAreaM2"));
            Near(0.216d, Q(wall, "GrossVolumeM3"));
            Near(0.192d, Q(wall, "NetVolumeM3"));
        }

        private static void ArchitecturalWallDoesNotProduceCurtainQuantities()
        {
            var project = new ProjectState("p2", "Wall smoke");
            var wall = new ProjectElement("wall-1", ElementCategory.ArchitecturalWall, "f-wall", "floor", "zone");
            wall.Properties["LengthM"] = "4";
            wall.Properties["HeightM"] = "3";
            wall.Properties["ThicknessM"] = "0.2";
            project.Elements.Add(wall);
            new WallRegenerator().Regenerate(project, wall);
            if (wall.Quantities.ContainsKey("CurtainPanelCount")) throw new Exception("ArchitecturalWall must not receive curtain-wall quantities.");
        }

        private static double Q(ProjectElement element, string key)
        {
            if (!element.Quantities.TryGetValue(key, out var value)) throw new Exception("Missing quantity: " + key);
            return value;
        }

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
