using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectCatalogStructuralMutationVersionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AddAdvancesRevision();
            RemoveAdvancesRevision();
            ReplacementAdvancesRevision();
            DuplicateReferenceOwnershipRemainsStable();
            NullInsertionFailsWithoutMutation();
        }

        private static void AddAdvancesRevision()
        {
            var project = NewProject("P-CATALOG-ADD");
            var before = project.ChangeVersion;

            project.Floors.Add(new FloorDefinition("L1", "Level 1", 0d));

            Equal(before + 1L, project.ChangeVersion, "Adding a persisted floor did not advance the project revision exactly once.");
        }

        private static void RemoveAdvancesRevision()
        {
            var project = NewProject("P-CATALOG-REMOVE");
            var floor = new FloorDefinition("L1", "Level 1", 0d);
            project.Floors.Add(floor);
            var before = project.ChangeVersion;

            if (!project.Floors.Remove(floor))
                throw new Exception("Fixture floor was not removed.");

            Equal(before + 1L, project.ChangeVersion, "Removing a persisted floor did not advance the project revision exactly once.");
        }

        private static void ReplacementAdvancesRevision()
        {
            var project = NewProject("P-CATALOG-REPLACE");
            var original = new ZoneDefinition("Z1", "Zone 1");
            var replacement = new ZoneDefinition("Z2", "Zone 2");
            project.Zones.Add(original);
            var before = project.ChangeVersion;

            project.Zones[0] = replacement;

            Equal(before + 1L, project.ChangeVersion, "Replacing a persisted catalog entry did not advance the project revision exactly once.");
            before = project.ChangeVersion;
            project.Zones[0] = replacement;
            Equal(before, project.ChangeVersion, "Assigning the same catalog reference should be a structural no-op.");
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

            var beforeFirstRemove = project.ChangeVersion;
            project.Families.RemoveAt(0);
            Equal(beforeFirstRemove + 1L, project.ChangeVersion, "Removing one duplicate reference did not advance revision once.");

            var beforeStillOwnedMutation = project.ChangeVersion;
            family.Name = "Family 1B";
            Equal(beforeStillOwnedMutation + 1L, project.ChangeVersion, "Removing one duplicate reference detached a still-owned child.");

            var beforeLastRemove = project.ChangeVersion;
            project.Families.RemoveAt(0);
            Equal(beforeLastRemove + 1L, project.ChangeVersion, "Removing the last duplicate reference did not advance revision once.");

            var afterDetach = project.ChangeVersion;
            family.Name = "Family 1C";
            Equal(afterDetach, project.ChangeVersion, "Removing the last duplicate reference left a stale child subscription.");
        }

        private static void NullInsertionFailsWithoutMutation()
        {
            var project = NewProject("P-CATALOG-NULL");
            var before = project.ChangeVersion;

            Throws<ArgumentNullException>(() => project.Zones.Add(null!));

            Equal(before, project.ChangeVersion, "Rejected null insertion advanced the project revision.");
            Equal(0, project.Zones.Count, "Rejected null insertion corrupted the persisted catalog.");
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
