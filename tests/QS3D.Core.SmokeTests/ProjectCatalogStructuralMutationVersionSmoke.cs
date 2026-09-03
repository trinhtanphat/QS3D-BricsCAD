using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectCatalogStructuralMutationVersionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AddInsertRemoveAndClearAdvanceRevision();
            ReplacementAdvancesRevision();
            FailedAndNullMutationsRemainNeutral();
            DuplicateReferenceOwnershipRemainsStable();
            StructuralMutationOverflowFailsBeforeCatalogChange();
            LoadPreservesPersistedRevisionAfterCatalogHydration();
        }

        private static void AddInsertRemoveAndClearAdvanceRevision()
        {
            var project = NewProject("P-CATALOG-STRUCTURAL");
            var floor1 = new FloorDefinition("L1", "Level 1", 0d);
            var floor2 = new FloorDefinition("L2", "Level 2", 3d);
            var floor3 = new FloorDefinition("L3", "Level 3", 6d);

            ExpectOneRevision(project, () => project.Floors.Add(floor1), "Add");
            ExpectOneRevision(project, () => project.Floors.Insert(0, floor2), "Insert");
            ExpectOneRevision(project, () => project.Floors.RemoveAt(0), "RemoveAt");
            ExpectOneRevision(project, () =>
            {
                if (!project.Floors.Remove(floor1)) throw new Exception("Fixture floor was not removed.");
            }, "Remove");

            project.Floors.Add(floor1);
            project.Floors.Add(floor2);
            project.Floors.Add(floor3);
            ExpectOneRevision(project, project.Floors.Clear, "Clear");
            Equal(0, project.Floors.Count, "Clear left persisted floor entries behind.");
        }

        private static void ReplacementAdvancesRevision()
        {
            var project = NewProject("P-CATALOG-REPLACE");
            var original = new ZoneDefinition("Z1", "Zone 1");
            var replacement = new ZoneDefinition("Z2", "Zone 2");
            project.Zones.Add(original);

            ExpectOneRevision(project, () => project.Zones[0] = replacement, "Indexer replacement");
            var before = project.ChangeVersion;
            project.Zones[0] = replacement;
            Equal(before, project.ChangeVersion, "Assigning the same catalog reference should be a structural no-op.");
        }

        private static void FailedAndNullMutationsRemainNeutral()
        {
            var project = NewProject("P-CATALOG-REJECT");
            var zone = new ZoneDefinition("Z1", "Zone 1");
            project.Zones.Add(zone);

            var before = project.ChangeVersion;
            if (project.Zones.Remove(new ZoneDefinition("MISSING", "Missing")))
                throw new Exception("Absent catalog entry was unexpectedly removed.");
            Equal(before, project.ChangeVersion, "Absent Remove advanced the project revision.");

            Throws<ArgumentOutOfRangeException>(() => project.Zones.Insert(3, new ZoneDefinition("Z2", "Zone 2")));
            Equal(before, project.ChangeVersion, "Rejected Insert advanced the project revision.");
            Equal(1, project.Zones.Count, "Rejected Insert corrupted the persisted catalog.");

            Throws<ArgumentNullException>(() => project.Zones.Add(null!));
            Equal(before, project.ChangeVersion, "Rejected null Add advanced the project revision.");
            Equal(1, project.Zones.Count, "Rejected null Add corrupted the persisted catalog.");

            Throws<ArgumentNullException>(() => project.Zones[0] = null!);
            Equal(before, project.ChangeVersion, "Rejected null replacement advanced the project revision.");
            Equal(zone, project.Zones[0], "Rejected null replacement corrupted the persisted catalog.");
        }

        private static void DuplicateReferenceOwnershipRemainsStable()
        {
            var project = NewProject("P-CATALOG-DUP");
            var family = new ProjectFamily("F1", "Family 1", ElementCategory.GlassWall);
            project.Families.Add(family);
            project.Families.Add(family);

            var beforeChildMutation = project.ChangeVersion;
            family.Name = "Family 1A";
            Equal(beforeChildMutation + 1L, project.ChangeVersion, "Duplicate catalog references subscribed the same child more than once or not at all.");

            ExpectOneRevision(project, () => project.Families.RemoveAt(0), "First duplicate removal");
            var beforeStillOwnedMutation = project.ChangeVersion;
            family.Name = "Family 1B";
            Equal(beforeStillOwnedMutation + 1L, project.ChangeVersion, "Removing one duplicate reference detached a still-owned child.");

            ExpectOneRevision(project, () => project.Families.RemoveAt(0), "Last duplicate removal");
            var afterDetach = project.ChangeVersion;
            family.Name = "Family 1C";
            Equal(afterDetach, project.ChangeVersion, "Removing the last duplicate reference left a stale child subscription.");
        }

        private static void StructuralMutationOverflowFailsBeforeCatalogChange()
        {
            var marker = new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

            var addProject = NewProject("P-CATALOG-OVERFLOW-ADD");
            RestoreProjectRevision(addProject, marker, long.MaxValue);
            Throws<OverflowException>(() => addProject.Zones.Add(new ZoneDefinition("Z1", "Zone 1")));
            Equal(0, addProject.Zones.Count, "Overflowing Add mutated the catalog before revision admission.");
            Equal(marker, addProject.UpdatedUtc, "Overflowing Add changed UpdatedUtc.");

            var replaceProject = NewProject("P-CATALOG-OVERFLOW-REPLACE");
            var original = new ZoneDefinition("Z1", "Zone 1");
            replaceProject.Zones.Add(original);
            RestoreProjectRevision(replaceProject, marker, long.MaxValue);
            Throws<OverflowException>(() => replaceProject.Zones[0] = new ZoneDefinition("Z2", "Zone 2"));
            Equal(original, replaceProject.Zones[0], "Overflowing replacement changed the catalog entry.");

            var removeProject = NewProject("P-CATALOG-OVERFLOW-REMOVE");
            var floor = new FloorDefinition("L1", "Level 1", 0d);
            removeProject.Floors.Add(floor);
            RestoreProjectRevision(removeProject, marker, long.MaxValue);
            Throws<OverflowException>(() => removeProject.Floors.RemoveAt(0));
            Equal(1, removeProject.Floors.Count, "Overflowing RemoveAt removed the catalog entry.");
            Equal(floor, removeProject.Floors[0], "Overflowing RemoveAt changed the retained catalog entry.");

            var clearProject = NewProject("P-CATALOG-OVERFLOW-CLEAR");
            clearProject.Families.Add(new ProjectFamily("F1", "Family 1", ElementCategory.GlassWall));
            clearProject.Families.Add(new ProjectFamily("F2", "Family 2", ElementCategory.GlassWall));
            RestoreProjectRevision(clearProject, marker, long.MaxValue);
            Throws<OverflowException>(clearProject.Families.Clear);
            Equal(2, clearProject.Families.Count, "Overflowing Clear changed the catalog.");
            Equal(marker, clearProject.UpdatedUtc, "Overflowing Clear changed UpdatedUtc.");
        }

        private static void LoadPreservesPersistedRevisionAfterCatalogHydration()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-catalog-revision-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "project.qsdb");
            try
            {
                var project = NewProject("P-CATALOG-ROUNDTRIP");
                project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
                project.Floors.Add(new FloorDefinition("L1", "Level 1", 0d));
                project.Families.Add(new ProjectFamily("F1", "Family 1", ElementCategory.GlassWall));
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var persistedVersion = project.ChangeVersion;
                var persistedUpdatedUtc = project.UpdatedUtc;

                var loaded = store.Load(path);

                Equal(persistedVersion, loaded.ChangeVersion, "QSDB load inflated the persisted project revision while hydrating catalogs.");
                Equal(persistedUpdatedUtc, loaded.UpdatedUtc, "QSDB load changed the persisted update timestamp while hydrating catalogs.");
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static void RestoreProjectRevision(ProjectState project, DateTime updatedUtc, long changeVersion)
        {
            var method = typeof(ProjectState).GetMethod("RestorePersistenceState", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("ProjectState.RestorePersistenceState was not found.");
            method.Invoke(project, new object[] { updatedUtc, changeVersion });
            Equal(changeVersion, project.ChangeVersion, "Fixture could not restore the requested project revision.");
            Equal(updatedUtc, project.UpdatedUtc, "Fixture could not restore the requested project timestamp.");
        }

        private static void ExpectOneRevision(ProjectState project, Action mutation, string label)
        {
            var before = project.ChangeVersion;
            mutation();
            Equal(before + 1L, project.ChangeVersion, label + " did not advance the project revision exactly once.");
        }

        private static ProjectState NewProject(string id) => new ProjectState(id, "Catalog revision fixture");

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new Exception("Expected " + typeof(TException).Name + " was not thrown.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
