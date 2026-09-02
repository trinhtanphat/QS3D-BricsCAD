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
