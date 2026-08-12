using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomStaleNoOpSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            FirstTransitionMutatesOnce();
            RepeatedCanonicalStaleIsNoOp();
            MalformedStaleMetadataIsRepaired();
        }

        private static void FirstTransitionMutatesOnce()
        {
            var project = CreateProject("P-AUTO-STALE-NOOP-1", out var room);
            room.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var staleUtc = new DateTime(2026, 8, 12, 6, 0, 0, DateTimeKind.Utc);

            var changed = Mark(project, staleUtc);

            Equal(1, changed.Count, "first transition count");
            Same(room, changed[0], "first transition room");
            Equal(beforeVersion + 1L, project.ChangeVersion, "first transition project revision");
            Equal(AutoRoomLifecycle.BoundaryStateStale, room.Properties[AutoRoomLifecycle.BoundaryStateKey], "first transition state");
            Equal("TopologyChanged", room.Properties["BoundaryStaleReason"], "first transition reason");
            Equal(staleUtc.ToString("O"), room.Properties["BoundaryStaleUtc"], "first transition timestamp");
        }

        private static void RepeatedCanonicalStaleIsNoOp()
        {
            var project = CreateProject("P-AUTO-STALE-NOOP-2", out var room);
            var firstUtc = new DateTime(2026, 8, 12, 6, 1, 0, DateTimeKind.Utc);
            Equal(1, Mark(project, firstUtc).Count, "initial stale count");
            room.MarkClean(ElementDirtyFlags.All);

            var beforeVersion = project.ChangeVersion;
            var beforeProjectUpdated = project.UpdatedUtc;
            var beforeRoomUpdated = room.UpdatedUtc;
            var beforeStaleUtc = room.Properties["BoundaryStaleUtc"];
            var repeated = Mark(project, firstUtc.AddMinutes(5));

            Equal(0, repeated.Count, "repeated stale count");
            Equal(beforeVersion, project.ChangeVersion, "repeated project revision");
            Equal(beforeProjectUpdated, project.UpdatedUtc, "repeated project timestamp");
            Equal(beforeRoomUpdated, room.UpdatedUtc, "repeated room timestamp");
            Equal(ElementDirtyFlags.None, room.Dirty, "repeated dirty flags");
            Equal(beforeStaleUtc, room.Properties["BoundaryStaleUtc"], "repeated stale timestamp");
        }

        private static void MalformedStaleMetadataIsRepaired()
        {
            var project = CreateProject("P-AUTO-STALE-NOOP-3", out var room);
            room.Properties[AutoRoomLifecycle.BoundaryStateKey] = AutoRoomLifecycle.BoundaryStateStale;
            room.Properties["BoundaryStaleReason"] = "TopologyChanged";
            room.Properties["BoundaryStaleUtc"] = "2026-08-12 06:02:00Z";
            room.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var repairUtc = new DateTime(2026, 8, 12, 6, 3, 0, DateTimeKind.Utc);

            var changed = Mark(project, repairUtc);

            Equal(1, changed.Count, "repair count");
            Same(room, changed[0], "repair room");
            Equal(beforeVersion + 1L, project.ChangeVersion, "repair project revision");
            Equal(repairUtc.ToString("O"), room.Properties["BoundaryStaleUtc"], "repair timestamp");
            Equal("TopologyChanged", room.Properties["BoundaryStaleReason"], "repair reason");
            True((room.Dirty & (ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity)) ==
                 (ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity), "repair dirty flags");
        }

        private static IReadOnlyList<ProjectElement> Mark(ProjectState project, DateTime utcNow)
        {
            return AutoRoomLifecycle.MarkStaleForSelection(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(new[] { "A", "B", "C" }, StringComparer.OrdinalIgnoreCase),
                "f",
                "z",
                utcNow);
        }

        private static ProjectState CreateProject(string id, out ProjectElement room)
        {
            var project = new ProjectState(id, "Auto Room stale no-op");
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            room = new ProjectElement("ROOM-STALE-NOOP", ElementCategory.Room, "room", "f", "z");
            room.Properties[AutoRoomLifecycle.BoundaryModeKey] = AutoRoomLifecycle.BoundaryModeAutoNetwork;
            room.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = "A;B;C";
            room.Properties[AutoRoomLifecycle.BoundarySourceSignatureKey] = "A;B;C";
            project.Elements.Add(room);
            return project;
        }

        private static void Same(object expected, object actual, string label)
        {
            if (!ReferenceEquals(expected, actual))
                throw new Exception("AutoRoomStaleNoOpSmoke expected same instance: " + label + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new Exception("AutoRoomStaleNoOpSmoke expected true: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("AutoRoomStaleNoOpSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
