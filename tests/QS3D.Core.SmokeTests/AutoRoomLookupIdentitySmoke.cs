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
