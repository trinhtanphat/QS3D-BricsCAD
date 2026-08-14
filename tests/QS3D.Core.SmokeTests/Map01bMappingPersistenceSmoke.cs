using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class Map01bMappingPersistenceSmoke
    {
        private const string Prefix = "QS3D.Mapping.v1.";

        internal static void Run()
        {
            if (ProjectState.CurrentSchemaVersion != 4) throw new Exception("MAP-01B requires project schema v4.");
            MetadataRoundtripIsCanonical();
            SnapshotIsDetached();
            PersistedConflictsFailClosed();
            MappingMutationsAdvanceProjectRevision();
            RevisionOverflowFailsBeforeMappingWrite();
        }

        private static void MetadataRoundtripIsCanonical()
        {
            var source = NewProject("source");
            Add(source, "map-z", ElementCategory.Room, "AreaM2", "class-room", "work-room");
            Add(source, "map-a", ElementCategory.ArchitecturalWall, "NetWallAreaM2", "class-wall", "work-wall");
            var persisted = Entries(source);
            if (persisted.Length != 2) throw new Exception("Project mapping collection did not create two persistence entries.");

            var restored = NewProject("restored");
            foreach (var pair in persisted) restored.Metadata.Add(pair.Key, pair.Value);
            var wall = new MeasurementWorkItemMappingCatalog(restored.MeasurementWorkItemMappings)
                .Resolve(ElementCategory.ArchitecturalWall, "NetWallAreaM2").Mapping;
            if (wall == null || wall.MappingId != "map-a" || wall.ClassificationId != "class-wall" || wall.WorkItemId != "work-wall")
                throw new Exception("Mapping persistence metadata did not reconstruct canonical project mapping state.");

            var reverse = NewProject("reverse");
            Add(reverse, "map-a", ElementCategory.ArchitecturalWall, "NetWallAreaM2", "class-wall", "work-wall");
            Add(reverse, "map-z", ElementCategory.Room, "AreaM2", "class-room", "work-room");
            if (!Entries(source).Select(x => x.Key + "=" + x.Value).SequenceEqual(Entries(reverse).Select(x => x.Key + "=" + x.Value), StringComparer.Ordinal))
                throw new Exception("Canonical mapping persistence depends on insertion order.");
        }

        private static void SnapshotIsDetached()
        {
            var project = NewProject("snapshot");
            Add(project, "map-a", ElementCategory.Room, "AreaM2", "class-room", "work-room");
            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            project.MeasurementWorkItemMappings.Clear();
            if (detached.MeasurementWorkItemMappings.Count != 1 || detached.MeasurementWorkItemMappings.Single().MappingId != "map-a")
                throw new Exception("Detached project snapshot lost mapping persistence state.");
        }

        private static void PersistedConflictsFailClosed()
        {
            var first = NewProject("first");
            Add(first, "map-a", ElementCategory.ArchitecturalWall, "NetWallAreaM2", "class-a", "work-a");
            var ambiguousOther = NewProject("ambiguous-other");
            Add(ambiguousOther, "map-b", ElementCategory.ArchitecturalWall, "NetWallAreaM2", "class-b", "work-b");
            var ambiguous = NewProject("ambiguous");
            ambiguous.Metadata.Add(Entries(first)[0].Key, Entries(first)[0].Value);
            ExpectFailure(() => ambiguous.Metadata.Add(Entries(ambiguousOther)[0].Key, Entries(ambiguousOther)[0].Value), "Ambiguous persisted mapping metadata was accepted.");

            var duplicateOther = NewProject("duplicate-other");
            Add(duplicateOther, "map-a", ElementCategory.Room, "AreaM2", "class-b", "work-b");
            var duplicate = NewProject("duplicate");
            duplicate.Metadata.Add(Entries(first)[0].Key, Entries(first)[0].Value);
            ExpectFailure(() => duplicate.Metadata.Add(Entries(duplicateOther)[0].Key, Entries(duplicateOther)[0].Value), "Duplicate persisted mapping id was accepted.");
        }

        private static void MappingMutationsAdvanceProjectRevision()
        {
            var remove = NewProject("revision-remove");
            var baseline = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc);
            remove.UpdatedUtc = baseline;
            var mapping = Mapping("map-a", ElementCategory.Room, "AreaM2", "class-room", "work-room");
            remove.MeasurementWorkItemMappings.Add(mapping);
            if (remove.ChangeVersion != 1L || remove.UpdatedUtc <= baseline)
                throw new Exception("Adding a project mapping did not advance project persistence state exactly once.");

            var afterAddVersion = remove.ChangeVersion;
            var afterAddUpdated = remove.UpdatedUtc;
            ExpectFailure(() => remove.MeasurementWorkItemMappings.Add(mapping), "Duplicate mapping add unexpectedly succeeded.");
            if (remove.ChangeVersion != afterAddVersion || remove.UpdatedUtc != afterAddUpdated)
                throw new Exception("Rejected mapping add changed project persistence state.");

            var missing = Mapping("missing", ElementCategory.Room, "PerimeterM", "class-missing", "work-missing");
            if (remove.MeasurementWorkItemMappings.Remove(missing)) throw new Exception("Missing mapping removal unexpectedly succeeded.");
            if (remove.ChangeVersion != afterAddVersion || remove.UpdatedUtc != afterAddUpdated)
                throw new Exception("Missing mapping removal changed project persistence state.");

            if (!remove.MeasurementWorkItemMappings.Remove(mapping)) throw new Exception("Existing mapping removal unexpectedly failed.");
            if (remove.ChangeVersion != afterAddVersion + 1L || Entries(remove).Length != 0)
                throw new Exception("Existing mapping removal did not advance project revision exactly once.");

            var clear = NewProject("revision-clear");
            Add(clear, "map-a", ElementCategory.Room, "AreaM2", "class-room", "work-room");
            Add(clear, "map-b", ElementCategory.Column, "VolumeM3", "class-column", "work-column");
            var beforeClearVersion = clear.ChangeVersion;
            clear.MeasurementWorkItemMappings.Clear();
            if (clear.ChangeVersion != beforeClearVersion + 1L || Entries(clear).Length != 0)
                throw new Exception("Non-empty mapping clear did not advance project revision exactly once.");
            var afterClearVersion = clear.ChangeVersion;
            var afterClearUpdated = clear.UpdatedUtc;
            clear.MeasurementWorkItemMappings.Clear();
            if (clear.ChangeVersion != afterClearVersion || clear.UpdatedUtc != afterClearUpdated)
                throw new Exception("Empty mapping clear changed project persistence state.");
        }

        private static void RevisionOverflowFailsBeforeMappingWrite()
        {
            var project = NewProject("revision-overflow");
            var changeVersion = typeof(ProjectState).GetProperty(nameof(ProjectState.ChangeVersion))
                ?? throw new Exception("Project ChangeVersion property is unavailable.");
            var setter = changeVersion.GetSetMethod(true)
                ?? throw new Exception("Project ChangeVersion private setter is unavailable.");
            setter.Invoke(project, new object[] { long.MaxValue });
            var beforeUpdated = project.UpdatedUtc;

            try
            {
                Add(project, "map-a", ElementCategory.Room, "AreaM2", "class-room", "work-room");
            }
            catch (OverflowException)
            {
                if (project.ChangeVersion != long.MaxValue || project.UpdatedUtc != beforeUpdated || Entries(project).Length != 0)
                    throw new Exception("Mapping revision overflow mutated project persistence state before failing.");
                return;
            }

            throw new Exception("Mapping mutation accepted a project ChangeVersion overflow.");
        }

        private static System.Collections.Generic.KeyValuePair<string, string>[] Entries(ProjectState project) =>
            project.Metadata.Where(x => x.Key.StartsWith(Prefix, StringComparison.Ordinal)).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Key, StringComparer.Ordinal).ToArray();

        private static MeasurementWorkItemMapping Mapping(string id, ElementCategory category, string item, string classification, string work) =>
            new MeasurementWorkItemMapping(id, category, item, classification, work);

        private static void Add(ProjectState project, string id, ElementCategory category, string item, string classification, string work) =>
            project.MeasurementWorkItemMappings.Add(Mapping(id, category, item, classification, work));

        private static void ExpectFailure(Action action, string message)
        {
            try { action(); }
            catch (ArgumentException) { return; }
            catch (FormatException) { return; }
            throw new Exception(message);
        }

        private static ProjectState NewProject(string id) => new ProjectState("map01b-" + id, "MAP-01B smoke");
    }
}
