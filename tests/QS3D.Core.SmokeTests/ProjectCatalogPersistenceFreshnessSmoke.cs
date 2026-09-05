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
            OwnedFamilyPropertyMutationsAdvanceProjectFreshness();
            NormalizedNoOpsDoNotAdvanceProjectFreshness();
            OwnershipTracksRemovalReplacementAndSnapshotRestore();
            DuplicateCatalogReferencesHaveSingleOwnershipSubscription();
            ServiceRenameAdvancesProjectFreshnessExactlyOnce();
            ServicePropertyMutationsAdvanceProjectFreshnessExactlyOnce();
            ServiceDuplicateAdvancesProjectFreshnessExactlyOnce();
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

        private static void OwnedFamilyPropertyMutationsAdvanceProjectFreshness()
        {
            var project = CreateProject(out _, out _, out var family);
            var baseline = project.ChangeVersion;

            family.Properties.Add("FireRating", "60");
            Equal(baseline + 1L, project.ChangeVersion, "family property add");

            family.Properties["FireRating"] = "60";
            Equal(baseline + 1L, project.ChangeVersion, "family property identical replacement no-op");

            family.Properties["firerating"] = "90";
            Equal(baseline + 2L, project.ChangeVersion, "family property replacement");
            Equal("90", family.Properties["FireRating"], "family property comparer compatibility");

            if (family.Properties.Remove("missing"))
                throw new Exception("Removing a missing family property must report false.");
            Equal(baseline + 2L, project.ChangeVersion, "family property missing remove no-op");

            if (!family.Properties.Remove("FIRERATING"))
                throw new Exception("Removing an existing family property must report true.");
            Equal(baseline + 3L, project.ChangeVersion, "family property remove");

            family.Properties.Clear();
            Equal(baseline + 3L, project.ChangeVersion, "empty family property clear no-op");

            family.Properties.Add("A", "1");
            family.Properties.Add("B", "2");
            Equal(baseline + 5L, project.ChangeVersion, "family property additions before clear");
            family.Properties.Clear();
            Equal(baseline + 6L, project.ChangeVersion, "family property clear must be one logical mutation");

            project.Families.Remove(family);
            family.Properties.Add("Detached", "true");
            Equal(baseline + 7L, project.ChangeVersion, "structural family removal must advance once and detached family property mutation must not touch former owner");
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
            family.Properties["Captured"] = "yes";
            var snapshot = ProjectStateSnapshot.Capture(project);
            var capturedVersion = project.ChangeVersion;
            var capturedUpdatedUtc = project.UpdatedUtc;

            project.Zones.Remove(zone);
            zone.Name = "Detached zone";
            Equal(capturedVersion + 1L, project.ChangeVersion, "removed zone structural mutation must advance once and detached child must stay neutral");

            var replacement = new ZoneDefinition("Z2", "Replacement");
            project.Zones.Add(replacement);
            replacement.Name = "Replacement edited";
            Equal(capturedVersion + 3L, project.ChangeVersion, "newly owned zone add plus child edit must each advance freshness once");

            floor.ElevationM = 7d;
            family.Name = "Changed family";
            family.Properties["Captured"] = "changed";
            snapshot.Restore(project);

            Equal(capturedVersion, project.ChangeVersion, "snapshot restore version");
            Equal(capturedUpdatedUtc, project.UpdatedUtc, "snapshot restore timestamp");
            if (!ReferenceEquals(zone, project.Zones[0]))
                throw new Exception("Snapshot restore must preserve the captured Zone object identity.");
            if (!ReferenceEquals(family, project.Families[0]))
                throw new Exception("Snapshot restore must preserve the captured Family object identity.");
            Equal("yes", family.Properties["Captured"], "snapshot restore family property");

            zone.Name = "Zone after restore";
            Equal(capturedVersion + 1L, project.ChangeVersion, "restored zone ownership");
            family.Properties["Captured"] = "after restore";
            Equal(capturedVersion + 2L, project.ChangeVersion, "restored family property ownership");

            var detachedCopy = ProjectStateSnapshot.CreateDetachedCopy(project);
            var copyVersion = detachedCopy.ChangeVersion;
            detachedCopy.Floors[0].ElevationM = 9d;
            Equal(copyVersion + 1L, detachedCopy.ChangeVersion, "detached snapshot copy ownership");
            detachedCopy.Families[0].Properties["Captured"] = "copy only";
            Equal(copyVersion + 2L, detachedCopy.ChangeVersion, "detached snapshot copy family property ownership");
            Equal(capturedVersion + 2L, project.ChangeVersion, "detached copy must not mutate source freshness");
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
            Equal(baseline + 3L, project.ChangeVersion, "first duplicate removal and remaining owned child edit must each advance once");

            project.Zones.RemoveAt(0);
            zone.Name = "Duplicate zone detached";
            Equal(baseline + 4L, project.ChangeVersion, "last duplicate removal must advance once and detach ownership");

            var family = new ProjectFamily("PF-DUP", "Duplicate family", ElementCategory.ArchitecturalWall);
            project.Families.Add(family);
            project.Families.Add(family);
            family.Properties["A"] = "1";
            Equal(baseline + 7L, project.ChangeVersion, "two family structural adds plus one owned property mutation must each advance once");
            project.Families.RemoveAt(0);
            family.Properties["A"] = "2";
            Equal(baseline + 9L, project.ChangeVersion, "first family removal and remaining owned property edit must each advance once");
            project.Families.RemoveAt(0);
            family.Properties["A"] = "3";
            Equal(baseline + 10L, project.ChangeVersion, "last family removal must advance once and detach ownership");
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

        private static void ServicePropertyMutationsAdvanceProjectFreshnessExactlyOnce()
        {
            var project = CreateProject(out _, out _, out var family);
            var baseline = project.ChangeVersion;

            ProjectFamilyService.SetProperty(project, family.Id, "FireRating", "60");
            Equal(baseline + 1L, project.ChangeVersion, "service set property must advance freshness exactly once");
            Equal("60", family.Properties["FireRating"], "service set property value");

            ProjectFamilyService.SetProperty(project, family.Id, "firerating", "60");
            Equal(baseline + 1L, project.ChangeVersion, "service identical set property no-op");

            ProjectFamilyService.SetProperty(project, family.Id, "FIRERATING", "90");
            Equal(baseline + 2L, project.ChangeVersion, "service replace property must advance freshness exactly once");

            ProjectFamilyService.RemoveProperty(project, family.Id, "missing");
            Equal(baseline + 2L, project.ChangeVersion, "service missing remove no-op");

            ProjectFamilyService.RemoveProperty(project, family.Id, "firerating");
            Equal(baseline + 3L, project.ChangeVersion, "service remove property must advance freshness exactly once");
        }

        private static void ServiceDuplicateAdvancesProjectFreshnessExactlyOnce()
        {
            var project = CreateProject(out _, out _, out var family);
            family.Properties["A"] = "1";
            family.Properties["B"] = "2";
            var baseline = project.ChangeVersion;

            var duplicate = ProjectFamilyService.Duplicate(project, family.Id, "PF2", "Wall Type Copy");

            Equal(baseline + 1L, project.ChangeVersion, "service duplicate with properties must advance freshness exactly once");
            Equal("1", duplicate.Properties["A"], "duplicate property A");
            Equal("2", duplicate.Properties["B"], "duplicate property B");
            if (!ReferenceEquals(duplicate, project.FindFamily("PF2")))
                throw new Exception("Duplicated family must be attached as the project-owned instance.");

            duplicate.Properties["A"] = "3";
            Equal(baseline + 2L, project.ChangeVersion, "duplicated family direct property edit must be owned");
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
            Equal(baseline + 3L, project.ChangeVersion, "three persisted catalog structural adds must each advance freshness once");
            return project;
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception(label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}