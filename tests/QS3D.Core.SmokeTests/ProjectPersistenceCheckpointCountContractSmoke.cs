using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceCheckpointCountContractSmoke
    {
        private const int MaximumElementCount = 10000;

        [ModuleInitializer]
        internal static void Run()
        {
            HiddenOversizedSecondaryCountFailsBeforeEnumeration();
            ConflictingInBoundCountsFailBeforeEnumeration();
            OversizeTakesPrecedenceOverConflict();
            ConsistentMultiContractCountRemainsAccepted();
        }

        private static void HiddenOversizedSecondaryCountFailsBeforeEnumeration()
        {
            var project = BuildSingleElementProject("P-HIDDEN-OVERSIZE");
            var source = MultiCountCollection.NeverEnumerate(1, MaximumElementCount + 1, 1);

            var error = Capture<InvalidOperationException>(() => ProjectPersistenceCheckpoint.Capture(project, source));

            Equal(1, source.GenericCountReads, "Hidden-oversize validation must inspect ICollection<string>.Count exactly once.");
            Equal(1, source.ReadOnlyCountReads, "Hidden-oversize validation must inspect IReadOnlyCollection<string>.Count exactly once.");
            Equal(1, source.NonGenericCountReads, "Hidden-oversize validation must inspect ICollection.Count exactly once.");
            Equal(0, source.GetEnumeratorCalls, "A secondary oversized Count contract must reject before enumeration.");
            Contains("10000", error.Message, "Hidden oversized Count must preserve the checkpoint capacity diagnostic.");
        }

        private static void ConflictingInBoundCountsFailBeforeEnumeration()
        {
            var project = BuildSingleElementProject("P-CONFLICT");
            var source = MultiCountCollection.NeverEnumerate(1, 2, 1);

            var error = Capture<InvalidOperationException>(() => ProjectPersistenceCheckpoint.Capture(project, source));

            Equal(1, source.GenericCountReads, "Conflicting-count validation must inspect ICollection<string>.Count exactly once.");
            Equal(1, source.ReadOnlyCountReads, "Conflicting-count validation must inspect IReadOnlyCollection<string>.Count exactly once.");
            Equal(1, source.NonGenericCountReads, "Conflicting-count validation must inspect ICollection.Count exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Conflicting in-bound Count contracts must reject before enumeration.");
            Contains("conflicting known counts", error.Message, "Conflicting Count contracts must fail closed explicitly.");
        }

        private static void OversizeTakesPrecedenceOverConflict()
        {
            var project = BuildSingleElementProject("P-OVERSIZE-PRECEDENCE");
            var source = MultiCountCollection.NeverEnumerate(MaximumElementCount + 1, 2, 2);

            var error = Capture<InvalidOperationException>(() => ProjectPersistenceCheckpoint.Capture(project, source));

            Equal(0, source.GetEnumeratorCalls, "Oversized conflicting Count contracts must reject before enumeration.");
            Contains("10000", error.Message, "Capacity rejection must take precedence when any known Count is oversized.");
        }

        private static void ConsistentMultiContractCountRemainsAccepted()
        {
            var project = BuildSingleElementProject("P-CONSISTENT");
            var source = MultiCountCollection.WithValues(1, 1, 1, "e1");

            var checkpoint = ProjectPersistenceCheckpoint.Capture(project, source);

            Equal(1, source.GenericCountReads, "Consistent-count validation must inspect ICollection<string>.Count exactly once.");
            Equal(1, source.ReadOnlyCountReads, "Consistent-count validation must inspect IReadOnlyCollection<string>.Count exactly once.");
            Equal(1, source.NonGenericCountReads, "Consistent-count validation must inspect ICollection.Count exactly once.");
            Equal(1, source.GetEnumeratorCalls, "Consistent Count contracts should enumerate the caller source exactly once.");
            Equal(1, checkpoint.ElementIds.Count, "Consistent multi-contract source did not capture its canonical target.");
            Equal("e1", checkpoint.ElementIds[0], "Checkpoint changed canonical caller identity text.");
            True(checkpoint.Matches(project), "Consistent multi-contract checkpoint did not match its source project.");
        }

        private static ProjectState BuildSingleElementProject(string projectId)
        {
            var project = new ProjectState(projectId, "Checkpoint count-contract regression");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.GlassWall));
            project.Touch();
            return project;
        }

        private sealed class MultiCountCollection : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly string[]? _values;
            private readonly bool _throwOnEnumeration;

            private MultiCountCollection(int genericCount, int readOnlyCount, int nonGenericCount, string[]? values, bool throwOnEnumeration)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _values = values;
                _throwOnEnumeration = throwOnEnumeration;
            }

            public static MultiCountCollection NeverEnumerate(int genericCount, int readOnlyCount, int nonGenericCount) =>
                new MultiCountCollection(genericCount, readOnlyCount, nonGenericCount, null, true);

            public static MultiCountCollection WithValues(int genericCount, int readOnlyCount, int nonGenericCount, params string[] values) =>
                new MultiCountCollection(genericCount, readOnlyCount, nonGenericCount, values, false);

            int ICollection<string>.Count
            {
                get
                {
                    GenericCountReads++;
                    return _genericCount;
                }
            }

            int IReadOnlyCollection<string>.Count
            {
                get
                {
                    ReadOnlyCountReads++;
                    return _readOnlyCount;
                }
            }

            int ICollection.Count
            {
                get
                {
                    NonGenericCountReads++;
                    return _nonGenericCount;
                }
            }

            public int GenericCountReads { get; private set; }
            public int ReadOnlyCountReads { get; private set; }
            public int NonGenericCountReads { get; private set; }
            public int GetEnumeratorCalls { get; private set; }
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<string> GetEnumerator()
            {
                GetEnumeratorCalls++;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Count-contract rejection must happen before enumeration.");
                return ((IEnumerable<string>)(_values ?? Array.Empty<string>())).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private static T Capture<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T ex) { return ex; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Actual='" + (actual ?? "<null>") + "'.");
        }
    }
}
