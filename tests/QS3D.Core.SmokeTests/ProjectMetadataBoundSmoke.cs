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
            PersistenceReplacementRejectsNegativeGenericCountBeforeEnumeration();
            PersistenceReplacementRejectsNegativeNonGenericCountBeforeEnumeration();
            PersistenceReplacementRejectsConflictingKnownCountsBeforeEnumeration();
            PersistenceReplacementRejectsKnownCountTraversalMismatch();
            PersistenceReplacementStopsAtBoundAndIsAtomic();
            PersistenceReplacementAcceptsExactKnownBoundary();
            PersistenceReplacementAcceptsPureStreamingInput();
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

            ExpectPersistenceReplacementInvalidOperation(project, input, "known oversized project metadata input");

            Equal(false, input.WasEnumerated, "known oversized metadata enumerated");
            AssertSeedUnchanged(project, "known oversized");
        }

        private static void PersistenceReplacementRejectsKnownOversizedReadOnlyInputBeforeEnumeration()
        {
            var project = NewProject("known-read-only-count");
            project.Metadata.Add("seed", "original");
            var input = new KnownReadOnlyCountMetadataCollection(MaximumEntries + 1);

            ExpectPersistenceReplacementInvalidOperation(project, input, "known oversized read-only project metadata input");

            Equal(false, input.WasEnumerated, "known oversized read-only metadata enumerated");
            AssertSeedUnchanged(project, "known oversized read-only");
        }

        private static void PersistenceReplacementRejectsNegativeGenericCountBeforeEnumeration()
        {
            var project = NewProject("negative-generic-count");
            project.Metadata.Add("seed", "original");
            var input = new KnownCountMetadataCollection(-1);

            ExpectPersistenceReplacementInvalidOperation(project, input, "negative generic project metadata Count");

            Equal(false, input.WasEnumerated, "negative generic metadata enumerated");
            AssertSeedUnchanged(project, "negative generic");
        }

        private static void PersistenceReplacementRejectsNegativeNonGenericCountBeforeEnumeration()
        {
            var project = NewProject("negative-non-generic-count");
            project.Metadata.Add("seed", "original");
            var input = new NonGenericKnownCountMetadataCollection(-1);

            ExpectPersistenceReplacementInvalidOperation(project, input, "negative non-generic project metadata Count");

            Equal(false, input.WasEnumerated, "negative non-generic metadata enumerated");
            AssertSeedUnchanged(project, "negative non-generic");
        }

        private static void PersistenceReplacementRejectsConflictingKnownCountsBeforeEnumeration()
        {
            var project = NewProject("conflicting-counts");
            project.Metadata.Add("seed", "original");
            var input = new ConflictingKnownCountMetadataCollection(1, 2);

            ExpectPersistenceReplacementInvalidOperation(project, input, "conflicting project metadata Count contracts");

            Equal(false, input.WasEnumerated, "conflicting-count metadata enumerated");
            AssertSeedUnchanged(project, "conflicting known counts");
        }

        private static void PersistenceReplacementRejectsKnownCountTraversalMismatch()
        {
            AssertKnownCountTraversalMismatch(2, 1, "under-enumeration");
            AssertKnownCountTraversalMismatch(1, 2, "over-enumeration");
        }

        private static void AssertKnownCountTraversalMismatch(int advertisedCount, int yieldedCount, string label)
        {
            var project = NewProject("traversal-" + label);
            project.Metadata.Add("seed", "original");
            var input = new TraversalMismatchMetadataCollection(advertisedCount, yieldedCount);

            ExpectPersistenceReplacementInvalidOperation(project, input, "project metadata " + label);

            Equal(true, input.WasEnumerated, label + " metadata enumerated");
            Equal(yieldedCount, input.YieldedCount, label + " metadata yielded count");
            AssertSeedUnchanged(project, label);
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
            AssertSeedUnchanged(project, "streaming overflow");
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

        private static void PersistenceReplacementAcceptsPureStreamingInput()
        {
            var project = NewProject("streaming-control");

            InvokePersistenceReplacement(project, StreamingMetadata(3));

            Equal(3, project.Metadata.Count, "pure streaming metadata count");
            Equal("v2", project.Metadata[Key(2)], "pure streaming metadata value");
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

        private static void ExpectPersistenceReplacementInvalidOperation(
            ProjectState project,
            IEnumerable<KeyValuePair<string, string>> input,
            string label)
        {
            try
            {
                InvokePersistenceReplacement(project, input);
                throw new InvalidOperationException("Expected " + label + " to be rejected.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
            {
                // Expected: reflection wraps the fail-closed persistence replacement failure.
            }
        }

        private static void AssertSeedUnchanged(ProjectState project, string label)
        {
            Equal(1, project.Metadata.Count, label + " atomic metadata replacement count");
            Equal("original", project.Metadata["seed"], label + " atomic metadata replacement value");
        }

        private static IEnumerable<KeyValuePair<string, string>> OverflowingMetadata(Action onYield)
        {
            for (var i = 0; i <= MaximumEntries; i++)
            {
                onYield();
                yield return new KeyValuePair<string, string>(Key(i), "v");
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> StreamingMetadata(int count)
        {
            for (var i = 0; i < count; i++)
                yield return new KeyValuePair<string, string>(Key(i), "v" + i);
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
                throw new InvalidOperationException("Known metadata Count must be rejected before enumeration.");
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
                throw new InvalidOperationException("Known read-only metadata Count must be rejected before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericKnownCountMetadataCollection : IEnumerable<KeyValuePair<string, string>>, ICollection
        {
            private readonly int _count;

            public NonGenericKnownCountMetadataCollection(int count) { _count = count; }
            public bool WasEnumerated { get; private set; }
            int ICollection.Count => _count;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                WasEnumerated = true;
                throw new InvalidOperationException("Known non-generic metadata Count must be rejected before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ConflictingKnownCountMetadataCollection :
            ICollection<KeyValuePair<string, string>>,
            IReadOnlyCollection<KeyValuePair<string, string>>
        {
            private readonly int _collectionCount;
            private readonly int _readOnlyCount;

            public ConflictingKnownCountMetadataCollection(int collectionCount, int readOnlyCount)
            {
                _collectionCount = collectionCount;
                _readOnlyCount = readOnlyCount;
            }

            int ICollection<KeyValuePair<string, string>>.Count => _collectionCount;
            int IReadOnlyCollection<KeyValuePair<string, string>>.Count => _readOnlyCount;
            public bool IsReadOnly => true;
            public bool WasEnumerated { get; private set; }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                WasEnumerated = true;
                throw new InvalidOperationException("Conflicting metadata Count contracts must be rejected before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
        }

        private sealed class TraversalMismatchMetadataCollection : ICollection<KeyValuePair<string, string>>
        {
            private readonly int _yieldCount;

            public TraversalMismatchMetadataCollection(int advertisedCount, int yieldCount)
            {
                Count = advertisedCount;
                _yieldCount = yieldCount;
            }

            public int Count { get; }
            public bool IsReadOnly => true;
            public bool WasEnumerated { get; private set; }
            public int YieldedCount { get; private set; }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                WasEnumerated = true;
                for (var i = 0; i < _yieldCount; i++)
                {
                    YieldedCount++;
                    yield return new KeyValuePair<string, string>(Key(i), "v");
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
        }
    }
}
