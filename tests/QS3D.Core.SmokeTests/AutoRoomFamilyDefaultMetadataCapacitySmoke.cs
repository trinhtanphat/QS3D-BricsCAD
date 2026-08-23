using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomFamilyDefaultMetadataCapacitySmoke
    {
        private const int MaximumMetadataEntries = 10000;
        private const string SnapshotPrefix = "AutoRoomFamilyDefault:";

        [ModuleInitializer]
        internal static void Run()
        {
            RejectsOverCapacityBeforeAnyMutation();
            StaleSnapshotRemovalCanFreeCapacityForReplacement();
        }

        private static void RejectsOverCapacityBeforeAnyMutation()
        {
            var project = new ProjectState("autoroom-capacity-reject", "AutoRoom capacity reject");
            var family = new ProjectFamily("room-family-capacity-reject", "Room family", ElementCategory.Room);
            family.Properties["FinishCode"] = "FINISH-A";
            var room = new ProjectElement("room-capacity-reject", ElementCategory.Room);
            room.MarkClean(ElementDirtyFlags.All);
            project.Families.Add(family);
            project.Elements.Add(room);
            FillMetadata(project, MaximumMetadataEntries, "capacity-reject-");

            var versionBefore = project.ChangeVersion;
            var dirtyBefore = room.Dirty;
            var familyIdBefore = room.FamilyId;
            var metadataCountBefore = project.Metadata.Count;
            var snapshotKey = SnapshotPrefix + room.Id + ":FinishCode";

            ExpectThrows<InvalidOperationException>(() => AutoRoomLifecycle.SyncFamilyDefaults(project, room, family));

            Equal(versionBefore, project.ChangeVersion, "Rejected AutoRoom sync changed project revision.");
            Equal(dirtyBefore, room.Dirty, "Rejected AutoRoom sync changed Room dirty state.");
            Equal(familyIdBefore, room.FamilyId, "Rejected AutoRoom sync changed Room family relation.");
            Equal(metadataCountBefore, project.Metadata.Count, "Rejected AutoRoom sync changed metadata cardinality.");
            False(room.Properties.ContainsKey("FinishCode"), "Rejected AutoRoom sync changed Room properties.");
            False(project.Metadata.ContainsKey(snapshotKey), "Rejected AutoRoom sync persisted a family-default snapshot.");
        }

        private static void StaleSnapshotRemovalCanFreeCapacityForReplacement()
        {
            var project = new ProjectState("autoroom-capacity-replace", "AutoRoom capacity replace");
            var family = new ProjectFamily("room-family-capacity-replace", "Room family", ElementCategory.Room);
            family.Properties["NewFinish"] = "NEW";
            var room = new ProjectElement("room-capacity-replace", ElementCategory.Room, family.Id, string.Empty, string.Empty);
            room.Properties["OldFinish"] = "OLD";
            room.MarkClean(ElementDirtyFlags.All);
            project.Families.Add(family);
            project.Elements.Add(room);

            FillMetadata(project, MaximumMetadataEntries - 1, "capacity-replace-");
            var oldSnapshotKey = SnapshotPrefix + room.Id + ":OldFinish";
            var newSnapshotKey = SnapshotPrefix + room.Id + ":NewFinish";
            project.Metadata.Add(oldSnapshotKey, "OLD");
            Equal(MaximumMetadataEntries, project.Metadata.Count, "Replacement fixture did not reach metadata capacity.");

            var versionBefore = project.ChangeVersion;
            var changed = AutoRoomLifecycle.SyncFamilyDefaults(project, room, family);

            True(changed > 0, "Capacity-neutral AutoRoom replacement reported no change.");
            Equal(versionBefore + 1L, project.ChangeVersion, "Capacity-neutral AutoRoom replacement did not touch exactly once.");
            Equal(MaximumMetadataEntries, project.Metadata.Count, "Capacity-neutral AutoRoom replacement changed final metadata cardinality.");
            False(project.Metadata.ContainsKey(oldSnapshotKey), "Stale family-default snapshot was not removed.");
            True(project.Metadata.TryGetValue(newSnapshotKey, out var newSnapshot) && string.Equals(newSnapshot, "NEW", StringComparison.Ordinal),
                "Replacement family-default snapshot was not persisted.");
            False(room.Properties.ContainsKey("OldFinish"), "Inherited stale Room default was not removed.");
            True(room.Properties.TryGetValue("NewFinish", out var newValue) && string.Equals(newValue, "NEW", StringComparison.Ordinal),
                "New inherited Room default was not applied.");
        }

        private static void FillMetadata(ProjectState project, int count, string prefix)
        {
            for (var i = 0; i < count; i++)
                project.Metadata.Add("Smoke." + prefix + i.ToString("D5"), "v");
        }

        private static void ExpectThrows<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void False(bool value, string message) => True(!value, message);

        private static void Equal(long expected, long actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(ElementDirtyFlags expected, ElementDirtyFlags actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(message + " Expected='" + expected + "', actual='" + actual + "'.");
        }
    }
}
