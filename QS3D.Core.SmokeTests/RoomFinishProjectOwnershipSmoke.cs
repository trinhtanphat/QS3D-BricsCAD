using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishProjectOwnershipSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-finish-owner", "Finish ownership");
            var ownedRoom = new ProjectElement("ROOM-1", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(ownedRoom);

            var finish = new ProjectElement(
                RoomFinishIdentityService.CanonicalId(ownedRoom.Id, ElementCategory.FloorFinish),
                ElementCategory.FloorFinish,
                string.Empty,
                string.Empty,
                string.Empty);
            finish.Properties[AutoRoomLifecycle.RoomSourceIdKey] = ownedRoom.Id;
            project.Elements.Add(finish);

            var found = RoomFinishIdentityService.FindExisting(project, ownedRoom, ElementCategory.FloorFinish);
            if (!ReferenceEquals(found, finish))
                throw new InvalidOperationException("RoomFinishProjectOwnershipSmoke: owned room did not resolve its canonical finish.");

            var foreignRoom = new ProjectElement("ROOM-1", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            Throws<InvalidOperationException>(() => RoomFinishIdentityService.FindExisting(project, foreignRoom, ElementCategory.FloorFinish));

            var missingRoom = new ProjectElement("ROOM-MISSING", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            Throws<InvalidOperationException>(() => RoomFinishIdentityService.FindExisting(project, missingRoom, ElementCategory.FloorFinish));
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("RoomFinishProjectOwnershipSmoke expected " + typeof(T).Name + ".");
        }
    }
}
