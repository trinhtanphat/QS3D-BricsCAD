using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomCanonicalScopeSmoke
    {
        public static void Run()
        {
            FindBySignatureUsesCanonicalScope();
            StaleSelectionUsesCanonicalScope();
            StaleSelectionProtectsCanonicalActiveIds();
            FinishQuantityScopeUsesCanonicalIdentity();
        }

        private static void FindBySignatureUsesCanonicalScope()
        {
            var project = new ProjectState("P-ROOM-SCOPE-1", "Room scope lookup");
            var room = AutoRoom("ROOM-1", "  floor-a  ", "  Zone-A  ", "a1;b2");
            project.Elements.Add(room);

            Same(room, AutoRoomLifecycle.FindBySourceSignature(project, " B2 ; A1 ", " FLOOR-A ", " zone-a "));
            Null(AutoRoomLifecycle.FindBySourceSignature(project, "A1;B2", "FLOOR-B", "ZONE-A"));
            Null(AutoRoomLifecycle.FindBySourceSignature(project, "A1;B2", "FLOOR-A", "ZONE-B"));
        }

        private static void StaleSelectionUsesCanonicalScope()
        {
            var project = new ProjectState("P-ROOM-SCOPE-2", "Room stale scope");
            var matching = AutoRoom("ROOM-MATCH", "  Floor-A ", " zone-a ", "AA;BB");
            var otherFloor = AutoRoom("ROOM-OTHER-FLOOR", "Floor-B", "Zone-A", "AA;BB");
            var otherZone = AutoRoom("ROOM-OTHER-ZONE", "Floor-A", "Zone-B", "AA;BB");
            project.Elements.Add(matching);
            project.Elements.Add(otherFloor);
            project.Elements.Add(otherZone);
            var beforeVersion = project.ChangeVersion;

            var stale = AutoRoomLifecycle.MarkStaleForSelection(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(new[] { " aa ", "bb" }, StringComparer.OrdinalIgnoreCase),
                " floor-a ",
                " ZONE-A ",
                new DateTime(2026, 8, 11, 15, 0, 0, DateTimeKind.Utc));

            Equal(1, stale.Count);
            Same(matching, stale[0]);
            True(AutoRoomLifecycle.IsStaleAutoRoom(matching));
            False(AutoRoomLifecycle.IsStaleAutoRoom(otherFloor));
            False(AutoRoomLifecycle.IsStaleAutoRoom(otherZone));
            Equal(beforeVersion + 1L, project.ChangeVersion);
        }

        private static void StaleSelectionProtectsCanonicalActiveIds()
        {
            var project = new ProjectState("P-ROOM-SCOPE-4", "Room active id scope");
            var active = AutoRoom("ROOM-LIVE", "Floor-A", "Zone-A", "AA;BB");
            var staleCandidate = AutoRoom("ROOM-OLD", "Floor-A", "Zone-A", "AA;BB");
            project.Elements.Add(active);
            project.Elements.Add(staleCandidate);
            var beforeVersion = project.ChangeVersion;

            var stale = AutoRoomLifecycle.MarkStaleForSelection(
                project,
                new HashSet<string>(new[] { " room-live " }, StringComparer.Ordinal),
                new HashSet<string>(new[] { "AA", "BB" }, StringComparer.Ordinal),
                "FLOOR-A",
                "ZONE-A",
                new DateTime(2026, 8, 12, 0, 49, 0, DateTimeKind.Utc));

            Equal(1, stale.Count);
            Same(staleCandidate, stale[0]);
            False(AutoRoomLifecycle.IsStaleAutoRoom(active));
            True(AutoRoomLifecycle.IsStaleAutoRoom(staleCandidate));
            Equal(beforeVersion + 1L, project.ChangeVersion);
        }

        private static void FinishQuantityScopeUsesCanonicalIdentity()
        {
            var project = new ProjectState("P-ROOM-SCOPE-3", "Room finish scope");
            var room = new ProjectElement("ROOM-LIVE", ElementCategory.Room)
            {
                FloorId = "  floor-a  ",
                ZoneId = " ZONE-A "
            };
            var finish = new ProjectElement("FINISH-1", ElementCategory.FloorFinish)
            {
                FloorId = "FLOOR-A",
                ZoneId = " zone-a "
            };
            finish.Properties[AutoRoomLifecycle.RoomSourceIdKey] = "room-live";
            project.Elements.Add(room);
            project.Elements.Add(finish);

            False(AutoRoomLifecycle.IsExcludedFromQuantity(project, finish));

            finish.ZoneId = "ZONE-B";
            True(AutoRoomLifecycle.IsExcludedFromQuantity(project, finish));
        }

        private static ProjectElement AutoRoom(string id, string floorId, string zoneId, string signature)
        {
            var room = new ProjectElement(id, ElementCategory.Room)
            {
                FloorId = floorId,
                ZoneId = zoneId
            };
            room.Properties[AutoRoomLifecycle.BoundaryModeKey] = AutoRoomLifecycle.BoundaryModeAutoNetwork;
            room.Properties[AutoRoomLifecycle.BoundaryStateKey] = AutoRoomLifecycle.BoundaryStateActive;
            room.Properties[AutoRoomLifecycle.BoundarySourceSignatureKey] = signature;
            room.MarkClean(ElementDirtyFlags.All);
            return room;
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected false.");
        }

        private static void Null(object? value)
        {
            if (value != null) throw new Exception("Expected null.");
        }

        private static void Same(object expected, object? actual)
        {
            if (!ReferenceEquals(expected, actual)) throw new Exception("Expected same object reference.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
