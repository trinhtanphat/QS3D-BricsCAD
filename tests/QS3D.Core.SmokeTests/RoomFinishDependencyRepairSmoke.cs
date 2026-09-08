using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishDependencyRepairSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => KeepsExactlyOneRoomDependency();

        private static void KeepsExactlyOneRoomDependency()
        {
            var project = new ProjectState("dependency-repair", "Room finish dependency repair");
            project.Families.Add(new ProjectFamily("room", "Phòng", ElementCategory.Room));
            var room = new ProjectElement("ROOM", ElementCategory.Room, "room", "f", "z");
            room.Properties["AreaM2"] = "10";
            project.Elements.Add(room);

            var helper = new ProjectElement("HELPER", ElementCategory.CustomQuantity, string.Empty, "f", "z");
            project.Elements.Add(helper);

            var finish = new ProjectElement("FINISH", ElementCategory.FloorFinish, string.Empty, "f", "z");
            finish.Properties[AutoRoomLifecycle.RoomSourceIdKey] = room.Id;
            finish.DependsOn.Add("ROOM");
            SeedPersistedDependency(finish, "room");
            finish.DependsOn.Add(helper.Id);
            project.Elements.Add(finish);

            RoomFinishSynchronizationService.Synchronize(project, room, finish);

            var roomDependencies = finish.DependsOn.Count(x => string.Equals((x ?? string.Empty).Trim(), room.Id, StringComparison.OrdinalIgnoreCase));
            if (roomDependencies != 1) throw new Exception("Room finish synchronization must keep exactly one canonical Room dependency.");
            if (!finish.DependsOn.Any(x => string.Equals(x, helper.Id, StringComparison.Ordinal)))
                throw new Exception("Room finish synchronization must preserve non-Room dependencies.");
        }

        private static void SeedPersistedDependency(ProjectElement element, string value)
        {
            var valuesField = element.DependsOn.GetType().GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to seed duplicate persisted room dependency state.");
            var values = valuesField.GetValue(element.DependsOn) as List<string>
                ?? throw new InvalidOperationException("Unexpected ProjectElement.DependsOn backing collection.");
            values.Add(value);
        }
    }
}
