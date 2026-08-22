using System;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishIdentitySmoke
    {
        public static void Run()
        {
            CanonicalIdIsDeterministic();
            ReusesCanonicalFinishWithoutLegacyProvenance();
            ReusesPropertyLinkedLegacyFinish();
            ReusesDependencyLinkedLegacyFinish();
            RejectsCanonicalAndLegacyDuplicate();
            RejectsDuplicateSemanticFinishIdentity();
            DuplicateFinishesFailClosedAcrossSchedules();
            RejectsCanonicalLinkedToAnotherRoom();
            RejectsCanonicalIdCategoryCollision();
            RejectsConflictingLegacyProvenance();
            RejectsNonFinishCategory();
        }

        private static void CanonicalIdIsDeterministic()
        {
            Equal("ROOM-A-WallFinish", RoomFinishIdentityService.CanonicalId(" ROOM-A ", ElementCategory.WallFinish));
        }

        private static void ReusesCanonicalFinishWithoutLegacyProvenance()
        {
            var project = BaseProject(out var room, out _);
            var finish = new ProjectElement(
                RoomFinishIdentityService.CanonicalId(room.Id, ElementCategory.FloorFinish),
                ElementCategory.FloorFinish,
                "finish",
                room.FloorId,
                room.ZoneId);
            project.Elements.Add(finish);
            Same(finish, RoomFinishIdentityService.FindExisting(project, room, ElementCategory.FloorFinish));
        }

        private static void ReusesPropertyLinkedLegacyFinish()
        {
            var project = BaseProject(out var room, out _);
            var finish = Legacy("LEGACY-PROPERTY", ElementCategory.WallFinish, room);
            finish.Properties[AutoRoomLifecycle.RoomSourceIdKey] = room.Id;
            project.Elements.Add(finish);
            Same(finish, RoomFinishIdentityService.FindExisting(project, room, ElementCategory.WallFinish));
        }

        private static void ReusesDependencyLinkedLegacyFinish()
        {
            var project = BaseProject(out var room, out _);
            var finish = Legacy("LEGACY-DEPENDENCY", ElementCategory.CeilingFinish, room);
            finish.DependsOn.Add(room.Id);
            project.Elements.Add(finish);
            Same(finish, RoomFinishIdentityService.FindExisting(project, room, ElementCategory.CeilingFinish));
        }

        private static void RejectsCanonicalAndLegacyDuplicate()
        {
            var project = BaseProject(out var room, out _);
            AddDuplicateSkirting(project, room);
            Throws<InvalidOperationException>(() => RoomFinishIdentityService.FindExisting(project, room, ElementCategory.Skirting), "Multiple Skirting finishes reference Room");
        }

        private static void RejectsDuplicateSemanticFinishIdentity()
        {
            var project = BaseProject(out var room, out _);
            var first = Legacy("LEGACY-DUP", ElementCategory.WallFinish, room);
            first.Properties[AutoRoomLifecycle.RoomSourceIdKey] = room.Id;
            var second = Legacy("legacy-dup", ElementCategory.WallFinish, room);
            second.Properties[AutoRoomLifecycle.RoomSourceIdKey] = room.Id;
            project.Elements.Add(first);
            project.Elements.Add(second);

            Throws<InvalidOperationException>(() => RoomFinishIdentityService.FindExisting(project, room, ElementCategory.WallFinish), "duplicate semantic element id");
        }

        private static void DuplicateFinishesFailClosedAcrossSchedules()
        {
            var project = BaseProject(out var room, out _);
            var family = project.FindFamily("finish") ?? throw new Exception("Missing finish family.");
            family.Properties["Material"] = "Sơn";
            var canonical = Legacy(RoomFinishIdentityService.CanonicalId(room.Id, ElementCategory.WallFinish), ElementCategory.WallFinish, room);
            canonical.Properties[AutoRoomLifecycle.RoomSourceIdKey] = room.Id;
            canonical.Quantities["NetFinishAreaM2"] = 10d;
            canonical.MarkClean(ElementDirtyFlags.All);
            var legacy = Legacy("LEGACY-WALL-FINISH", ElementCategory.WallFinish, room);
            legacy.Properties["ParentRoomId"] = room.Id;
            legacy.Quantities["NetFinishAreaM2"] = 12d;
            legacy.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(canonical);
            project.Elements.Add(legacy);

            Throws<InvalidOperationException>(() => ProjectQuantityReportBuilder.Group(project), "Multiple WallFinish finishes reference Room");
            Throws<InvalidOperationException>(() => RoomFinishScheduleBuilder.Build(project), "Multiple WallFinish finishes reference Room");
            Throws<InvalidOperationException>(() => MaterialUsageScheduleBuilder.Build(project), "Multiple WallFinish finishes reference Room");
        }

        private static void RejectsCanonicalLinkedToAnotherRoom()
        {
            var project = BaseProject(out var room, out var secondRoom);
            var canonical = Legacy(RoomFinishIdentityService.CanonicalId(room.Id, ElementCategory.Waterproofing), ElementCategory.Waterproofing, room);
            canonical.Properties[AutoRoomLifecycle.RoomSourceIdKey] = secondRoom.Id;
            project.Elements.Add(canonical);
            Throws<InvalidOperationException>(() => RoomFinishIdentityService.FindExisting(project, room, ElementCategory.Waterproofing), "references another Room");
        }

        private static void RejectsCanonicalIdCategoryCollision()
        {
            var project = BaseProject(out var room, out _);
            project.Elements.Add(new ProjectElement(
                RoomFinishIdentityService.CanonicalId(room.Id, ElementCategory.WallFinish),
                ElementCategory.Door,
                string.Empty,
                room.FloorId,
                room.ZoneId));
            Throws<InvalidOperationException>(() => RoomFinishIdentityService.FindExisting(project, room, ElementCategory.WallFinish), "id collision");
        }

        private static void RejectsConflictingLegacyProvenance()
        {
            var project = BaseProject(out var room, out var secondRoom);
            var legacy = Legacy("LEGACY-CONFLICT", ElementCategory.FloorFinish, room);
            legacy.Properties[AutoRoomLifecycle.RoomSourceIdKey] = room.Id;
            legacy.Properties["ParentRoomId"] = secondRoom.Id;
            project.Elements.Add(legacy);
            Throws<InvalidOperationException>(() => RoomFinishIdentityService.FindExisting(project, room, ElementCategory.FloorFinish), "Conflicting room provenance");
        }

        private static void RejectsNonFinishCategory()
        {
            var project = BaseProject(out var room, out _);
            Throws<ArgumentOutOfRangeException>(() => RoomFinishIdentityService.FindExisting(project, room, ElementCategory.Door), "HT_Phòng finish category");
        }

        private static void AddDuplicateSkirting(ProjectState project, ProjectElement room)
        {
            var canonical = Legacy(RoomFinishIdentityService.CanonicalId(room.Id, ElementCategory.Skirting), ElementCategory.Skirting, room);
            canonical.Properties[AutoRoomLifecycle.RoomSourceIdKey] = room.Id;
            var legacy = Legacy("LEGACY-SKIRT", ElementCategory.Skirting, room);
            legacy.Properties["ParentRoomId"] = room.Id;
            project.Elements.Add(canonical);
            project.Elements.Add(legacy);
        }

        private static ProjectState BaseProject(out ProjectElement room, out ProjectElement secondRoom)
        {
            var project = new ProjectState("p", "Room finish identity");
            project.Floors.Add(new FloorDefinition("f1", "Tầng 1", 0d));
            project.Zones.Add(new ZoneDefinition("z1", "Zone 1"));
            project.Families.Add(new ProjectFamily("room", "Phòng", ElementCategory.Room));
            project.Families.Add(new ProjectFamily("finish", "Finish", ElementCategory.WallFinish));
            room = new ProjectElement("ROOM-A", ElementCategory.Room, "room", "f1", "z1");
            secondRoom = new ProjectElement("ROOM-B", ElementCategory.Room, "room", "f1", "z1");
            project.Elements.Add(room);
            project.Elements.Add(secondRoom);
            return project;
        }

        private static ProjectElement Legacy(string id, ElementCategory category, ProjectElement room) =>
            new ProjectElement(id, category, "finish", room.FloorId, room.ZoneId);

        private static void Same(ProjectElement expected, ProjectElement? actual)
        {
            if (!ReferenceEquals(expected, actual)) throw new Exception("Expected the existing semantic finish to be reused.");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action, string messagePart) where T : Exception
        {
            try { action(); }
            catch (T ex)
            {
                if (ex.Message.IndexOf(messagePart, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Expected exception message containing '" + messagePart + "', got: " + ex.Message);
                return;
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
