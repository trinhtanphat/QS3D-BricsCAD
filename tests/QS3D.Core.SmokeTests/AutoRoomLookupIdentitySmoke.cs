using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomLookupIdentitySmoke
    {
        public static void Run()
        {
            RejectsNullEntryBeforeLookupResult();
            RejectsDuplicateIdsBeforeLookupResult();
            CanonicalRoomReferenceResolvesCaseInsensitively();
            RejectsPaddedRoomReferenceProperty();
            RejectsPaddedDependencyRoomReference();
        }

        private static void RejectsNullEntryBeforeLookupResult()
        {
            var project = Project();
            var room = AutoRoom("ROOM-A", "A;B");
            project.Elements.Add(room);
            project.Elements.Add(null!);
            var version = project.ChangeVersion;

            Throws<InvalidOperationException>(() => AutoRoomLifecycle.FindBySourceSignature(project, "B;A", "F", "Z"));

            Equal(version, project.ChangeVersion);
            Equal(AutoRoomLifecycle.BoundaryStateActive, room.Properties[AutoRoomLifecycle.BoundaryStateKey]);
        }

        private static void RejectsDuplicateIdsBeforeLookupResult()
        {
            var project = Project();
            var first = AutoRoom("ROOM-A", "A;B");
            var duplicate = AutoRoom("room-a", "X;Y");
            project.Elements.Add(first);
            project.Elements.Add(duplicate);
            var version = project.ChangeVersion;

            Throws<InvalidOperationException>(() => AutoRoomLifecycle.FindBySourceSignature(project, "A;B", "F", "Z"));

            Equal(version, project.ChangeVersion);
            Equal(AutoRoomLifecycle.BoundaryStateActive, first.Properties[AutoRoomLifecycle.BoundaryStateKey]);
            Equal(AutoRoomLifecycle.BoundaryStateActive, duplicate.Properties[AutoRoomLifecycle.BoundaryStateKey]);
        }

        private static void CanonicalRoomReferenceResolvesCaseInsensitively()
        {
            var project = Project();
            var room = AutoRoom("ROOM-A", "A;B");
            var finish = Finish("FINISH-CANONICAL");
            finish.Properties[AutoRoomLifecycle.RoomSourceIdKey] = "room-a";
            finish.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(room);
            project.Elements.Add(finish);
            var version = project.ChangeVersion;

            var excluded = AutoRoomLifecycle.IsExcludedFromQuantity(project, finish);

            Equal(false, excluded);
            Equal(version, project.ChangeVersion);
            Equal(ElementDirtyFlags.None, finish.Dirty);
            Equal("room-a", finish.Properties[AutoRoomLifecycle.RoomSourceIdKey]);
        }

        private static void RejectsPaddedRoomReferenceProperty()
        {
            var project = Project();
            var room = AutoRoom("ROOM-A", "A;B");
            var finish = Finish("FINISH-PROPERTY-PAD");
            finish.Properties[AutoRoomLifecycle.RoomSourceIdKey] = " ROOM-A ";
            finish.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(room);
            project.Elements.Add(finish);
            var version = project.ChangeVersion;

            Throws<InvalidOperationException>(() => AutoRoomLifecycle.IsExcludedFromQuantity(project, finish));

            Equal(version, project.ChangeVersion);
            Equal(ElementDirtyFlags.None, finish.Dirty);
            Equal(" ROOM-A ", finish.Properties[AutoRoomLifecycle.RoomSourceIdKey]);
        }

        private static void RejectsPaddedDependencyRoomReference()
        {
            var project = Project();
            var room = AutoRoom("ROOM-A", "A;B");
            var finish = Finish("FINISH-DEPENDENCY-PAD");
            finish.DependsOn.Add("\tROOM-A ");
            finish.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(room);
            project.Elements.Add(finish);
            var version = project.ChangeVersion;

            Throws<InvalidOperationException>(() => AutoRoomLifecycle.IsExcludedFromQuantity(project, finish));

            Equal(version, project.ChangeVersion);
            Equal(ElementDirtyFlags.None, finish.Dirty);
            Equal("\tROOM-A ", finish.DependsOn[0]);
        }

        private static ProjectState Project()
        {
            return new ProjectState("AUTO-LOOKUP", "Auto room lookup");
        }

        private static ProjectElement AutoRoom(string id, string handles)
        {
            var room = new ProjectElement(id, ElementCategory.Room, string.Empty, "F", "Z");
            room.Properties[AutoRoomLifecycle.BoundaryModeKey] = AutoRoomLifecycle.BoundaryModeAutoNetwork;
            room.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = handles;
            room.Properties[AutoRoomLifecycle.BoundarySourceSignatureKey] = AutoRoomLifecycle.NormalizeSourceHandles(handles.Split(';'));
            room.Properties[AutoRoomLifecycle.BoundaryStateKey] = AutoRoomLifecycle.BoundaryStateActive;
            return room;
        }

        private static ProjectElement Finish(string id)
        {
            return new ProjectElement(id, ElementCategory.FloorFinish, string.Empty, "F", "Z");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
