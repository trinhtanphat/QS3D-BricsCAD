using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialAuditLogCountTraversalSmoke
    {
        internal static void Run()
        {
            UnderEnumerationRejects();
            OverEnumerationRejects();
            HonestKnownCountRemainsAccepted();
            PureStreamingRemainsAccepted();
            NegativeKnownCountRejectsBeforePublication();
            ConflictingKnownCountsRejectBeforePublication();
            OversizedKnownCountRejectsBeforeEnumeration();
            TraversalMismatchRemainsFailureAtomic();
            NullRecordFailureRemainsAtomic();
        }

        private static void UnderEnumerationRejects()
        {
            var log = new CommercialAuditLog();
            var records = new ReportedCountCollection(reportedCount: 2, actualCount: 1);
            var error = Capture<InvalidOperationException>(() => log.AppendBatch(records));
            Contains("known Count does not match completed traversal cardinality", error.Message);
            Equal(0, log.Events.Count, "Under-enumerated batch must not publish partial audit events.");
        }

        private static void OverEnumerationRejects()
        {
            var log = new CommercialAuditLog();
            var records = new ReportedCountCollection(reportedCount: 1, actualCount: 2);
            var error = Capture<InvalidOperationException>(() => log.AppendBatch(records));
            Contains("known Count does not match completed traversal cardinality", error.Message);
            Equal(0, log.Events.Count, "Over-enumerated batch must not publish partial audit events.");
        }

        private static void HonestKnownCountRemainsAccepted()
        {
            var log = new CommercialAuditLog();
            log.AppendBatch(new ReportedCountCollection(reportedCount: 2, actualCount: 2));
            Equal(2, log.Events.Count, "Honest counted audit batch changed accepted cardinality.");
        }

        private static void PureStreamingRemainsAccepted()
        {
            var log = new CommercialAuditLog();
            log.AppendBatch(new StreamingRecords(2));
            Equal(2, log.Events.Count, "Pure streaming audit batches must remain supported.");
        }

        private static void NegativeKnownCountRejectsBeforePublication()
        {
            var log = new CommercialAuditLog();
            var error = Capture<InvalidOperationException>(
                () => log.AppendBatch(new ReportedCountCollection(reportedCount: -1, actualCount: 0)));
            Contains("invalid negative known Count", error.Message);
            Equal(0, log.Events.Count, "Negative Count evidence must fail before publication.");
        }

        private static void ConflictingKnownCountsRejectBeforePublication()
        {
            var log = new CommercialAuditLog();
            var error = Capture<InvalidOperationException>(() => log.AppendBatch(new ConflictingCountCollection()));
            Contains("conflicting known Count values", error.Message);
            Equal(0, log.Events.Count, "Conflicting Count evidence must fail before publication.");
        }

        private static void OversizedKnownCountRejectsBeforeEnumeration()
        {
            var log = new CommercialAuditLog();
            var records = new ReportedCountCollection(reportedCount: 10001, actualCount: 0);
            var error = Capture<InvalidOperationException>(() => log.AppendBatch(records));
            Contains("supports at most 10000 events", error.Message);
            Equal(0, records.EnumerationCount, "Oversized known Count must reject before traversal.");
            Equal(0, log.Events.Count, "Oversized known Count must not publish audit events.");
        }

        private static void TraversalMismatchRemainsFailureAtomic()
        {
            var log = new CommercialAuditLog();
            log.Append(CreateRecord(90));
            Capture<InvalidOperationException>(
                () => log.AppendBatch(new ReportedCountCollection(reportedCount: 2, actualCount: 1)));
            Equal(1, log.Events.Count, "Rejected Count/traversal mismatch mutated existing audit state.");
            Equal("COMM-AUDIT-00090", log.Events[0].EventId, "Rejected batch changed the pre-existing audit record.");
        }

        private static void NullRecordFailureRemainsAtomic()
        {
            var log = new CommercialAuditLog();
            log.Append(CreateRecord(91));
            Capture<ArgumentException>(() => log.AppendBatch(new NullContainingStream()));
            Equal(1, log.Events.Count, "Null-containing rejected batch published a buffered audit record.");
            Equal("COMM-AUDIT-00091", log.Events[0].EventId, "Null-containing rejected batch changed existing audit state.");
        }

        private static CommercialAuditRecord CreateRecord(int index)
        {
            var suffix = index.ToString("D5");
            return new CommercialAuditRecord(
                "COMM-AUDIT-" + suffix,
                "Element",
                "ELEMENT-" + suffix,
                "Update",
                "SmokeTest",
                new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc).AddSeconds(index),
                "Regression coverage",
                "CORRELATION-" + suffix,
                "Before",
                "After",
                Array.Empty<CommercialRevisionRef>());
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Expected diagnostic fragment '" + expected + "'. Actual: " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private sealed class ReportedCountCollection : ICollection<CommercialAuditRecord>
        {
            private readonly int _reportedCount;
            private readonly int _actualCount;

            internal ReportedCountCollection(int reportedCount, int actualCount)
            {
                _reportedCount = reportedCount;
                _actualCount = actualCount;
            }

            public int Count => _reportedCount;
            public bool IsReadOnly => true;
            public int EnumerationCount { get; private set; }

            public IEnumerator<CommercialAuditRecord> GetEnumerator()
            {
                EnumerationCount++;
                for (var i = 0; i < _actualCount; i++)
                    yield return CreateRecord(i);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(CommercialAuditRecord item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(CommercialAuditRecord item) => false;
            public void CopyTo(CommercialAuditRecord[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(CommercialAuditRecord item) => throw new NotSupportedException();
        }

        private sealed class ConflictingCountCollection : ICollection<CommercialAuditRecord>, IReadOnlyCollection<CommercialAuditRecord>
        {
            int ICollection<CommercialAuditRecord>.Count => 1;
            int IReadOnlyCollection<CommercialAuditRecord>.Count => 2;
            public bool IsReadOnly => true;

            public IEnumerator<CommercialAuditRecord> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(CommercialAuditRecord item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(CommercialAuditRecord item) => false;
            public void CopyTo(CommercialAuditRecord[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(CommercialAuditRecord item) => throw new NotSupportedException();
        }

        private sealed class StreamingRecords : IEnumerable<CommercialAuditRecord>
        {
            private readonly int _count;

            internal StreamingRecords(int count)
            {
                _count = count;
            }

            public IEnumerator<CommercialAuditRecord> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                    yield return CreateRecord(i);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NullContainingStream : IEnumerable<CommercialAuditRecord>
        {
            public IEnumerator<CommercialAuditRecord> GetEnumerator()
            {
                yield return CreateRecord(92);
                yield return null!;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
