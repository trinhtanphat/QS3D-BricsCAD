using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Rebar;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class HardeningRegressionSmoke
    {
        public static void Run()
        {
            SemanticQuantityHardening();
            RebarSpacingRequiresDistribution();
            ModelHealthReferenceIntegrity();
            FamilyChangeNotification();
        }

        private static void SemanticQuantityHardening()
        {
            var project = NewProject();
            var wall = new ProjectElement("W-HARD", ElementCategory.ArchitecturalWall, "wall", "f", "z");
            wall.Properties["LengthM"] = "NaN";
            wall.Properties["HeightM"] = "3";
            wall.Properties["ThicknessM"] = "0.2";
            new WallRegenerator().Regenerate(project, wall);
            Near(0d, wall.Quantities["LengthM"]);
            Near(0d, wall.Quantities["GrossWallAreaM2"]);

            wall.Properties["LengthM"] = "2";
            wall.Properties["HeightM"] = "2";
            wall.Properties["OpeningAreaM2"] = "99";
            new WallRegenerator().Regenerate(project, wall);
            Near(4d, wall.Quantities["GrossWallAreaM2"]);
            Near(4d, wall.Quantities["OpeningAreaM2"]);
            Near(0d, wall.Quantities["NetWallAreaM2"]);

            var opening = new ProjectElement("O-HARD", ElementCategory.WallOpening, "opening", "f", "z");
            opening.Properties["WidthM"] = "-1";
            opening.Properties["HeightM"] = "-2";
            new OpeningRegenerator().Regenerate(project, opening);
            Near(0d, opening.Quantities["OpeningAreaM2"]);

            var skirting = new ProjectElement("S-HARD", ElementCategory.Skirting, "skirting", "f", "z");
            skirting.Properties["PerimeterM"] = "10";
            skirting.Properties["DoorWidthM"] = "-2";
            new RoomRegenerator().Regenerate(project, skirting);
            Near(10d, skirting.Quantities["SkirtingLengthM"]);
        }

        private static void RebarSpacingRequiresDistribution()
        {
            Throws<InvalidOperationException>(() => RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput { ElementId = "S1", Notation = "D8@150", CuttingLengthM = 4d, DistributionLengthM = 0d }
            }));
        }

        private static void ModelHealthReferenceIntegrity()
        {
            var project = NewProject();
            project.ActiveFloorId = "missing-floor";

            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, "wall", "f", "z");
            project.Elements.Add(wall);

            var door = new ProjectElement("D1", ElementCategory.Door, "door", "f", "z");
            door.Properties["HostWallId"] = "missing-wall";
            door.DependsOn.Add("missing-wall");
            project.Elements.Add(door);

            var mismatched = new ProjectElement("BAD-FAMILY", ElementCategory.Slab, "wall", "f", "z");
            project.Elements.Add(mismatched);

            var beam = new ProjectElement("B1", ElementCategory.Beam, "beam", "f", "z");
            beam.Properties["LengthM"] = "5";
            beam.Properties["RebarNotation"] = "D8@150";
            project.Elements.Add(beam);

            var issues = new ModelHealthService().Inspect(project);
            True(issues.Any(x => x.Code == "INVALID_ACTIVE_FLOOR"));
            True(issues.Any(x => x.Code == "INVALID_HOST" && x.ElementId == "D1"));
            True(issues.Any(x => x.Code == "MISSING_DEPENDENCY" && x.ElementId == "D1"));
            True(issues.Any(x => x.Code == "FAMILY_CATEGORY_MISMATCH" && x.ElementId == "BAD-FAMILY"));
            True(issues.Any(x => x.Code == "REBAR_DISTRIBUTION_MISSING" && x.ElementId == "B1"));
        }

        private static void FamilyChangeNotification()
        {
            var family = new ProjectFamily("fam", "Old", ElementCategory.Room);
            var changed = string.Empty;
            family.PropertyChanged += (_, e) => changed = e.PropertyName ?? string.Empty;
            family.Name = "  New Name  ";
            Equal("New Name", family.Name);
            Equal("Name", changed);
            Throws<ArgumentException>(() => family.Name = "   ");
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "Hardening");
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.ActiveZoneId = "z";
            project.ActiveFloorId = "f";

            var wall = new ProjectFamily("wall", "Wall", ElementCategory.ArchitecturalWall); wall.Properties["Material"] = "Brick"; project.Families.Add(wall);
            var opening = new ProjectFamily("opening", "Opening", ElementCategory.WallOpening); project.Families.Add(opening);
            var door = new ProjectFamily("door", "Door", ElementCategory.Door); door.Properties["Material"] = "Wood"; project.Families.Add(door);
            var skirting = new ProjectFamily("skirting", "Skirting", ElementCategory.Skirting); skirting.Properties["Material"] = "Tile"; project.Families.Add(skirting);
            var beam = new ProjectFamily("beam", "Beam", ElementCategory.Beam); beam.Properties["Material"] = "Concrete"; project.Families.Add(beam);
            return project;
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
