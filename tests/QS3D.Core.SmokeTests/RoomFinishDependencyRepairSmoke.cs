using System;
using System.Linq;
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
            finish.DependsOn.Add("room");
            finish.DependsOn.Add(helper.Id);
            project.Elements.Add(finish);

            RoomFinishSynchronizationService.Synchronize(project, room, finish);

            var roomDependencies = finish.DependsOn.Count(x => string.Equals((x ?? string.Empty).Trim(), room.Id, StringComparison.OrdinalIgnoreCase));
            if (roomDependencies != 1) throw new Exception("Room finish synchronization must keep exactly one canonical Room dependency.");
            if (!finish.DependsOn.Any(x => string.Equals(x, helper.Id, StringComparison.Ordinal)))
                throw new Exception("Room finish synchronization must preserve non-Room dependencies.");
        }
    }
}
