using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomHandleIdentitySmoke
    {
        internal static void Run()
        {
            NumericEquivalentSpellingsCollapse();
            LookupUsesCanonicalNumericIdentity();
            StaleSelectionUsesCanonicalNumericIdentity();
            LegacyMalformedAndZeroTokensRemainStable();
        }

        private static void NumericEquivalentSpellingsCollapse()
        {
            var normalized = AutoRoomLifecycle.NormalizeSourceHandles(new[] { "A", "0A", "000a", "0xA", " B " });
            Equal("A;B", normalized);
        }

        private static void LookupUsesCanonicalNumericIdentity()
        {
            var project = NewProject();
            var room = AutoRoom("ROOM-CANONICAL", "000a;0xB", project);
            project.Elements.Add(room);

            var found = AutoRoomLifecycle.FindBySourceSignature(project, "A;000B", "f", "z");
            True(ReferenceEquals(room, found));
            Equal("A;B", AutoRoomLifecycle.SourceSignature(room));

            AutoRoomLifecycle.MarkActive(room, "0xA;000b");
            Equal("A;B", room.Properties[AutoRoomLifecycle.BoundarySourceSignatureKey]);
        }

        private static void StaleSelectionUsesCanonicalNumericIdentity()
        {
            var project = NewProject();
            var room = AutoRoom("ROOM-STALE", "A;B", project);
            project.Elements.Add(room);

            var stale = AutoRoomLifecycle.MarkStaleForSelection(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(new[] { "000a", "0xB" }, StringComparer.OrdinalIgnoreCase),
                "f",
                "z",
                new DateTime(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc));

            Equal(1, stale.Count);
            True(ReferenceEquals(room, stale[0]));
            True(AutoRoomLifecycle.IsStaleAutoRoom(room));
        }

        private static void LegacyMalformedAndZeroTokensRemainStable()
        {
            var normalized = AutoRoomLifecycle.NormalizeSourceHandles(new[] { " 0 ", " xyz ", "0x", "XYZ" });
            Equal("0;0x;xyz", normalized);
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState("p-handle", "AutoRoom Handle Identity");
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.ActiveFloorId = "f";
            project.ActiveZoneId = "z";
            project.Families.Add(new ProjectFamily("room", "Room", ElementCategory.Room));
            return project;
        }

        private static ProjectElement AutoRoom(string id, string handles, ProjectState project)
        {
            var room = new ProjectElement(id, ElementCategory.Room, "room", "f", "z");
            room.Properties[AutoRoomLifecycle.BoundaryModeKey] = AutoRoomLifecycle.BoundaryModeAutoNetwork;
            room.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = handles;
            room.Properties[AutoRoomLifecycle.BoundarySourceSignatureKey] = handles;
            return room;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }
    }
}
