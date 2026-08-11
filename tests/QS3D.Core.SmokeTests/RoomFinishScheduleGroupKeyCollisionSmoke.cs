using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishScheduleGroupKeyCollisionSmoke
    {
        internal static void Run()
        {
            const string separator = "\u001f";
            var project = new ProjectState("P-ROOM-FINISH-GROUP", "Room finish grouping");
            project.Floors.Add(new FloorDefinition("f1", "Tầng 1", 0d));

            var roomFamily = new ProjectFamily("room-family", "Phòng", ElementCategory.Room);
            project.Families.Add(roomFamily);
            var room = new ProjectElement("room-1", ElementCategory.Room, roomFamily.Id, "f1", "z");
            room.Properties["RoomName"] = "Phòng 101";
            project.Elements.Add(room);

            var firstFamily = new ProjectFamily("family" + separator + "material", "Finish A", ElementCategory.WallFinish);
            firstFamily.Properties["Material"] = "paint";
            var secondFamily = new ProjectFamily("family", "Finish B", ElementCategory.WallFinish);
            secondFamily.Properties["Material"] = "material" + separator + "paint";
            project.Families.Add(firstFamily);
            project.Families.Add(secondFamily);

            var first = Finish("finish-1", firstFamily.Id, 2d, "A1");
            var identical = Finish("finish-2", firstFamily.Id, 3d, "A2");
            var collidingUnderOldKey = Finish("finish-3", secondFamily.Id, 7d, "B1");
            project.Elements.Add(first);
            project.Elements.Add(identical);
            project.Elements.Add(collidingUnderOldKey);

            var rows = RoomFinishScheduleBuilder.Build(project);
            Equal(2, rows.Count, "distinct room-finish grouping tuples remain distinct");

            var firstGroup = rows.Single(x => x.FamilyName == "Finish A");
            Equal(2, firstGroup.Count, "identical tuple still groups");
            Equal(5d, firstGroup.AreaM2, "identical tuple area accumulates");
            Equal(5d, firstGroup.PrimaryQuantity, "identical tuple primary quantity accumulates");
            Equal("paint", firstGroup.Material, "first material preserved");
            Equal(2, firstGroup.ElementIds.Count, "first group element provenance preserved");
            Equal(2, firstGroup.SourceHandles.Count, "first group source provenance preserved");
            Equal(1, firstGroup.RoomIds.Count, "first group room provenance remains singular");

            var secondGroup = rows.Single(x => x.FamilyName == "Finish B");
            Equal(1, secondGroup.Count, "old delimiter collision no longer merges");
            Equal(7d, secondGroup.AreaM2, "second group area remains independent");
            Equal(7d, secondGroup.PrimaryQuantity, "second group primary quantity remains independent");
            Equal("material" + separator + "paint", secondGroup.Material, "separator-bearing material preserved");
            Equal("finish-3", secondGroup.ElementIds.Single(), "second group element provenance remains independent");
            Equal("B1", secondGroup.SourceHandles.Single(), "second group source provenance remains independent");
            Equal("room-1", secondGroup.RoomIds.Single(), "second group room provenance remains independent");
        }

        private static ProjectElement Finish(string id, string familyId, double areaM2, string sourceHandle)
        {
            var element = new ProjectElement(id, ElementCategory.WallFinish, familyId, "f1", "z");
            element.Properties["ParentRoomId"] = "room-1";
            element.Quantities["NetFinishAreaM2"] = areaM2;
            element.SourceHandles.Add(sourceHandle);
            return element;
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
