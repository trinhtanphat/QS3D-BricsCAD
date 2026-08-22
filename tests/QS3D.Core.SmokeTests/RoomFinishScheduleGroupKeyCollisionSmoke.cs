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
            const string separator = "|";
            var project = new ProjectState("P-ROOM-FINISH-GROUP", "Room finish grouping");
            project.Floors.Add(new FloorDefinition("A" + separator + "B", "Floor AB", 0d));
            project.Floors.Add(new FloorDefinition("A", "Floor A", 3d));
            project.Floors.Add(new FloorDefinition("D", "Floor D", 6d));
            project.Zones.Add(new ZoneDefinition("z", "Zone Z"));

            var roomFamily = new ProjectFamily("room-family", "Phòng", ElementCategory.Room);
            var finishFamily = new ProjectFamily("wf", "Sơn nước", ElementCategory.WallFinish);
            finishFamily.Properties["Material"] = "Paint";
            project.Families.Add(roomFamily);
            project.Families.Add(finishFamily);

            Equal(
                LegacyDelimitedKey(separator, "A" + separator + "B", "C", "WallFinish", "wf", "Paint", "m\u00b2"),
                LegacyDelimitedKey(separator, "A", "B" + separator + "C", "WallFinish", "wf", "Paint", "m\u00b2"),
                "fixture tuples collide under six-token delimiter-only grouping");

            var firstRoom = Room("C", roomFamily.Id, "A" + separator + "B", "Room C");
            var secondRoom = Room("B" + separator + "C", roomFamily.Id, "A", "Room BC");
            project.Elements.Add(firstRoom);
            project.Elements.Add(secondRoom);

            var first = LinkedFinish("finish-1", finishFamily.Id, "A" + separator + "B", firstRoom.Id, 2d, "A1");
            var collidingUnderOldKey = LinkedFinish("finish-2", finishFamily.Id, "A", secondRoom.Id, 7d, "B1");
            var identicalUnlinked = UnlinkedFinish("finish-3", finishFamily.Id, "D", 3d, "C1");
            var identicalUnlinkedAgain = UnlinkedFinish("finish-4", finishFamily.Id, "D", 4d, "C2");
            project.Elements.Add(first);
            project.Elements.Add(collidingUnderOldKey);
            project.Elements.Add(identicalUnlinked);
            project.Elements.Add(identicalUnlinkedAgain);

            var rows = RoomFinishScheduleBuilder.Build(project);
            Equal(3, rows.Count, "old delimiter collision remains split while identical tuples still group");

            var firstGroup = rows.Single(x => x.Room == "Room C");
            Equal(1, firstGroup.Count, "first linked finish remains independent");
            Equal(2d, firstGroup.PrimaryQuantity, "first linked quantity remains independent");
            Equal("finish-1", firstGroup.ElementIds.Single(), "first element provenance remains independent");
            Equal("A1", firstGroup.SourceHandles.Single(), "first source provenance remains independent");
            Equal("C", firstGroup.RoomIds.Single(), "first room provenance remains independent");

            var secondGroup = rows.Single(x => x.Room == "Room BC");
            Equal(1, secondGroup.Count, "old delimiter collision no longer merges");
            Equal(7d, secondGroup.PrimaryQuantity, "second linked quantity remains independent");
            Equal("finish-2", secondGroup.ElementIds.Single(), "second element provenance remains independent");
            Equal("B1", secondGroup.SourceHandles.Single(), "second source provenance remains independent");
            Equal("B" + separator + "C", secondGroup.RoomIds.Single(), "separator-bearing room id is preserved");

            var identicalGroup = rows.Single(x => x.Room == "(chưa liên kết phòng)");
            Equal(2, identicalGroup.Count, "identical unlinked tuple still groups");
            Equal(7d, identicalGroup.PrimaryQuantity, "identical tuple quantities still accumulate");
            Equal(2, identicalGroup.ElementIds.Count, "identical tuple element provenance accumulates");
            Equal(2, identicalGroup.SourceHandles.Count, "identical tuple source provenance accumulates");
            Equal(0, identicalGroup.RoomIds.Count, "unlinked tuple does not invent room provenance");
        }

        private static ProjectElement Room(string id, string familyId, string floorId, string label)
        {
            var room = new ProjectElement(id, ElementCategory.Room, familyId, floorId, "z");
            room.Properties["RoomName"] = label;
            return room;
        }

        private static ProjectElement LinkedFinish(
            string id,
            string familyId,
            string floorId,
            string roomId,
            double areaM2,
            string sourceHandle)
        {
            var element = UnlinkedFinish(id, familyId, floorId, areaM2, sourceHandle);
            element.Properties["ParentRoomId"] = roomId;
            return element;
        }

        private static ProjectElement UnlinkedFinish(string id, string familyId, string floorId, double areaM2, string sourceHandle)
        {
            var element = new ProjectElement(id, ElementCategory.WallFinish, familyId, floorId, "z");
            element.Quantities["NetFinishAreaM2"] = areaM2;
            element.SourceHandles.Add(sourceHandle);
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
