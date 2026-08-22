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

        private static System.Collections.Generic.KeyValuePair<string, string>[] Entries(ProjectState project) =>
            project.Metadata.Where(x => x.Key.StartsWith(Prefix, StringComparison.Ordinal)).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Key, StringComparer.Ordinal).ToArray();

        private static void Add(ProjectState project, string id, ElementCategory category, string item, string classification, string work) =>
            project.MeasurementWorkItemMappings.Add(new MeasurementWorkItemMapping(id, category, item, classification, work));

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
