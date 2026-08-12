using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningScheduleGroupKeyCollisionSmoke
    {
        internal static void Run()
        {
            const string separator = "\u001f";
            var project = new ProjectState("P-DOOR-GROUP", "Door grouping");

            var first = Door("E1", "X" + separator + "1", 2d, 3d, 4d, 5d, "M");
            var identical = Door("E2", "X" + separator + "1", 2d, 3d, 4d, 5d, "M");
            var collidingUnderOldKey = Door("E3", "X", 1d, 2d, 3d, 4d, "5" + separator + "M");
            project.Elements.Add(first);
            project.Elements.Add(identical);
            project.Elements.Add(collidingUnderOldKey);

            var rows = DoorOpeningScheduleBuilder.Build(project);
            Equal(2, rows.Count, "distinct grouping tuples remain distinct");

            var wide = rows.Single(x => x.WidthM.Equals(2d));
            Equal(2, wide.Count, "identical rows still group");
            Equal(12d, wide.OpeningAreaM2, "identical row areas accumulate");
            Equal("X" + separator + "1", wide.FamilyName, "wide family identity preserved");
            Equal("M", wide.Material, "wide material preserved");

            var narrow = rows.Single(x => x.WidthM.Equals(1d));
            Equal(1, narrow.Count, "old delimiter collision no longer merges");
            Equal(2d, narrow.OpeningAreaM2, "colliding row area remains independent");
            Equal("X", narrow.FamilyName, "narrow family identity preserved");
            Equal("5" + separator + "M", narrow.Material, "separator-bearing material preserved");
        }

        private static ProjectElement Door(
            string id,
            string familyId,
            double width,
            double height,
            double sill,
            double thickness,
            string material)
        {
            var element = new ProjectElement(id, ElementCategory.Door, familyId, string.Empty, string.Empty);
            element.SetProperty("WidthM", width.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            element.SetProperty("HeightM", height.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            element.SetProperty("SillHeightM", sill.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            element.SetProperty("ThicknessM", thickness.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            element.SetProperty("Material", material);
            return element;
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
