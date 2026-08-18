using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using QS3D.Core.Cost;
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
            PersistenceReplacementRejectsKnownOversizedInputBeforeEnumeration();
            PersistenceReplacementRejectsKnownOversizedReadOnlyInputBeforeEnumeration();
            PersistenceReplacementStopsAtBoundAndIsAtomic();
            PersistenceReplacementAcceptsExactKnownBoundary();
            OwnedMappingCapacityFailureDoesNotTouchProject();
            OwnedTbqCapacityFailureDoesNotTouchProject();
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

        private static void PersistenceReplacementRejectsKnownOversizedInputBeforeEnumeration()
        {
            var project = NewProject("known-count");
            project.Metadata.Add("seed", "original");
            var input = new KnownCountMetadataCollection(MaximumEntries + 1);

            try
            {
                InvokePersistenceReplacement(project, input);
                throw new InvalidOperationException("Expected known oversized project metadata input to be rejected.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
            {
                // Expected: reflection wraps the bounded replacement failure.
            }

            Equal(false, input.WasEnumerated, "known oversized metadata enumerated");
            Equal(1, project.Metadata.Count, "known oversized atomic metadata replacement count");
            Equal("original", project.Metadata["seed"], "known oversized atomic metadata replacement value");
        }

        private static void PersistenceReplacementRejectsKnownOversizedReadOnlyInputBeforeEnumeration()
        {
            var project = NewProject("known-read-only-count");
            project.Metadata.Add("seed", "original");
            var input = new KnownReadOnlyCountMetadataCollection(MaximumEntries + 1);

            try
            {
                InvokePersistenceReplacement(project, input);
                throw new InvalidOperationException("Expected known oversized read-only project metadata input to be rejected.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
            {
                // Expected: reflection wraps the bounded replacement failure.
            }

            Equal(false, input.WasEnumerated, "known oversized read-only metadata enumerated");
            Equal(1, project.Metadata.Count, "known oversized read-only atomic metadata replacement count");
            Equal("original", project.Metadata["seed"], "known oversized read-only atomic metadata replacement value");
        }

        private static void PersistenceReplacementStopsAtBoundAndIsAtomic()
        {
            var project = NewProject("persistence");
            project.Metadata.Add("seed", "original");

            var yielded = 0;
            try
            {
                InvokePersistenceReplacement(project, OverflowingMetadata(() => yielded++));
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

        private static void PersistenceReplacementAcceptsExactKnownBoundary()
        {
            var project = NewProject("known-exact");
            var input = new List<KeyValuePair<string, string>>(MaximumEntries);
            for (var i = 0; i < MaximumEntries; i++)
                input.Add(new KeyValuePair<string, string>(Key(i), "v"));

            InvokePersistenceReplacement(project, input);
            Equal(MaximumEntries, project.Metadata.Count, "known metadata exact boundary");
            Equal("v", project.Metadata[Key(MaximumEntries - 1)], "known metadata exact boundary value");
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

        private static void OwnedTbqCapacityFailureDoesNotTouchProject()
        {
            var project = NewProject("tbq-owned");
            for (var i = 0; i < MaximumEntries; i++)
                project.Metadata.Add(Key(i), "v");

            var workspace = ProjectTbqWorkspace.Open(project);
            var state = new TbqProjectWorkspaceState(
                "VND",
                0m,
                Array.Empty<TbqBillItem>(),
                Array.Empty<BuildUpRateSnapshot>(),
                Array.Empty<RateReferenceEdge>(),
                "PROJECT",
                Array.Empty<BqLibraryEntry>());
            var changeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(
                () => workspace.Replace(state),
                "owned TBQ workspace metadata entry 10001");
            Equal(changeVersion, project.ChangeVersion, "failed TBQ metadata mutation change version");
            Equal(MaximumEntries, project.Metadata.Count, "failed TBQ metadata mutation count");
            Equal(false, workspace.HasValue, "failed TBQ metadata mutation workspace state");
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

        private static void InvokePersistenceReplacement(
            ProjectState project,
            IEnumerable<KeyValuePair<string, string>> input)
        {
            var method = project.Metadata.GetType().GetMethod(
                "ReplacePersistenceState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException("Project metadata persistence replacement method was not found.");
            method.Invoke(project.Metadata, new object[] { input });
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

        private sealed class KnownCountMetadataCollection : ICollection<KeyValuePair<string, string>>
        {
            public KnownCountMetadataCollection(int count) { Count = count; }
            public int Count { get; }
            public bool IsReadOnly => true;
            public bool WasEnumerated { get; private set; }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                WasEnumerated = true;
                throw new InvalidOperationException("Known oversized metadata must be rejected before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
        }

        private sealed class KnownReadOnlyCountMetadataCollection : IReadOnlyCollection<KeyValuePair<string, string>>
        {
            public KnownReadOnlyCountMetadataCollection(int count) { Count = count; }
            public int Count { get; }
            public bool WasEnumerated { get; private set; }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                WasEnumerated = true;
                throw new InvalidOperationException("Known oversized read-only metadata must be rejected before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
