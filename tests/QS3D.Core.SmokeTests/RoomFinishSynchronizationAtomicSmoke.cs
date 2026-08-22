using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishSynchronizationAtomicSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => SingleFailureRollsBackPartialMutation();

        private static void SingleFailureRollsBackPartialMutation()
        {
            var project = new ProjectState("single-sync", "Single finish atomicity");
            project.Floors.Add(new FloorDefinition("f1", "Tầng 1", 0d));
            project.Floors.Add(new FloorDefinition("f2", "Tầng 2", 3.6d));
            project.Zones.Add(new ZoneDefinition("z1", "Zone 1"));
            project.Zones.Add(new ZoneDefinition("z2", "Zone 2"));
            project.Families.Add(new ProjectFamily("room", "Phòng", ElementCategory.Room));

            var room = new ProjectElement("ROOM", ElementCategory.Room, "room", "f2", "z2")
            {
                DrawingFingerprint = "NEW-FP"
            };
            room.Properties["AreaM2"] = "20";
            room.Properties["PerimeterM"] = "invalid-after-area";
            project.Elements.Add(room);

            var finish = new ProjectElement("FINISH", ElementCategory.WallFinish, string.Empty, "f1", "z1")
            {
                DrawingFingerprint = "OLD-FP"
            };
            finish.Properties["ParentRoomId"] = room.Id;
            finish.Properties["AreaM2"] = "9";
            project.Elements.Add(finish);

            try
            {
                RoomFinishSynchronizationService.Synchronize(project, room, finish);
                throw new Exception("Expected invalid Room metric to fail synchronization.");
            }
            catch (InvalidOperationException)
            {
            }

            var restored = project.FindElement("FINISH") ?? throw new Exception("Atomic rollback lost the finish.");
            if (restored.FloorId != "f1" || restored.ZoneId != "z1" || restored.DrawingFingerprint != "OLD-FP")
                throw new Exception("Single finish synchronization did not restore scope/fingerprint after failure.");
            if (restored.Properties.ContainsKey(AutoRoomLifecycle.RoomSourceIdKey))
                throw new Exception("Single finish synchronization leaked canonical Room provenance after failure.");
            if (!restored.Properties.TryGetValue("ParentRoomId", out var parent) || parent != "ROOM")
                throw new Exception("Single finish synchronization did not restore legacy provenance after failure.");
            if (!restored.Properties.TryGetValue("AreaM2", out var area) || area != "9")
                throw new Exception("Single finish synchronization leaked a partially-copied Room metric after failure.");
            if (restored.DependsOn.Count != 0)
                throw new Exception("Single finish synchronization leaked a dependency after failure.");
        }
    }
}
