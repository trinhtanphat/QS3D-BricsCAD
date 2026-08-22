using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallScheduleGroupKeyCollisionSmoke
    {
        internal static void Run()
        {
            const string separator = "|";
            var project = new ProjectState("P-CURTAIN-GROUP", "Curtain grouping");
            project.Floors.Add(new FloorDefinition("A" + separator + "B", "A" + separator + "B", 0d));
            project.Floors.Add(new FloorDefinition("A", "A", 3d));
            project.Families.Add(new ProjectFamily("C", "C", ElementCategory.GlassWall));
            project.Families.Add(new ProjectFamily("B" + separator + "C", "B" + separator + "C", ElementCategory.GlassWall));

            Equal(
                LegacyDelimitedKey("A" + separator + "B", "C", separator),
                LegacyDelimitedKey("A", "B" + separator + "C", separator),
                "fixture tuples collide under delimiter-only grouping");

            var first = Wall("E1", "A" + separator + "B", "C", 2d);
            var identical = Wall("E2", "A" + separator + "B", "C", 3d);
            var collidingUnderOldKey = Wall("E3", "A", "B" + separator + "C", 7d);
            project.Elements.Add(first);
            project.Elements.Add(identical);
            project.Elements.Add(collidingUnderOldKey);

            var rows = CurtainWallScheduleBuilder.Build(project);
            Equal(2, rows.Count, "distinct grouping tuples remain distinct");

            var firstGroup = rows.Single(x => x.Floor == "A" + separator + "B");
            Equal(2, firstGroup.WallCount, "identical floor/family tuple still groups");
            Equal(5d, firstGroup.TotalWallLengthM, "identical tuple quantities accumulate");
            Equal("C", firstGroup.FamilyName, "first group family preserved");

            var secondGroup = rows.Single(x => x.Floor == "A");
            Equal(1, secondGroup.WallCount, "old delimiter collision no longer merges");
            Equal(7d, secondGroup.TotalWallLengthM, "second group quantity remains independent");
            Equal("B" + separator + "C", secondGroup.FamilyName, "separator-bearing family preserved");
        }

        private static ProjectElement Wall(string id, string floorId, string familyId, double lengthM)
        {
            var element = new ProjectElement(id, ElementCategory.GlassWall, familyId, floorId, string.Empty);
            element.SetQuantity("LengthM", lengthM);
            return element;
        }

        private static string LegacyDelimitedKey(string floorId, string familyId, string separator) =>
            floorId + separator + familyId;

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
