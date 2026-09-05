using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialSnapshotSemanticGenerationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CountedSemanticGenerationDriftFailsClosed();
            CountedEquivalentValueGenerationSucceeds();
            StreamingSourceRemainsSinglePass();
        }

        private static void CountedSemanticGenerationDriftFailsClosed()
        {
            var source = new ReplayCollection(
                new[]
                {
                    Revision("model", "A-1", "r1"),
                    Revision("model", "A-2", "r1"),
                },
                new[]
                {
                    Revision("model", "B-1", "r1"),
                    Revision("model", "B-2", "r1"),
                });

            Throws<InvalidOperationException>(() => Record("DRIFT", source));
            Equal(2, source.EnumerationCount, "counted semantic drift must be detected by replaying the admitted generation");
        }

        private static void CountedEquivalentValueGenerationSucceeds()
        {
            var source = new ReplayCollection(
                new[]
                {
                    Revision("model", "EQ-1", "r1"),
                    Revision("rate-book", "EQ-2", "r7"),
                },
                new[]
                {
                    Revision("model", "EQ-1", "r1"),
                    Revision("rate-book", "EQ-2", "r7"),
                });

            var record = Record("EQUIVALENT", source);
            Equal(2, source.EnumerationCount, "counted stable generation should be replayed exactly once");
            Equal(2, record.SourceRevisions.Count, "stable replay lost source revisions");
            Equal("EQ-1", record.SourceRevisions[0].SourceId, "stable replay changed first source revision ordering");
            Equal("EQ-2", record.SourceRevisions[1].SourceId, "stable replay changed second source revision ordering");
        }

        private static void StreamingSourceRemainsSinglePass()
        {
            var source = new SinglePassStream();
            var record = Record("STREAM", source);
            Equal(1, source.EnumerationCount, "pure streaming source must not be replayed");
            Equal(2, record.SourceRevisions.Count, "streaming snapshot lost source revisions");
        }

        private static CommercialRevisionRef Revision(string kind, string id, string revision)
        {
            return new CommercialRevisionRef(kind, id, revision);
        }

        private static CommercialAuditRecord Record(string eventId, IEnumerable<CommercialRevisionRef> revisions)
        {
            return new CommercialAuditRecord(
                eventId,
                "estimate",
                "entity-1",
                "update",
                "tester",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                "semantic generation",
                "corr-1",
                "before",
                "after",
                revisions);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException(
                "CommercialSnapshotSemanticGenerationSmoke: expected " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    "CommercialSnapshotSemanticGenerationSmoke: " + message + ". Expected " + expected + ", got " + actual + ".");
        }

        private sealed class ReplayCollection : ICollection<CommercialRevisionRef>
        {
            private readonly CommercialRevisionRef[] _first;
            private readonly CommercialRevisionRef[] _second;

            public ReplayCollection(CommercialRevisionRef[] first, CommercialRevisionRef[] second)
            {
                _first = first;
                _second = second;
                if (_first.Length != _second.Length)
                    throw new ArgumentException("Replay generations must expose the same Count.");
            }

            public int EnumerationCount { get; private set; }
            public int Count => _first.Length;
            public bool IsReadOnly => true;

            public IEnumerator<CommercialRevisionRef> GetEnumerator()
            {
                EnumerationCount++;
                var generation = EnumerationCount == 1 ? _first : _second;
                for (var i = 0; i < generation.Length; i++)
                    yield return generation[i];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(CommercialRevisionRef item) => Array.IndexOf(_first, item) >= 0;
            public void CopyTo(CommercialRevisionRef[] array, int arrayIndex) => _first.CopyTo(array, arrayIndex);
            public void Add(CommercialRevisionRef item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(CommercialRevisionRef item) => throw new NotSupportedException();
        }

        private sealed class SinglePassStream : IEnumerable<CommercialRevisionRef>
        {
            public int EnumerationCount { get; private set; }

            public IEnumerator<CommercialRevisionRef> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount > 1)
                    throw new InvalidOperationException("Streaming source was enumerated more than once.");
                yield return Revision("model", "STREAM-1", "r1");
                yield return Revision("model", "STREAM-2", "r1");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
