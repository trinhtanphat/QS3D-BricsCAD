using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningFamilyCategorySmoke
    {
        public static void Run()
        {
            MismatchedFamilyCategoryFailsClosed();
            MatchingDoorFamilyPreservesFallbackDimensions();
        }

        private static void MismatchedFamilyCategoryFailsClosed()
        {
            var project = NewProject("door-mismatch");
            var wallFamily = Family("F-WALL", ElementCategory.ArchitecturalWall);
            project.Families.Add(wallFamily);
            project.Elements.Add(new ProjectElement("D1", ElementCategory.Door, wallFamily.Id, "F1", "Z1"));

            Throws<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
        }

        private static void MatchingDoorFamilyPreservesFallbackDimensions()
        {
            var project = NewProject("door-matching");
            var doorFamily = Family("F-DOOR", ElementCategory.Door);
            project.Families.Add(doorFamily);
            var door = new ProjectElement("D1", ElementCategory.Door, doorFamily.Id, "F1", "Z1");
            door.SourceHandles.Add("D0A1");
            project.Elements.Add(door);

            var row = DoorOpeningScheduleBuilder.Build(project).Single();
            Equal("Door family", row.FamilyName);
            Equal("Timber", row.Material);
            Equal("Door", row.Category);
            Equal(1, row.Count);
            Near(0.9d, row.WidthM);
            Near(2.2d, row.HeightM);
            Near(0.1d, row.ThicknessM);
            Near(1.98d, row.OpeningAreaM2);
            Equal("D1", row.ElementIds.Single());
            Equal("D0A1", row.SourceHandles.Single());
        }

        private static ProjectFamily Family(string id, ElementCategory category)
        {
            var family = new ProjectFamily(id, category == ElementCategory.Door ? "Door family" : "Wall family", category);
            family.Properties["WidthM"] = "0.9";
            family.Properties["HeightM"] = "2.2";
            family.Properties["ThicknessM"] = "0.1";
            family.Properties["Material"] = "Timber";
            return family;
        }

        private static ProjectState NewProject(string id)
        {
            var project = new ProjectState(id, id);
            project.Floors.Add(new FloorDefinition("F1", "Floor 1", 0d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            return project;
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
