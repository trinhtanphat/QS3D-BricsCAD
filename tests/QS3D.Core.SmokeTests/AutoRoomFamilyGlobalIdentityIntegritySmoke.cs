using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomFamilyGlobalIdentityIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsUnrelatedDuplicateElementsBeforeMutation();
            RejectsUnrelatedDuplicateFamiliesBeforeMutation();
            AllowsValidBootstrapSynchronization();
        }

        private static void RejectsUnrelatedDuplicateElementsBeforeMutation()
        {
            var project = new ProjectState("P-AUTOROOM-ELEMENT-DUP", "Auto Room element identity smoke");
            project.Elements.Add(new ProjectElement("DUP", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("dup", ElementCategory.Column));
            var room = new ProjectElement("ROOM", ElementCategory.Room);
            project.Elements.Add(room);
            var family = new ProjectFamily("ROOM-FAM", "Room Family", ElementCategory.Room);
            family.Properties["FinishCode"] = "A";
            project.Families.Add(family);

            AssertRejectedWithoutMutation(project, room, family, "duplicate element identity");
        }

        private static void RejectsUnrelatedDuplicateFamiliesBeforeMutation()
        {
            var project = new ProjectState("P-AUTOROOM-FAMILY-DUP", "Auto Room family identity smoke");
            var room = new ProjectElement("ROOM", ElementCategory.Room);
            project.Elements.Add(room);
            project.Families.Add(new ProjectFamily("DUP-FAM", "Duplicate A", ElementCategory.Beam));
            project.Families.Add(new ProjectFamily("dup-fam", "Duplicate B", ElementCategory.Column));
            var family = new ProjectFamily("ROOM-FAM", "Room Family", ElementCategory.Room);
            family.Properties["FinishCode"] = "A";
            project.Families.Add(family);

            AssertRejectedWithoutMutation(project, room, family, "duplicate Family identity");
        }

        private static void AssertRejectedWithoutMutation(ProjectState project, ProjectElement room, ProjectFamily family, string label)
        {
            var beforeFamilyId = room.FamilyId;
            var beforePropertyCount = room.Properties.Count;
            var beforeDirty = room.Dirty;
            var beforeRoomUpdated = room.UpdatedUtc;
            var beforeMetadataCount = project.Metadata.Count;
            var beforeVersion = project.ChangeVersion;
            var beforeProjectUpdated = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => AutoRoomLifecycle.SyncFamilyDefaults(project, room, family), label);

            Equal(beforeFamilyId, room.FamilyId, label + " FamilyId");
            Equal(beforePropertyCount, room.Properties.Count, label + " property count");
            Equal(beforeDirty, room.Dirty, label + " dirty flags");
            Equal(beforeRoomUpdated, room.UpdatedUtc, label + " room timestamp");
            Equal(beforeMetadataCount, project.Metadata.Count, label + " metadata count");
            Equal(beforeVersion, project.ChangeVersion, label + " project version");
            Equal(beforeProjectUpdated, project.UpdatedUtc, label + " project timestamp");
        }

        private static void AllowsValidBootstrapSynchronization()
        {
            var project = new ProjectState("P-AUTOROOM-VALID", "Auto Room valid smoke");
            var room = new ProjectElement("ROOM", ElementCategory.Room);
            project.Elements.Add(room);
            var family = new ProjectFamily("ROOM-FAM", "Room Family", ElementCategory.Room);
            family.Properties["FinishCode"] = "A";
            project.Families.Add(family);
            var beforeVersion = project.ChangeVersion;

            var changed = AutoRoomLifecycle.SyncFamilyDefaults(project, room, family);

            Equal(2, changed, "valid changed count");
            Equal("ROOM-FAM", room.FamilyId, "valid FamilyId");
            Equal("A", room.Properties["FinishCode"], "valid inherited property");
            Equal("A", project.Metadata["AutoRoomFamilyDefault:ROOM:FinishCode"], "valid default snapshot");
            Equal(beforeVersion + 1L, project.ChangeVersion, "valid project revision");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("AutoRoomFamilyGlobalIdentityIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("AutoRoomFamilyGlobalIdentityIntegritySmoke " + label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
