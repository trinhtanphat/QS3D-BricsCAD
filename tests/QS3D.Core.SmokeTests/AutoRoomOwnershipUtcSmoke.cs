using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomOwnershipUtcSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsSpoofedRoomBeforeMutation();
            RejectsSpoofedFamilyBeforeMutation();
            RejectsNonUtcStaleTimestampBeforeMutation();
        }

        private static void RejectsSpoofedRoomBeforeMutation()
        {
            var project = NewProject();
            var owned = Room("R1");
            project.Elements.Add(owned);
            var family = project.FindFamily("room-next") ?? throw new InvalidOperationException("missing family");
            var spoofed = Room("R1");
            var beforeMetadata = project.Metadata.Count;
            var beforeUpdated = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => AutoRoomLifecycle.SyncFamilyDefaults(project, spoofed, family));
            Require(ReferenceEquals(project.FindElement("R1"), owned), "owned room identity changed");
            Require(string.Equals(owned.FamilyId, "room", StringComparison.OrdinalIgnoreCase), "owned room was mutated by spoofed instance");
            Require(project.Metadata.Count == beforeMetadata, "spoofed room mutated project metadata");
            Require(project.UpdatedUtc == beforeUpdated, "spoofed room touched project timestamp");
        }

        private static void RejectsSpoofedFamilyBeforeMutation()
        {
            var project = NewProject();
            var room = Room("R2");
            project.Elements.Add(room);
            var spoofedFamily = new ProjectFamily("room-next", "Spoofed", ElementCategory.Room);
            spoofedFamily.Properties["HeightM"] = "99";
            var beforeMetadata = project.Metadata.Count;
            var beforeUpdated = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => AutoRoomLifecycle.SyncFamilyDefaults(project, room, spoofedFamily));
            Require(string.Equals(room.FamilyId, "room", StringComparison.OrdinalIgnoreCase), "spoofed family changed room family id");
            Require(!room.Properties.ContainsKey("HeightM"), "spoofed family propagated defaults");
            Require(project.Metadata.Count == beforeMetadata, "spoofed family mutated project metadata");
            Require(project.UpdatedUtc == beforeUpdated, "spoofed family touched project timestamp");
        }

        private static void RejectsNonUtcStaleTimestampBeforeMutation()
        {
            var project = NewProject();
            var room = Room("R3");
            room.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = "AA;BB";
            room.Properties[AutoRoomLifecycle.BoundarySourceSignatureKey] = "AA;BB";
            project.Elements.Add(room);
            var beforeUpdated = project.UpdatedUtc;

            Throws<ArgumentException>(() => AutoRoomLifecycle.MarkStaleForSelection(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(new[] { "AA", "BB" }, StringComparer.OrdinalIgnoreCase),
                "f",
                "z",
                new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Unspecified)));

            Require(!room.Properties.ContainsKey("BoundaryStaleUtc"), "non-UTC timestamp partially marked room stale");
            Require(!room.Properties.ContainsKey("BoundaryStaleReason"), "non-UTC timestamp wrote stale reason");
            Require(project.UpdatedUtc == beforeUpdated, "non-UTC timestamp touched project");
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState("P-auto-hardening", "Auto room hardening");
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Families.Add(new ProjectFamily("room", "Room", ElementCategory.Room));
            var next = new ProjectFamily("room-next", "Room next", ElementCategory.Room);
            next.Properties["HeightM"] = "3.6";
            project.Families.Add(next);
            return project;
        }

        private static ProjectElement Room(string id)
        {
            var room = new ProjectElement(id, ElementCategory.Room, "room", "f", "z");
            room.Properties[AutoRoomLifecycle.BoundaryModeKey] = AutoRoomLifecycle.BoundaryModeAutoNetwork;
            return room;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("AutoRoomOwnershipUtcSmoke expected " + typeof(T).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("AutoRoomOwnershipUtcSmoke: " + message);
        }
    }
}
