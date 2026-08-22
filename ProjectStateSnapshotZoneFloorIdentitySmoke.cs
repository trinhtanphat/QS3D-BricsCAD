using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateSnapshotZoneFloorIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RestorePreservesCapturedZoneFloorIdentity();
            DetachedCopyNeverAliasesCanonicalZoneFloor();
            ForeignTargetRestoreNeverAliasesCapturedZoneFloor();
        }

        private static void RestorePreservesCapturedZoneFloorIdentity()
        {
            var project = new ProjectState("snapshot-zone-floor-identity", "Snapshot Zone Floor identity");
            var zone1 = new ZoneDefinition("Z1", "Zone One");
            var zone2 = new ZoneDefinition("Z2", "Zone Two");
            var floor1 = new FloorDefinition("F1", "Level One", 1.25d);
            var floor2 = new FloorDefinition("F2", "Level Two", 4.5d);
            project.Zones.Add(zone1);
            project.Zones.Add(zone2);
            project.Floors.Add(floor1);
            project.Floors.Add(floor2);
            project.Touch();

            var projectUpdatedUtc = project.UpdatedUtc;
            var projectChangeVersion = project.ChangeVersion;
            var rollback = ProjectStateSnapshot.Capture(project);

            zone1.Name = "Mutated Zone";
            project.Zones.Remove(zone2);
            project.Zones.Insert(0, new ZoneDefinition("Z3", "Added Zone"));

            floor1.Name = "Mutated Level";
            floor1.ElevationM = 99d;
            project.Floors.Remove(floor2);
            project.Floors.Insert(0, new FloorDefinition("F3", "Added Level", 123d));
            project.Touch();

            rollback.Restore(project);

            Require(project.Zones.Count == 2, "Rollback did not restore captured Zone count.");
            Require(ReferenceEquals(project.Zones[0], zone1), "Rollback replaced the first captured canonical Zone reference.");
            Require(ReferenceEquals(project.Zones[1], zone2), "Rollback did not reinsert the removed captured canonical Zone reference.");
            Require(ReferenceEquals(project.FindZone("Z1"), zone1), "FindZone(Z1) no longer returns the pre-transaction canonical Zone after rollback.");
            Require(ReferenceEquals(project.FindZone("Z2"), zone2), "FindZone(Z2) no longer returns the removed pre-transaction canonical Zone after rollback.");
            Require(project.FindZone("Z3") == null, "Rollback retained a Zone created after snapshot capture.");
            Require(zone1.Name == "Zone One" && zone2.Name == "Zone Two", "Rollback did not restore Zone names in place.");

            Require(project.Floors.Count == 2, "Rollback did not restore captured Floor count.");
            Require(ReferenceEquals(project.Floors[0], floor1), "Rollback replaced the first captured canonical Floor reference.");
            Require(ReferenceEquals(project.Floors[1], floor2), "Rollback did not reinsert the removed captured canonical Floor reference.");
            Require(ReferenceEquals(project.FindFloor("F1"), floor1), "FindFloor(F1) no longer returns the pre-transaction canonical Floor after rollback.");
            Require(ReferenceEquals(project.FindFloor("F2"), floor2), "FindFloor(F2) no longer returns the removed pre-transaction canonical Floor after rollback.");
            Require(project.FindFloor("F3") == null, "Rollback retained a Floor created after snapshot capture.");
            Require(floor1.Name == "Level One" && floor1.ElevationM == 1.25d, "Rollback did not restore first Floor values in place.");
            Require(floor2.Name == "Level Two" && floor2.ElevationM == 4.5d, "Rollback did not restore removed Floor values in place.");

            Require(project.ChangeVersion == projectChangeVersion, "Rollback did not restore project ChangeVersion.");
            Require(project.UpdatedUtc == projectUpdatedUtc, "Rollback did not restore project UpdatedUtc.");
        }

        private static void DetachedCopyNeverAliasesCanonicalZoneFloor()
        {
            var project = new ProjectState("snapshot-zone-floor-detached", "Snapshot detached");
            var zone = new ZoneDefinition("Z1", "Canonical Zone");
            var floor = new FloorDefinition("F1", "Canonical Floor", 2d);
            project.Zones.Add(zone);
            project.Floors.Add(floor);

            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            var detachedZone = detached.FindZone("Z1") ?? throw new Exception("Detached copy lost Z1.");
            var detachedFloor = detached.FindFloor("F1") ?? throw new Exception("Detached copy lost F1.");

            Require(!ReferenceEquals(detachedZone, zone), "CreateDetachedCopy aliased the canonical Zone.");
            Require(!ReferenceEquals(detachedFloor, floor), "CreateDetachedCopy aliased the canonical Floor.");

            detachedZone.Name = "Detached Zone";
            detachedFloor.Name = "Detached Floor";
            detachedFloor.ElevationM = 10d;
            Require(zone.Name == "Canonical Zone", "Mutating detached Zone changed canonical Zone.");
            Require(floor.Name == "Canonical Floor" && floor.ElevationM == 2d, "Mutating detached Floor changed canonical Floor.");
        }

        private static void ForeignTargetRestoreNeverAliasesCapturedZoneFloor()
        {
            var source = new ProjectState("snapshot-zone-floor-foreign", "Source");
            var capturedZone = new ZoneDefinition("Z1", "Source Zone");
            var capturedFloor = new FloorDefinition("F1", "Source Floor", 3d);
            source.Zones.Add(capturedZone);
            source.Floors.Add(capturedFloor);
            var rollback = ProjectStateSnapshot.Capture(source);

            var target = new ProjectState("snapshot-zone-floor-foreign", "Target");
            target.Zones.Add(new ZoneDefinition("OLD-Z", "Old Zone"));
            target.Floors.Add(new FloorDefinition("OLD-F", "Old Floor", 0d));
            rollback.Restore(target);

            var restoredZone = target.FindZone("Z1") ?? throw new Exception("Foreign target restore lost Z1.");
            var restoredFloor = target.FindFloor("F1") ?? throw new Exception("Foreign target restore lost F1.");
            Require(!ReferenceEquals(restoredZone, capturedZone), "Foreign restore aliased the captured canonical Zone.");
            Require(!ReferenceEquals(restoredFloor, capturedFloor), "Foreign restore aliased the captured canonical Floor.");

            restoredZone.Name = "Target Zone";
            restoredFloor.Name = "Target Floor";
            restoredFloor.ElevationM = 20d;
            Require(capturedZone.Name == "Source Zone", "Foreign target Zone mutation changed captured source Zone.");
            Require(capturedFloor.Name == "Source Floor" && capturedFloor.ElevationM == 3d, "Foreign target Floor mutation changed captured source Floor.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}
