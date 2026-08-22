using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomDanglingPreviousFamilySmoke
    {
        public static void Run()
        {
            DanglingPreviousFamilyFailsBeforeMutation();
            EmptyPreviousFamilyStillBootstraps();
            CanonicalSameFamilyRemainsNoOp();
        }

        private static void DanglingPreviousFamilyFailsBeforeMutation()
        {
            var project = new ProjectState("auto-room-dangling-family", "Auto Room dangling Family");
            var target = new ProjectFamily("ROOM-TARGET", "Room Target", ElementCategory.Room);
            target.Properties["HeightM"] = "3.6";
            project.Families.Add(target);

            var room = new ProjectElement("R1", ElementCategory.Room, "MISSING-FAMILY", string.Empty, string.Empty);
            room.Properties["HeightM"] = "3.0";
            room.Properties["InstanceOverride"] = "keep";
            room.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(room);
            project.Metadata["AutoRoomFamilyDefault:R1:HeightM"] = "3.0";

            var beforeFamilyId = room.FamilyId;
            var beforeHeight = room.Properties["HeightM"];
            var beforeOverride = room.Properties["InstanceOverride"];
            var beforeDirty = room.Dirty;
            var beforeRoomUpdatedUtc = room.UpdatedUtc;
            var beforeProjectUpdatedUtc = project.UpdatedUtc;
            var beforeVersion = project.ChangeVersion;
            var beforeMetadataCount = project.Metadata.Count;
            var beforeSnapshot = project.Metadata["AutoRoomFamilyDefault:R1:HeightM"];

            Throws<InvalidOperationException>(() => AutoRoomLifecycle.SyncFamilyDefaults(project, room, target));

            Equal(beforeFamilyId, room.FamilyId, "Dangling previous Family rejection changed Room FamilyId.");
            Equal(beforeHeight, room.Properties["HeightM"], "Dangling previous Family rejection changed inherited Room properties.");
            Equal(beforeOverride, room.Properties["InstanceOverride"], "Dangling previous Family rejection changed instance overrides.");
            Require(room.Properties.Count == 2, "Dangling previous Family rejection changed the Room property set.");
            Require(room.Dirty == beforeDirty, "Dangling previous Family rejection changed Room dirty state.");
            Require(room.UpdatedUtc == beforeRoomUpdatedUtc, "Dangling previous Family rejection changed Room UpdatedUtc.");
            Require(project.ChangeVersion == beforeVersion, "Dangling previous Family rejection changed project ChangeVersion.");
            Require(project.UpdatedUtc == beforeProjectUpdatedUtc, "Dangling previous Family rejection changed project UpdatedUtc.");
            Require(project.Metadata.Count == beforeMetadataCount, "Dangling previous Family rejection changed project metadata count.");
            Equal(beforeSnapshot, project.Metadata["AutoRoomFamilyDefault:R1:HeightM"], "Dangling previous Family rejection changed Family-default metadata.");
        }

        private static void EmptyPreviousFamilyStillBootstraps()
        {
            var project = new ProjectState("auto-room-empty-family", "Auto Room empty Family");
            var target = new ProjectFamily("ROOM-TARGET", "Room Target", ElementCategory.Room);
            target.Properties["HeightM"] = "3.6";
            project.Families.Add(target);
            var room = new ProjectElement("R1", ElementCategory.Room);
            project.Elements.Add(room);
            room.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;

            var changed = AutoRoomLifecycle.SyncFamilyDefaults(project, room, target);

            Require(changed > 0, "Empty previous Family bootstrap did not report a semantic change.");
            Equal(target.Id, room.FamilyId, "Empty previous Family bootstrap did not assign the target Family.");
            Equal("3.6", room.Properties["HeightM"], "Empty previous Family bootstrap did not apply target defaults.");
            Equal("3.6", project.Metadata["AutoRoomFamilyDefault:R1:HeightM"], "Empty previous Family bootstrap did not persist the target default snapshot.");
            Require(project.ChangeVersion == checked(beforeVersion + 1L), "Empty previous Family bootstrap did not touch the project exactly once.");
        }

        private static void CanonicalSameFamilyRemainsNoOp()
        {
            var project = new ProjectState("auto-room-same-family", "Auto Room same Family");
            var target = new ProjectFamily("ROOM-TARGET", "Room Target", ElementCategory.Room);
            target.Properties["HeightM"] = "3.6";
            project.Families.Add(target);
            var room = new ProjectElement("R1", ElementCategory.Room, " room-target ", string.Empty, string.Empty);
            room.Properties["HeightM"] = "3.6";
            room.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(room);
            project.Metadata["AutoRoomFamilyDefault:R1:HeightM"] = "3.6";
            var beforeVersion = project.ChangeVersion;
            var beforeProjectUpdatedUtc = project.UpdatedUtc;
            var beforeRoomUpdatedUtc = room.UpdatedUtc;
            var beforeFamilyId = room.FamilyId;

            var changed = AutoRoomLifecycle.SyncFamilyDefaults(project, room, target);

            Require(changed == 0, "Canonical same-Family synchronization reported a false semantic change.");
            Equal(beforeFamilyId, room.FamilyId, "Canonical same-Family synchronization rewrote persisted FamilyId formatting.");
            Require(project.ChangeVersion == beforeVersion, "Canonical same-Family synchronization changed project ChangeVersion.");
            Require(project.UpdatedUtc == beforeProjectUpdatedUtc, "Canonical same-Family synchronization changed project UpdatedUtc.");
            Require(room.UpdatedUtc == beforeRoomUpdatedUtc, "Canonical same-Family synchronization changed Room UpdatedUtc.");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception(message + " Expected='" + expected + "', actual='" + actual + "'.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
