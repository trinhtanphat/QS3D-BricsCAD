using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishSynchronizationSmoke
    {
        public static void Run()
        {
            RepairsLegacyDependencyScopeAndFingerprint();
            RemovedRoomMetricsClearOldDeductions();
            QuantityFallbackIsCanonicalized();
            RepeatedSynchronizationIsNoOpButRepairsDrift();
            BatchFailureRollsBackEarlierFinishMutation();
            RejectsInvalidRoomMetric();
            RejectsStaleAutoRoom();
            RejectsForeignProjectObject();
        }

        private static void RepairsLegacyDependencyScopeAndFingerprint()
        {
            var project = Project(out var room);
            room.FloorId = "f2";
            room.ZoneId = "z2";
            room.DrawingFingerprint = "DWG-FP";
            room.Properties["AreaM2"] = "20";
            room.Properties["PerimeterM"] = "18";
            room.Properties["HeightM"] = "3";
            room.Properties["OpeningAreaM2"] = "2";
            room.Properties["DoorWidthM"] = "0.9";

            var finish = Finish("LEGACY-WALL", ElementCategory.WallFinish, "f1", "z1");
            finish.Properties["ParentRoomId"] = room.Id;
            project.Elements.Add(finish);

            RoomFinishSynchronizationService.Synchronize(project, room, finish);
            Equal("f2", finish.FloorId);
            Equal("z2", finish.ZoneId);
            Equal("DWG-FP", finish.DrawingFingerprint);
            Equal(room.Id, finish.Properties[AutoRoomLifecycle.RoomSourceIdKey]);
            True(finish.DependsOn.Any(x => string.Equals(x, room.Id, StringComparison.OrdinalIgnoreCase)));
            Equal("20", finish.Properties["AreaM2"]);
            Equal("18", finish.Properties["PerimeterM"]);
            Equal("3", finish.Properties["HeightM"]);
            Equal("2", finish.Properties["OpeningAreaM2"]);
            Equal("0.9", finish.Properties["DoorWidthM"]);
            True(finish.Dirty != ElementDirtyFlags.None);

            new RoomRegenerator().Regenerate(project, finish);
            Near(52d, finish.Quantities["NetFinishAreaM2"]);
        }

        private static void RemovedRoomMetricsClearOldDeductions()
        {
            var project = Project(out var room);
            room.Properties["AreaM2"] = "30";
            room.Properties["PerimeterM"] = "10";
            room.Properties["HeightM"] = "3";

            var wall = Finish("WALL", ElementCategory.WallFinish, room.FloorId, room.ZoneId);
            wall.Properties[AutoRoomLifecycle.RoomSourceIdKey] = room.Id;
            wall.Properties["OpeningAreaM2"] = "4";
            wall.Properties["DoorWidthM"] = "1";
            project.Elements.Add(wall);

            var skirting = Finish("SKIRT", ElementCategory.Skirting, room.FloorId, room.ZoneId);
            skirting.Properties[AutoRoomLifecycle.RoomSourceIdKey] = room.Id;
            skirting.Properties["DoorWidthM"] = "2";
            project.Elements.Add(skirting);

            RoomFinishSynchronizationService.Synchronize(project, room, wall);
            RoomFinishSynchronizationService.Synchronize(project, room, skirting);
            True(!wall.Properties.ContainsKey("OpeningAreaM2"));
            True(!wall.Properties.ContainsKey("DoorWidthM"));
            True(!skirting.Properties.ContainsKey("DoorWidthM"));

            new RoomRegenerator().Regenerate(project, wall);
            new RoomRegenerator().Regenerate(project, skirting);
            Near(30d, wall.Quantities["NetFinishAreaM2"]);
            Near(10d, skirting.Quantities["SkirtingLengthM"]);
        }

        private static void QuantityFallbackIsCanonicalized()
        {
            var project = Project(out var room);
            room.SetQuantity("AreaM2", 12.5d);
            var finish = Finish("FLOOR", ElementCategory.FloorFinish, room.FloorId, room.ZoneId);
            finish.DependsOn.Add(room.Id);
            project.Elements.Add(finish);

            RoomFinishSynchronizationService.Synchronize(project, room, finish);
            Equal("12.5", finish.Properties["AreaM2"]);
        }

        private static void RepeatedSynchronizationIsNoOpButRepairsDrift()
        {
            var project = Project(out var room);
            room.DrawingFingerprint = "ROOM-FP";
            room.Properties["AreaM2"] = "12.5";
            room.Properties["PerimeterM"] = "10";
            room.Properties["HeightM"] = "3";

            var finish = Finish("IDEMPOTENT", ElementCategory.WallFinish, room.FloorId, room.ZoneId);
            project.Elements.Add(finish);

            var beforeFirstVersion = project.ChangeVersion;
            RoomFinishSynchronizationService.Synchronize(project, room, finish);
            True(project.ChangeVersion == beforeFirstVersion + 1L);
            finish.MarkClean(ElementDirtyFlags.All);

            var canonicalVersion = project.ChangeVersion;
            var canonicalProjectUpdatedUtc = project.UpdatedUtc;
            var canonicalFinishUpdatedUtc = finish.UpdatedUtc;
            RoomFinishSynchronizationService.Synchronize(project, room, finish);

            True(project.ChangeVersion == canonicalVersion);
            True(project.UpdatedUtc == canonicalProjectUpdatedUtc);
            True(finish.UpdatedUtc == canonicalFinishUpdatedUtc);
            True(finish.Dirty == ElementDirtyFlags.None);
            True(finish.DependsOn.Count(x => string.Equals((x ?? string.Empty).Trim(), room.Id, StringComparison.OrdinalIgnoreCase)) == 1);
            Equal(room.Id, finish.DependsOn[finish.DependsOn.Count - 1]);

            finish.Properties["DoorWidthM"] = "0.9";
            finish.DependsOn.Insert(0, room.Id.ToLowerInvariant());
            var beforeRepairVersion = project.ChangeVersion;
            RoomFinishSynchronizationService.Synchronize(project, room, finish);

            True(project.ChangeVersion == beforeRepairVersion + 1L);
            True(finish.Dirty == ElementDirtyFlags.All);
            True(!finish.Properties.ContainsKey("DoorWidthM"));
            True(finish.DependsOn.Count(x => string.Equals((x ?? string.Empty).Trim(), room.Id, StringComparison.OrdinalIgnoreCase)) == 1);
            Equal(room.Id, finish.DependsOn[finish.DependsOn.Count - 1]);
        }

        private static void BatchFailureRollsBackEarlierFinishMutation()
        {
            var project = Project(out var room);
            room.FloorId = "f2";
            room.ZoneId = "z2";
            room.DrawingFingerprint = "NEW-FP";
            room.Properties["AreaM2"] = "25";

            var floor = Finish("LEGACY-FLOOR", ElementCategory.FloorFinish, "f1", "z1");
            floor.DrawingFingerprint = "OLD-FP";
            floor.Properties["ParentRoomId"] = room.Id;
            project.Elements.Add(floor);

            var otherRoom = new ProjectElement("ROOM-OTHER", ElementCategory.Room, room.FamilyId, "f2", "z2");
            project.Elements.Add(otherRoom);
            var bad = Finish("BAD-WATERPROOF", ElementCategory.Waterproofing, "f1", "z1");
            bad.Properties[AutoRoomLifecycle.RoomSourceIdKey] = room.Id;
            bad.Properties["ParentRoomId"] = otherRoom.Id;
            project.Elements.Add(bad);

            Throws<InvalidOperationException>(() => RoomFinishSynchronizationService.SynchronizeExisting(project, room));

            var restored = project.FindElement("LEGACY-FLOOR") ?? throw new Exception("Rollback lost the earlier finish.");
            Equal("f1", restored.FloorId);
            Equal("z1", restored.ZoneId);
            Equal("OLD-FP", restored.DrawingFingerprint);
            True(!restored.Properties.ContainsKey(AutoRoomLifecycle.RoomSourceIdKey));
            True(restored.Properties.TryGetValue("ParentRoomId", out var parent) && parent == room.Id);
            True(!restored.DependsOn.Any(x => string.Equals(x, room.Id, StringComparison.OrdinalIgnoreCase)));
            True(!restored.Properties.ContainsKey("AreaM2"));
        }

        private static void RejectsInvalidRoomMetric()
        {
            var project = Project(out var room);
            room.Properties["AreaM2"] = "not-a-number";
            var finish = Finish("BAD", ElementCategory.FloorFinish, room.FloorId, room.ZoneId);
            finish.DependsOn.Add(room.Id);
            project.Elements.Add(finish);
            Throws<InvalidOperationException>(() => RoomFinishSynchronizationService.Synchronize(project, room, finish));
        }

        private static void RejectsStaleAutoRoom()
        {
            var project = Project(out var room);
            room.Properties[AutoRoomLifecycle.BoundaryModeKey] = AutoRoomLifecycle.BoundaryModeAutoNetwork;
            room.Properties[AutoRoomLifecycle.BoundaryStateKey] = AutoRoomLifecycle.BoundaryStateStale;
            var finish = Finish("STALE", ElementCategory.CeilingFinish, room.FloorId, room.ZoneId);
            finish.DependsOn.Add(room.Id);
            project.Elements.Add(finish);
            Throws<InvalidOperationException>(() => RoomFinishSynchronizationService.Synchronize(project, room, finish));
        }

        private static void RejectsForeignProjectObject()
        {
            var project = Project(out var room);
            var foreignRoom = new ProjectElement(room.Id, ElementCategory.Room, room.FamilyId, room.FloorId, room.ZoneId);
            var finish = Finish("OWNED", ElementCategory.FloorFinish, room.FloorId, room.ZoneId);
            finish.DependsOn.Add(room.Id);
            project.Elements.Add(finish);
            Throws<ArgumentException>(() => RoomFinishSynchronizationService.Synchronize(project, foreignRoom, finish));
        }

        private static ProjectState Project(out ProjectElement room)
        {
            var project = new ProjectState("sync", "Room finish sync");
            project.Floors.Add(new FloorDefinition("f1", "Tầng 1", 0d));
            project.Floors.Add(new FloorDefinition("f2", "Tầng 2", 3.6d));
            project.Zones.Add(new ZoneDefinition("z1", "Zone 1"));
            project.Zones.Add(new ZoneDefinition("z2", "Zone 2"));
            project.Families.Add(new ProjectFamily("room", "Phòng", ElementCategory.Room));
            room = new ProjectElement("ROOM", ElementCategory.Room, "room", "f1", "z1");
            project.Elements.Add(room);
            return project;
        }

        private static ProjectElement Finish(string id, ElementCategory category, string floorId, string zoneId) =>
            new ProjectElement(id, category, string.Empty, floorId, zoneId);

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}