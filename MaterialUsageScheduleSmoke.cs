using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageScheduleSmoke
    {
        public static void Run()
        {
            FamilyInheritanceAndCurtainComponents();
            InstanceOverrideUsesCatalogUnit();
            PrimaryQuantitiesIgnoreInvalidFallbacks();
            InvalidUsedFallbackIsRejected();
            RejectsInvalidQuantities();
        }

        private static void FamilyInheritanceAndCurtainComponents()
        {
            var project = new ProjectState("p", "Materials");
            project.Floors.Add(new FloorDefinition("f1", "Tầng 1", 0d));
            var glassFamily = new ProjectFamily("glass", "Vách kính 12mm", ElementCategory.GlassWall);
            glassFamily.Properties["Material"] = "Kính";
            glassFamily.Properties["CurtainFrameMaterial"] = "Nhôm";
            project.Families.Add(glassFamily);
            var glass = new ProjectElement("g1", ElementCategory.GlassWall, glassFamily.Id, "f1", "z");
            glass.Quantities["LengthM"] = 6d;
            glass.Quantities["CurtainNetGlassAreaM2"] = 14.4d;
            glass.Quantities["CurtainFrameLengthM"] = 33d;
            glass.Quantities["CurtainFrameFaceAreaM2"] = 1.6d;
            project.Elements.Add(glass);

            var rows = MaterialUsageScheduleBuilder.Build(project);
            if (rows.Count != 2) throw new Exception("Glass wall must produce glass and curtain-frame material rows.");
            var main = rows.Single(x => x.Component == "Material");
            if (main.MaterialName != "Kính" || main.UnitHint != "m²" || main.Category != "GlassWall") throw new Exception("Glass material inheritance failed.");
            Near(14.4d, main.AreaM2);
            Near(14.4d, main.PrimaryQuantity);
            var frame = rows.Single(x => x.Component == "CurtainFrame");
            if (frame.MaterialName != "Nhôm" || frame.UnitHint != "m") throw new Exception("Curtain frame material inheritance failed.");
            Near(33d, frame.LengthM);
            Near(1.6d, frame.AreaM2);
            Near(33d, frame.PrimaryQuantity);
        }

        private static void InstanceOverrideUsesCatalogUnit()
        {
            var project = new ProjectState("p2", "Override");
            project.Floors.Add(new FloorDefinition("f1", "Tầng 1", 0d));
            var family = new ProjectFamily("wall", "Tường 200", ElementCategory.ArchitecturalWall);
            family.Properties["Material"] = "Gạch";
            project.Families.Add(family);
            ProjectMaterialCatalog.UpsertCustom(project, "aac", "Gạch AAC", "m²", "Tường nhẹ");
            var first = new ProjectElement("w1", ElementCategory.ArchitecturalWall, family.Id, "f1", "z");
            first.Properties["Material"] = "Gạch AAC";
            first.Quantities["LengthM"] = 5d;
            first.Quantities["NetWallAreaM2"] = 14d;
            first.Quantities["NetVolumeM3"] = 2.8d;
            project.Elements.Add(first);
            var second = new ProjectElement("w2", ElementCategory.ArchitecturalWall, family.Id, "f1", "z");
            second.Properties["Material"] = "Gạch AAC";
            second.Quantities["LengthM"] = 3d;
            second.Quantities["NetWallAreaM2"] = 8d;
            second.Quantities["NetVolumeM3"] = 1.6d;
            project.Elements.Add(second);

            var row = MaterialUsageScheduleBuilder.Build(project).Single();
            if (row.MaterialName != "Gạch AAC" || row.UnitHint != "m²" || row.ElementCount != 2) throw new Exception("Material override grouping failed.");
            Near(8d, row.LengthM);
            Near(22d, row.AreaM2);
            Near(4.4d, row.VolumeM3);
            Near(22d, row.PrimaryQuantity);
            if (row.ElementIds.Count != 2) throw new Exception("Material schedule provenance failed.");
        }

        private static void PrimaryQuantitiesIgnoreInvalidFallbacks()
        {
            var project = new ProjectState("p-primary", "Lazy fallback");
            var family = new ProjectFamily("wall", "Tường", ElementCategory.ArchitecturalWall);
            family.Properties["Material"] = "Gạch";
            project.Families.Add(family);
            var wall = new ProjectElement("w-primary", ElementCategory.ArchitecturalWall, family.Id, "floor", "z");
            wall.Quantities["NetVolumeM3"] = 2.4d;
            wall.Quantities["VolumeM3"] = -99d;
            wall.Quantities["NetWallAreaM2"] = 12d;
            wall.Quantities["SideAreaM2"] = double.NaN;
            project.Elements.Add(wall);

            var row = MaterialUsageScheduleBuilder.Build(project).Single();
            Near(2.4d, row.VolumeM3);
            Near(12d, row.AreaM2);
        }

        private static void InvalidUsedFallbackIsRejected()
        {
            var project = new ProjectState("p-fallback", "Used fallback");
            var family = new ProjectFamily("wall", "Tường", ElementCategory.ArchitecturalWall);
            family.Properties["Material"] = "Gạch";
            project.Families.Add(family);
            var wall = new ProjectElement("w-fallback", ElementCategory.ArchitecturalWall, family.Id, "floor", "z");
            wall.Quantities["SideAreaM2"] = -1d;
            project.Elements.Add(wall);
            Throws<InvalidOperationException>(() => MaterialUsageScheduleBuilder.Build(project));
        }

        private static void RejectsInvalidQuantities()
        {
            var project = new ProjectState("p3", "Bad materials");
            var family = new ProjectFamily("wall", "Tường", ElementCategory.ArchitecturalWall);
            family.Properties["Material"] = "Gạch";
            project.Families.Add(family);
            var wall = new ProjectElement("w", ElementCategory.ArchitecturalWall, family.Id, "floor", "z");
            wall.Quantities["NetWallAreaM2"] = -1d;
            project.Elements.Add(wall);
            Throws<InvalidOperationException>(() => MaterialUsageScheduleBuilder.Build(project));
        }

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
