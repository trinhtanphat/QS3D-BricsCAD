using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class AutomaticRoomLifecycleSmoke
    {
        public static void Run()
        {
            StableIdentityUsesSourceHandles();
            LegacySourceSignatureIsRecovered();
            GeneratedFinishesAreRemovedWithStaleRoom();
            ProtectedDependentsRetainStaleRoom();
            UnselectedAndCurrentRoomsAreUntouched();
        }

        private static void StableIdentityUsesSourceHandles()
        {
            var a = AutomaticRoomLifecycleService.NormalizeSourceSignature(new[] { "b2", " A1 ", "a1" });
            var b = AutomaticRoomLifecycleService.NormalizeSourceSignature(new[] { "A1", "B2" });
            Equal("A1|B2", a);
            Equal(a, b);
            var first = AutomaticRoomLifecycleService.BuildStableElementId(a, "geometry-v1", false);
            var moved = AutomaticRoomLifecycleService.BuildStableElementId(b, "geometry-v2", false);
            Equal(first, moved);
            True(first.StartsWith("ROOMAUTO-", StringComparison.Ordinal));
            True(AutomaticRoomLifecycleService.BuildStableElementId(a, "geometry-v1", true) != AutomaticRoomLifecycleService.BuildStableElementId(a, "geometry-v2", true));
        }

        private static void LegacySourceSignatureIsRecovered()
        {
            var room = new ProjectElement("ROOMAUTO-LEGACY", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            room.Properties["BoundaryMode"] = "AutoNetwork";
            room.Properties["BoundarySourceHandles"] = "0a;0B;0a";
            True(AutomaticRoomLifecycleService.IsManaged(room));
            Equal("0A|0B", AutomaticRoomLifecycleService.GetSourceSignature(room));
            room.SourceHandles.Add("0C");
            Equal("0C", AutomaticRoomLifecycleService.GetSourceSignature(room));
        }

        private static void GeneratedFinishesAreRemovedWithStaleRoom()
        {
            var project = Project();
            var room = AutoRoom("ROOM-OLD", "H1");
            var floor = new ProjectElement("FINISH-FLOOR", ElementCategory.FloorFinish, string.Empty, "F", "Z");
            floor.DependsOn.Add(room.Id);
            var skirting = new ProjectElement("FINISH-SKIRT", ElementCategory.Skirting, string.Empty, "F", "Z");
            skirting.DependsOn.Add(floor.Id);
            project.Elements.Add(room); project.Elements.Add(floor); project.Elements.Add(skirting);

            var result = AutomaticRoomLifecycleService.ReconcileStale(project, Array.Empty<string>(), new[] { "h1" });
            Equal(1, result.RemovedRoomIds.Count);
            Equal(2, result.RemovedDependentIds.Count);
            True(project.FindElement(room.Id) == null);
            True(project.FindElement(floor.Id) == null);
            True(project.FindElement(skirting.Id) == null);
        }

        private static void ProtectedDependentsRetainStaleRoom()
        {
            var project = Project();
            var room = AutoRoom("ROOM-PROTECTED", "H2");
            var finish = new ProjectElement("FINISH", ElementCategory.WallFinish, string.Empty, "F", "Z");
            finish.DependsOn.Add(room.Id);
            var protectedElement = new ProjectElement("MANUAL", ElementCategory.CustomQuantity, string.Empty, "F", "Z");
            protectedElement.DependsOn.Add(finish.Id);
            project.Elements.Add(room); project.Elements.Add(finish); project.Elements.Add(protectedElement);

            var result = AutomaticRoomLifecycleService.ReconcileStale(project, Array.Empty<string>(), new[] { "H2" });
            Equal(1, result.RetainedStaleRoomIds.Count);
            Equal(0, result.RemovedRoomIds.Count);
            True(project.FindElement(room.Id) != null);
            Equal("true", room.Properties["AutoBoundaryStale"]);
            True(project.FindElement(finish.Id) != null);
            True(project.FindElement(protectedElement.Id) != null);
        }

        private static void UnselectedAndCurrentRoomsAreUntouched()
        {
            var project = Project();
            var current = AutoRoom("ROOM-CURRENT", "H3");
            var unrelated = AutoRoom("ROOM-UNRELATED", "H4");
            project.Elements.Add(current); project.Elements.Add(unrelated);
            var result = AutomaticRoomLifecycleService.ReconcileStale(project, new[] { current.Id }, new[] { "H3" });
            Equal(0, result.RemovedRoomIds.Count);
            Equal(0, result.RetainedStaleRoomIds.Count);
            True(project.FindElement(current.Id) != null);
            True(project.FindElement(unrelated.Id) != null);
        }

        private static ProjectElement AutoRoom(string id, string handle)
        {
            var room = new ProjectElement(id, ElementCategory.Room, string.Empty, "F", "Z");
            room.Properties["BoundaryMode"] = "AutoNetwork";
            room.SourceHandles.Add(handle);
            return room;
        }

        private static ProjectState Project()
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "Auto Room Lifecycle");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            project.ActiveZoneId = "Z"; project.ActiveFloorId = "F";
            return project;
        }

        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
    }
}
