using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishSyncGlobalElementIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsUnrelatedDuplicateIdsBeforeMutation();
            AllowsValidDirectSynchronization();
        }

        private static void RejectsUnrelatedDuplicateIdsBeforeMutation()
        {
            var project = new ProjectState("P-FINISH-SYNC-DUP", "Room finish duplicate smoke");
            project.Elements.Add(new ProjectElement("DUP", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("dup", ElementCategory.Column));
            var room = new ProjectElement("ROOM", ElementCategory.Room, string.Empty, "F1", "Z1")
            {
                DrawingFingerprint = "ROOM-FP"
            };
            room.Properties["AreaM2"] = "12.5";
            var finish = new ProjectElement("FINISH", ElementCategory.FloorFinish);
            project.Elements.Add(room);
            project.Elements.Add(finish);

            var beforeFloor = finish.FloorId;
            var beforeZone = finish.ZoneId;
            var beforeFingerprint = finish.DrawingFingerprint;
            var beforeProperties = finish.Properties.Count;
            var beforeDependencies = finish.DependsOn.Count;
            var beforeDirty = finish.Dirty;
            var beforeFinishUpdated = finish.UpdatedUtc;
            var beforeVersion = project.ChangeVersion;
            var beforeProjectUpdated = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => RoomFinishSynchronizationService.Synchronize(project, room, finish));

            Equal(beforeFloor, finish.FloorId, "malformed FloorId");
            Equal(beforeZone, finish.ZoneId, "malformed ZoneId");
            Equal(beforeFingerprint, finish.DrawingFingerprint, "malformed fingerprint");
            Equal(beforeProperties, finish.Properties.Count, "malformed property count");
            Equal(beforeDependencies, finish.DependsOn.Count, "malformed dependency count");
            Equal(beforeDirty, finish.Dirty, "malformed dirty flags");
            Equal(beforeFinishUpdated, finish.UpdatedUtc, "malformed finish timestamp");
            Equal(beforeVersion, project.ChangeVersion, "malformed project version");
            Equal(beforeProjectUpdated, project.UpdatedUtc, "malformed project timestamp");
        }

        private static void AllowsValidDirectSynchronization()
        {
            var project = new ProjectState("P-FINISH-SYNC-VALID", "Room finish valid smoke");
            var room = new ProjectElement("ROOM", ElementCategory.Room, string.Empty, "F1", "Z1")
            {
                DrawingFingerprint = "ROOM-FP"
            };
            room.Properties["AreaM2"] = "12.5";
            var finish = new ProjectElement("FINISH", ElementCategory.FloorFinish);
            project.Elements.Add(room);
            project.Elements.Add(finish);
            var beforeVersion = project.ChangeVersion;

            RoomFinishSynchronizationService.Synchronize(project, room, finish);

            Equal("F1", finish.FloorId, "valid FloorId");
            Equal("Z1", finish.ZoneId, "valid ZoneId");
            Equal("ROOM-FP", finish.DrawingFingerprint, "valid fingerprint");
            Equal("ROOM", finish.Properties[AutoRoomLifecycle.RoomSourceIdKey], "valid RoomSourceId");
            Equal("12.5", finish.Properties["AreaM2"], "valid AreaM2");
            Equal(1, finish.DependsOn.Count, "valid dependency count");
            Equal("ROOM", finish.DependsOn[0], "valid dependency");
            Equal(beforeVersion + 1L, project.ChangeVersion, "valid project revision");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("RoomFinishSyncGlobalElementIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("RoomFinishSyncGlobalElementIntegritySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
