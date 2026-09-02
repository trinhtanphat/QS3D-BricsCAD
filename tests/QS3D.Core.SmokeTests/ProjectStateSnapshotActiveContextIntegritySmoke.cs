using System;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateSnapshotActiveContextIntegritySmoke
    {
        internal static void Run()
        {
            RejectsDanglingZoneContextWithoutMutation();
            RejectsDanglingFloorContextWithoutMutation();
            PreservesResolvedContextIdentities();
            PreservesEmptyContextIdentities();
        }

        private static void RejectsDanglingZoneContextWithoutMutation()
        {
            var project = new ProjectState("P-SNAPSHOT-ZONE", "Dangling active zone");
            var zone = new ZoneDefinition("Zone-A", "Zone A");
            project.Zones.Add(zone);
            project.ActiveZoneId = "zone-a";
            project.Zones.Remove(zone);

            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeActiveZoneId = project.ActiveZoneId;

            Throws<InvalidOperationException>(() => ProjectStateSnapshot.Capture(project), "Capture accepted a dangling active zone.");
            Throws<InvalidOperationException>(() => ProjectStateSnapshot.CreateDetachedCopy(project), "CreateDetachedCopy accepted a dangling active zone.");

            Equal(beforeVersion, project.ChangeVersion, "Rejected zone snapshot changed source ChangeVersion.");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "Rejected zone snapshot changed source UpdatedUtc.");
            Equal(beforeActiveZoneId, project.ActiveZoneId, "Rejected zone snapshot changed source ActiveZoneId.");
            Equal(0, project.Zones.Count, "Rejected zone snapshot changed source Zones.");
        }

        private static void RejectsDanglingFloorContextWithoutMutation()
        {
            var project = new ProjectState("P-SNAPSHOT-FLOOR", "Dangling active floor");
            var floor = new FloorDefinition("Floor-A", "Floor A", 3.25d);
            project.Floors.Add(floor);
            project.ActiveFloorId = "floor-a";
            project.Floors.Remove(floor);

            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeActiveFloorId = project.ActiveFloorId;

            Throws<InvalidOperationException>(() => ProjectStateSnapshot.Capture(project), "Capture accepted a dangling active floor.");
            Throws<InvalidOperationException>(() => ProjectStateSnapshot.CreateDetachedCopy(project), "CreateDetachedCopy accepted a dangling active floor.");

            Equal(beforeVersion, project.ChangeVersion, "Rejected floor snapshot changed source ChangeVersion.");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "Rejected floor snapshot changed source UpdatedUtc.");
            Equal(beforeActiveFloorId, project.ActiveFloorId, "Rejected floor snapshot changed source ActiveFloorId.");
            Equal(0, project.Floors.Count, "Rejected floor snapshot changed source Floors.");
        }

        private static void PreservesResolvedContextIdentities()
        {
            var project = new ProjectState("P-SNAPSHOT-VALID", "Resolved active context");
            project.Zones.Add(new ZoneDefinition("Zone-A", "Zone A"));
            project.Floors.Add(new FloorDefinition("Floor-A", "Floor A", 0d));
            project.ActiveZoneId = "zone-a";
            project.ActiveFloorId = "floor-a";

            _ = ProjectStateSnapshot.Capture(project);
            var copy = ProjectStateSnapshot.CreateDetachedCopy(project);

            Equal("zone-a", copy.ActiveZoneId, "Resolved active zone identity changed during detached copy.");
            Equal("floor-a", copy.ActiveFloorId, "Resolved active floor identity changed during detached copy.");
            Equal("Zone-A", copy.Zones[0].Id, "Zone catalog identity changed during detached copy.");
            Equal("Floor-A", copy.Floors[0].Id, "Floor catalog identity changed during detached copy.");
        }

        private static void PreservesEmptyContextIdentities()
        {
            var project = new ProjectState("P-SNAPSHOT-EMPTY", "Empty active context");
            project.Zones.Add(new ZoneDefinition("Zone-A", "Zone A"));
            project.Floors.Add(new FloorDefinition("Floor-A", "Floor A", 0d));

            _ = ProjectStateSnapshot.Capture(project);
            var copy = ProjectStateSnapshot.CreateDetachedCopy(project);

            Equal(string.Empty, copy.ActiveZoneId, "Empty active zone identity changed during detached copy.");
            Equal(string.Empty, copy.ActiveFloorId, "Empty active floor identity changed during detached copy.");
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception(message + " Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
