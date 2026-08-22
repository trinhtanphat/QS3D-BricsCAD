using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishFamilyCategorySmoke
    {
        public static void Run()
        {
            MismatchedFamilyCategoryFailsClosed();
            MatchingFamilyCategoryPreservesInheritance();
            MissingFamilyFailsClosed();
        }

        private static void MismatchedFamilyCategoryFailsClosed()
        {
            var project = NewProject("finish-mismatch");
            var wallFinishFamily = new ProjectFamily("WF", "Wall finish family", ElementCategory.WallFinish);
            wallFinishFamily.Properties["Material"] = "Gạch";
            project.Families.Add(wallFinishFamily);

            var floorFinish = new ProjectElement("FF1", ElementCategory.FloorFinish, wallFinishFamily.Id, "F1", "Z1");
            floorFinish.Quantities["BottomAreaM2"] = 12d;
            project.Elements.Add(floorFinish);

            Throws<InvalidOperationException>(() => RoomFinishScheduleBuilder.Build(project));
        }

        private static void MatchingFamilyCategoryPreservesInheritance()
        {
            var project = NewProject("finish-matching");
            var floorFinishFamily = new ProjectFamily("FF", "Floor finish family", ElementCategory.FloorFinish);
            floorFinishFamily.Properties["Material"] = "Gạch";
            project.Families.Add(floorFinishFamily);

            var floorFinish = new ProjectElement("FF1", ElementCategory.FloorFinish, floorFinishFamily.Id, "F1", "Z1");
            floorFinish.Quantities["BottomAreaM2"] = 12d;
            floorFinish.SourceHandles.Add("A1");
            project.Elements.Add(floorFinish);

            var row = RoomFinishScheduleBuilder.Build(project).Single();
            Equal("FloorFinish", row.Category);
            Equal("Floor finish family", row.FamilyName);
            Equal("Gạch", row.Material);
            Equal("m²", row.UnitHint);
            Equal(1, row.Count);
            Near(12d, row.AreaM2);
            Near(12d, row.PrimaryQuantity);
            Equal("FF1", row.ElementIds.Single());
            Equal("A1", row.SourceHandles.Single());
        }

        private static void MissingFamilyFailsClosed()
        {
            var project = NewProject("finish-missing-family");
            var floorFinish = new ProjectElement("FF1", ElementCategory.FloorFinish, "MISSING", "F1", "Z1");
            floorFinish.Quantities["BottomAreaM2"] = 3d;
            project.Elements.Add(floorFinish);

            Throws<InvalidOperationException>(() => RoomFinishScheduleBuilder.Build(project));
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
