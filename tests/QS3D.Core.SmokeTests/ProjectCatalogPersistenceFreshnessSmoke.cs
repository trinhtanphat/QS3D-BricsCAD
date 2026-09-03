using System;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectCatalogPersistenceFreshnessSmoke
    {
        public static void Run()
        {
            OwnedCatalogScalarMutationsAdvanceProjectFreshness();
            NormalizedNoOpsDoNotAdvanceProjectFreshness();
            OwnershipTracksRemovalReplacementAndSnapshotRestore();
            DuplicateCatalogReferencesHaveSingleOwnershipSubscription();
            ServiceRenameAdvancesProjectFreshnessExactlyOnce();
            ServiceZoneUpdateAdvancesProjectFreshnessExactlyOnce();
            ServiceFloorUpdateAdvancesProjectFreshnessOncePerLogicalUpdate();
        }

        private static void OwnedCatalogScalarMutationsAdvanceProjectFreshness()
        {
            var project = CreateProject(out var zone, out var floor, out var family);
            var baseline = project.ChangeVersion;

            zone.Name = "Zone B";
            Equal(baseline + 1L, project.ChangeVersion, "zone name");

            floor.Name = "Level 1";
            Equal(baseline + 2L, project.ChangeVersion, "floor name");

            floor.ElevationM = 3.25d;
            Equal(baseline + 3L, project.ChangeVersion, "floor elevation");

            family.Name = "Wall Type B";
            Equal(baseline + 4L, project.ChangeVersion, "family name");

            family.Category = ElementCategory.StructuralWall;
            Equal(baseline + 5L, project.ChangeVersion, "family category");
        }

        private static void NormalizedNoOpsDoNotAdvanceProjectFreshness()
        {
            var project = CreateProject(out var zone, out var floor, out var family);
            var baseline = project.ChangeVersion;

            zone.Name = "  Zone A  ";
            floor.Name = " Floor 0 ";
            floor.ElevationM = -0d;
            family.Name = " Wall Type A ";
            family.Category = ElementCategory.ArchitecturalWall;

            Equal(baseline, project.ChangeVersion, "normalized catalog no-op");
        }

        private static void OwnershipTracksRemovalReplacementAndSnapshotRestore()
        {
            var project = CreateProject(out var zone, out var floor, out var family);
            var snapshot = ProjectStateSnapshot.Capture(project);
            var capturedVersion = project.ChangeVersion;
            var capturedUpdatedUtc = project.UpdatedUtc;

            project.Zones.Remove(zone);
            zone.Name = "Detached zone";
            Equal(capturedVersion, project.ChangeVersion, "removed zone must be detached");

            var replacement = new ZoneDefinition("Z2", "Replacement");
            project.Zones.Add(replacement);
            replacement.Name = "Replacement edited";
            Equal(capturedVersion + 1L, project.ChangeVersion, "newly owned zone must be attached");

            floor.ElevationM = 7d;
            family.Name = "Changed family";
            snapshot.Restore(project);

            Equal(capturedVersion, project.ChangeVersion, "snapshot restore version");
            Equal(capturedUpdatedUtc, project.UpdatedUtc, "snapshot restore timestamp");
            if (!ReferenceEquals(zone, project.Zones[0]))
                throw new Exception("Snapshot restore must preserve the captured Zone object identity.");

            zone.Name = "Zone after restore";
            Equal(capturedVersion + 1L, project.ChangeVersion, "restored zone ownership");

            var detachedCopy = ProjectStateSnapshot.CreateDetachedCopy(project);
            var copyVersion = detachedCopy.ChangeVersion;
            detachedCopy.Floors[0].ElevationM = 9d;
            Equal(copyVersion + 1L, detachedCopy.ChangeVersion, "detached snapshot copy ownership");
            Equal(capturedVersion + 1L, project.ChangeVersion, "detached copy must not mutate source freshness");
        }

        private static void DuplicateCatalogReferencesHaveSingleOwnershipSubscription()
        {
            var project = new ProjectState("P-CATALOG-DUPLICATE", "Catalog duplicate ownership");
            var zone = new ZoneDefinition("Z-DUP", "Duplicate zone");
            project.Zones.Add(zone);
            project.Zones.Add(zone);

            var baseline = project.ChangeVersion;
            zone.Name = "Duplicate zone edited once";
            Equal(baseline + 1L, project.ChangeVersion, "duplicate reference must request one project touch");

            project.Zones.RemoveAt(0);
            zone.Name = "Duplicate zone still owned";
            Equal(baseline + 2L, project.ChangeVersion, "remaining duplicate reference must stay attached once");

            project.Zones.RemoveAt(0);
            zone.Name = "Duplicate zone detached";
            Equal(baseline + 2L, project.ChangeVersion, "last duplicate removal must detach ownership");
        }

        private static void ServiceRenameAdvancesProjectFreshnessExactlyOnce()
        {
            var project = CreateProject(out _, out _, out var family);
            var baseline = project.ChangeVersion;

            var renamed = ProjectFamilyService.Rename(project, family.Id, "Wall Type Renamed");

            if (!ReferenceEquals(family, renamed))
                throw new Exception("ProjectFamilyService.Rename must preserve the owned family object identity.");
            Equal("Wall Type Renamed", family.Name, "service rename family name");
            Equal(baseline + 1L, project.ChangeVersion, "service rename must advance project freshness exactly once");

            ProjectFamilyService.Rename(project, family.Id, " Wall Type Renamed ");
            Equal(baseline + 1L, project.ChangeVersion, "normalized service rename no-op must not advance project freshness");
        }

        private static void ServiceZoneUpdateAdvancesProjectFreshnessExactlyOnce()
        {
            var project = CreateProject(out var zone, out _, out _);
            var baseline = project.ChangeVersion;

            var updated = ProjectZoneService.Update(project, zone.Id, "Zone Updated");

            if (!ReferenceEquals(zone, updated))
                throw new Exception("ProjectZoneService.Update must preserve the owned zone object identity.");
            Equal("Zone Updated", zone.Name, "service zone update name");
            Equal(baseline + 1L, project.ChangeVersion, "service zone update must advance project freshness exactly once");

            ProjectZoneService.Update(project, zone.Id, " Zone Updated ");
            Equal(baseline + 1L, project.ChangeVersion, "normalized service zone update no-op must not advance project freshness");
        }

        private static void ServiceFloorUpdateAdvancesProjectFreshnessOncePerLogicalUpdate()
        {
            var project = CreateProject(out _, out var floor, out _);
            var baseline = project.ChangeVersion;

            var nameOnly = ProjectFloorService.Update(project, floor.Id, "Floor Renamed", floor.ElevationM);
            if (!ReferenceEquals(floor, nameOnly))
                throw new Exception("ProjectFloorService.Update must preserve the owned floor object identity.");
            Equal("Floor Renamed", floor.Name, "floor name-only update");
            Equal(baseline + 1L, project.ChangeVersion, "floor name-only update must advance freshness once");

            ProjectFloorService.Update(project, floor.Id, floor.Name, 4.5d);
            Equal(4.5d, floor.ElevationM, "floor elevation-only update");
            Equal(baseline + 2L, project.ChangeVersion, "floor elevation-only update must advance freshness once");

            ProjectFloorService.Update(project, floor.Id, "Floor Combined", 8.25d);
            Equal("Floor Combined", floor.Name, "floor combined update name");
            Equal(8.25d, floor.ElevationM, "floor combined update elevation");
            Equal(baseline + 3L, project.ChangeVersion, "floor combined update must advance freshness once");

            ProjectFloorService.Update(project, floor.Id, " Floor Combined ", 8.25d);
            Equal(baseline + 3L, project.ChangeVersion, "normalized floor update no-op must not advance freshness");
        }

        private static ProjectState CreateProject(
            out ZoneDefinition zone,
            out FloorDefinition floor,
            out ProjectFamily family)
        {
            var project = new ProjectState("P-CATALOG-FRESHNESS", "Catalog freshness");
            zone = new ZoneDefinition("Z1", "Zone A");
            floor = new FloorDefinition("F1", "Floor 0", 0d);
            family = new ProjectFamily("PF1", "Wall Type A", ElementCategory.ArchitecturalWall);

            var baseline = project.ChangeVersion;
            project.Zones.Add(zone);
            project.Floors.Add(floor);
            project.Families.Add(family);
            Equal(baseline, project.ChangeVersion, "catalog materialization must not advance freshness");
            return project;
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception(label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
