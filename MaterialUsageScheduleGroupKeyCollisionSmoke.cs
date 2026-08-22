using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageScheduleGroupKeyCollisionSmoke
    {
        internal static void Run()
        {
            const string separator = "|";
            var project = new ProjectState("P-MATERIAL-GROUP", "Material grouping");
            project.Floors.Add(new FloorDefinition("A" + separator + "B", "Floor AB", 0d));
            project.Floors.Add(new FloorDefinition("A", "Floor A", 3d));

            Equal(
                LegacyDelimitedKey(separator, "A" + separator + "B", "C", "Material", "Beam", string.Empty),
                LegacyDelimitedKey(separator, "A", "B" + separator + "C", "Material", "Beam", string.Empty),
                "fixture tuples collide under delimiter-only grouping");

            var first = Element("E1", "A" + separator + "B", "C", 2d);
            var identical = Element("E2", "A" + separator + "B", "C", 3d);
            var collidingUnderOldKey = Element("E3", "A", "B" + separator + "C", 7d);
            project.Elements.Add(first);
            project.Elements.Add(identical);
            project.Elements.Add(collidingUnderOldKey);

            var rows = MaterialUsageScheduleBuilder.Build(project);
            Equal(2, rows.Count, "distinct grouping tuples remain distinct");

            var firstGroup = rows.Single(x => x.Floor == "Floor AB");
            Equal(2, firstGroup.ElementCount, "identical material tuple still groups");
            Equal(5d, firstGroup.LengthM, "identical tuple quantities accumulate");
            Equal("C", firstGroup.MaterialName, "first group material preserved");
            Equal("Material", firstGroup.Component, "first group component preserved");

            var secondGroup = rows.Single(x => x.Floor == "Floor A");
            Equal(1, secondGroup.ElementCount, "old delimiter collision no longer merges");
            Equal(7d, secondGroup.LengthM, "second group quantity remains independent");
            Equal("B" + separator + "C", secondGroup.MaterialName, "separator-bearing material preserved");
            Equal("Material", secondGroup.Component, "second group component preserved");
        }

        private static ProjectElement Element(string id, string floorId, string material, double lengthM)
        {
            var element = new ProjectElement(id, ElementCategory.Beam, string.Empty, floorId, string.Empty);
            element.SetProperty("Material", material);
            element.SetQuantity("LengthM", lengthM);
            return element;
        }

        private static string LegacyDelimitedKey(string separator, params string[] tokens) =>
            string.Join(separator, tokens);

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
