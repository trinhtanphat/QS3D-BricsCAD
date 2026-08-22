using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomPreviousFamilyCategoryIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsMismatchedPreviousFamilyWithoutMutation();
            AllowsRoomToRoomSynchronization();
        }

        private static void RejectsMismatchedPreviousFamilyWithoutMutation()
        {
            var project = new ProjectState("P-AUTOROOM-PREV-CATEGORY", "Auto Room previous Family category integrity");
            var wrongPrevious = new ProjectFamily("F-BEAM-PREV", "Wrong beam previous Family", ElementCategory.Beam);
            wrongPrevious.Properties["LegacyInherited"] = "wrong-default";
            var target = new ProjectFamily("F-ROOM-TARGET", "Target Room Family", ElementCategory.Room);
            target.Properties["FinishCode"] = "R1";
            project.Families.Add(wrongPrevious);
            project.Families.Add(target);

            var room = new ProjectElement("ROOM-1", ElementCategory.Room, wrongPrevious.Id, "F1", "Z1");
            room.Properties["LegacyInherited"] = "wrong-default";
            room.Properties["KeepInstance"] = "keep";
            room.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(room);
            project.Metadata["Sentinel"] = "keep";

            var beforeVersion = project.ChangeVersion;
            var beforeProjectUpdated = project.UpdatedUtc;
            var beforeRoomUpdated = room.UpdatedUtc;
            var beforeFamilyId = room.FamilyId;
            var beforeDirty = room.Dirty;
            var beforePropertyCount = room.Properties.Count;
            var beforeMetadataCount = project.Metadata.Count;

            ThrowsContaining<InvalidOperationException>(
                () => AutoRoomLifecycle.SyncFamilyDefaults(project, room, target),
                "references previous Family 'F-BEAM-PREV' category Beam while the room category is Room");

            Equal(beforeVersion, project.ChangeVersion, "rejected project version");
            Equal(beforeProjectUpdated, project.UpdatedUtc, "rejected project timestamp");
            Equal(beforeRoomUpdated, room.UpdatedUtc, "rejected room timestamp");
            Equal(beforeFamilyId, room.FamilyId, "rejected FamilyId");
            Equal(beforeDirty, room.Dirty, "rejected dirty flags");
            Equal(beforePropertyCount, room.Properties.Count, "rejected property count");
            Equal("wrong-default", room.Properties["LegacyInherited"], "rejected legacy property");
            Equal("keep", room.Properties["KeepInstance"], "rejected instance property");
            False(room.Properties.ContainsKey("FinishCode"), "rejected target default");
            Equal(beforeMetadataCount, project.Metadata.Count, "rejected metadata count");
            Equal("keep", project.Metadata["Sentinel"], "rejected metadata value");
        }

        private static void AllowsRoomToRoomSynchronization()
        {
            var project = new ProjectState("P-AUTOROOM-PREV-CATEGORY-VALID", "Auto Room valid previous Family category");
            var previous = new ProjectFamily("F-ROOM-PREV", "Previous Room Family", ElementCategory.Room);
            previous.Properties["FinishCode"] = "OLD";
            previous.Properties["LegacyInherited"] = "remove-me";
            var target = new ProjectFamily("F-ROOM-NEXT", "Next Room Family", ElementCategory.Room);
            target.Properties["FinishCode"] = "NEW";
            project.Families.Add(previous);
            project.Families.Add(target);

            var room = new ProjectElement("ROOM-2", ElementCategory.Room, previous.Id, "F1", "Z1");
            room.Properties["FinishCode"] = "OLD";
            room.Properties["LegacyInherited"] = "remove-me";
            room.Properties["KeepInstance"] = "keep";
            room.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(room);
            var beforeVersion = project.ChangeVersion;

            var changed = AutoRoomLifecycle.SyncFamilyDefaults(project, room, target);

            True(changed > 0, "valid changed count");
            Equal(target.Id, room.FamilyId, "valid FamilyId");
            Equal("NEW", room.Properties["FinishCode"], "valid inherited default");
            False(room.Properties.ContainsKey("LegacyInherited"), "valid obsolete inherited default");
            Equal("keep", room.Properties["KeepInstance"], "valid instance override");
            Equal(beforeVersion + 1L, project.ChangeVersion, "valid project version");
        }

        private static void ThrowsContaining<TException>(Action action, string expected) where TException : Exception
        {
            try { action(); }
            catch (TException ex)
            {
                if (ex.Message.IndexOf(expected, StringComparison.Ordinal) >= 0) return;
                throw new Exception("Expected message containing '" + expected + "', got '" + ex.Message + "'.");
            }
            throw new Exception("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new Exception(label + ": expected true.");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception(label + ": expected false.");
        }
    }
}
