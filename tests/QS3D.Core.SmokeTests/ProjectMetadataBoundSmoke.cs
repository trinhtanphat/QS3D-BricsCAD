using System;
using System.Collections.Generic;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMetadataBoundSmoke
    {
        private const int MaximumEntries = 10000;

        internal static void Run()
        {
            PublicMutationStopsAtBoundAndPreservesUpdates();
            PersistenceReplacementStopsAtBoundAndIsAtomic();
            OwnedMappingCapacityFailureDoesNotTouchProject();
            SnapshotSupportsExactBoundary();
        }

        private static void PublicMutationStopsAtBoundAndPreservesUpdates()
        {
            var project = NewProject("public");
            for (var i = 0; i < MaximumEntries; i++)
                project.Metadata.Add(Key(i), "v");

            Equal(MaximumEntries, project.Metadata.Count, "metadata exact boundary");

            project.Metadata[Key(0)] = "updated";
            Equal("updated", project.Metadata[Key(0)], "existing metadata update at boundary");
            Equal(MaximumEntries, project.Metadata.Count, "metadata count after update");

            Throws<ArgumentException>(() => project.Metadata.Add(Key(0).ToUpperInvariant(), "duplicate"), "duplicate precedence at boundary");
            Throws<InvalidOperationException>(() => project.Metadata.Add("overflow", "v"), "metadata entry 10001");
            Equal(MaximumEntries, project.Metadata.Count, "metadata count after rejected overflow");

            if (!project.Metadata.Remove(Key(1)))
                throw new InvalidOperationException("Expected metadata removal before replacement slot test.");
            project.Metadata.Add("replacement", "v");
            Equal(MaximumEntries, project.Metadata.Count, "metadata replacement after removal");
        }

        private static void PersistenceReplacementStopsAtBoundAndIsAtomic()
        {
            var project = NewProject("persistence");
            project.Metadata.Add("seed", "original");

            var method = project.Metadata.GetType().GetMethod(
                "ReplacePersistenceState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException("Project metadata persistence replacement method was not found.");

            var yielded = 0;
            try
            {
                method.Invoke(project.Metadata, new object[] { OverflowingMetadata(() => yielded++) });
                throw new InvalidOperationException("Expected project metadata persistence replacement to reject entry 10001.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
            {
                // Expected: reflection wraps the bounded replacement failure.
            }

            Equal(MaximumEntries + 1, yielded, "lazy metadata enumeration stop point");
            Equal(1, project.Metadata.Count, "atomic metadata replacement count");
            Equal("original", project.Metadata["seed"], "atomic metadata replacement value");
        }

        private static void OwnedMappingCapacityFailureDoesNotTouchProject()
        {
            var project = NewProject("owned");
            for (var i = 0; i < MaximumEntries; i++)
                project.Metadata.Add(Key(i), "v");

            var changeVersion = project.ChangeVersion;
            var mapping = new MeasurementWorkItemMapping(
                "metadata-bound-mapping",
                ElementCategory.Room,
                "area",
                "classification",
                "work-item");

            Throws<InvalidOperationException>(
                () => project.MeasurementWorkItemMappings.Add(mapping),
                "owned mapping metadata entry 10001");
            Equal(changeVersion, project.ChangeVersion, "failed owned metadata mutation change version");
            Equal(MaximumEntries, project.Metadata.Count, "failed owned metadata mutation count");
            Equal(0, project.MeasurementWorkItemMappings.Count, "failed owned mapping collection count");
        }

        private static void SnapshotSupportsExactBoundary()
        {
            var project = NewProject("snapshot");
            for (var i = 0; i < MaximumEntries; i++)
                project.Metadata.Add(Key(i), "v");

            var snapshot = ProjectStateSnapshot.Capture(project);
            if (snapshot == null)
                throw new InvalidOperationException("Project snapshot unexpectedly returned null at the metadata boundary.");
        }

        private static IEnumerable<KeyValuePair<string, string>> OverflowingMetadata(Action onYield)
        {
            for (var i = 0; i <= MaximumEntries; i++)
            {
                onYield();
                yield return new KeyValuePair<string, string>(Key(i), "v");
            }
        }

        private static ProjectState NewProject(string suffix)
        {
            return new ProjectState("metadata-bound-" + suffix, "Metadata Bound " + suffix);
        }

        private static string Key(int index) => "metadata_" + index.ToString("D5");

        private static void Throws<T>(Action action, string label) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException(label + ": expected " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
