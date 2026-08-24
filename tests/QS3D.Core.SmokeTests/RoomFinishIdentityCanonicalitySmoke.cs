using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishIdentityCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CanonicalBuilderPreservesValidIdentity();
            CanonicalBuilderRejectsSurroundingWhitespaceAndControls();
            ProjectValidationRejectsNonCanonicalElementIdentityWithoutMutation();
            FindExistingPreservesCanonicalCaseInsensitiveLookup();
        }

        private static void CanonicalBuilderPreservesValidIdentity()
        {
            Equal(
                "ROOM-1-FloorFinish",
                RoomFinishIdentityService.CanonicalId("ROOM-1", ElementCategory.FloorFinish),
                "Canonical room finish identity must remain deterministic.");
        }

        private static void CanonicalBuilderRejectsSurroundingWhitespaceAndControls()
        {
            foreach (var roomId in new[]
            {
                " ROOM-1",
                "ROOM-1 ",
                "\tROOM-1",
                "ROOM-1\t",
                "\rROOM-1",
                "ROOM-1\r",
                "\nROOM-1",
                "ROOM-1\n",
                "ROOM\t1",
                "ROOM\n1"
            })
            {
                ExpectArgument(() => RoomFinishIdentityService.CanonicalId(roomId, ElementCategory.FloorFinish));
            }
        }

        private static void ProjectValidationRejectsNonCanonicalElementIdentityWithoutMutation()
        {
            var project = new ProjectState("P1", "Project");
            var room = new ProjectElement("ROOM-1", ElementCategory.Room);
            project.Elements.Add(room);
            var version = project.ChangeVersion;

            SetLegacyElementId(room, " ROOM-1 ");

            ExpectInvalidOperation(() => RoomFinishIdentityService.ValidateProject(project));
            Equal(version, project.ChangeVersion, "Room-finish identity validation must remain read-only on malformed state.");
            Equal(" ROOM-1 ", room.Id, "Rejected project identity must not be silently repaired.");
        }

        private static void FindExistingPreservesCanonicalCaseInsensitiveLookup()
        {
            var project = new ProjectState("P1", "Project");
            var room = new ProjectElement("ROOM-1", ElementCategory.Room);
            var finish = new ProjectElement("room-1-FloorFinish", ElementCategory.FloorFinish);
            finish.Properties[AutoRoomLifecycle.RoomSourceIdKey] = "ROOM-1";
            project.Elements.Add(room);
            project.Elements.Add(finish);
            var version = project.ChangeVersion;

            var found = RoomFinishIdentityService.FindExisting(project, room, ElementCategory.FloorFinish);

            if (!ReferenceEquals(finish, found))
                throw new InvalidOperationException("Canonical case-insensitive room-finish lookup must remain supported.");
            Equal(version, project.ChangeVersion, "Room-finish lookup must remain read-only.");
        }

        private static void SetLegacyElementId(ProjectElement element, string id)
        {
            var field = typeof(ProjectElement).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new InvalidOperationException("ProjectElement Id backing field was not found for corrupt-state regression setup.");
            field.SetValue(element, id);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }

        private static void ExpectArgument(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("Expected malformed room identity to fail closed.");
        }

        private static void ExpectInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("Expected malformed project semantic identity to fail closed.");
        }
    }
}
