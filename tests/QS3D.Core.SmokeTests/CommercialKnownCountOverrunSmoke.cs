using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialKnownCountOverrunSmoke
    {
        private static readonly DateTime OccurredUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            AuditBatchOverrunPrecedesUnexpectedRecordValidation();
            AuditBatchUnderTraversalRemainsFailureAtomic();
            SourceRevisionOverrunPrecedesUnexpectedItemValidation();
            SourceRevisionUnderTraversalStillFailsAfterTraversal();
            HonestCountedAndStreamingInputsRemainAccepted();
        }

        private static void AuditBatchOverrunPrecedesUnexpectedRecordValidation()
        {
            var log = new CommercialAuditLog();
            var error = Capture<InvalidOperationException>(() =>
                log.AppendBatch(new MisreportedReadOnlyCollection<CommercialAuditRecord>(1, Record("EVENT-1"), null!)));

            Contains("known Count was exceeded", error.Message,
                "Commercial audit batch must reject Count overrun before validating the unexpected null record.");
            Equal(0, log.Events.Count, "Rejected audit batch must remain failure-atomic.");
        }

        private static void AuditBatchUnderTraversalRemainsFailureAtomic()
        {
            var log = new CommercialAuditLog();
            var error = Capture<InvalidOperationException>(() =>
                log.AppendBatch(new MisreportedReadOnlyCollection<CommercialAuditRecord>(2, Record("EVENT-UNDER"))));

            Contains("known Count does not match completed traversal cardinality", error.Message,
                "Under-traversal must retain the final Count/cardinality check.");
            Equal(0, log.Events.Count, "Under-traversal rejection must not publish a partial batch.");
        }

        private static void SourceRevisionOverrunPrecedesUnexpectedItemValidation()
        {
            var error = Capture<InvalidOperationException>(() =>
                Record(
                    "EVENT-REV-OVER",
                    new MisreportedReadOnlyCollection<CommercialRevisionRef>(1, Revision("REV-1"), null!)));

            Contains("known Count was exceeded", error.Message,
                "Source revision snapshot must reject Count overrun before validating the unexpected null revision.");
        }

        private static void SourceRevisionUnderTraversalStillFailsAfterTraversal()
        {
            var error = Capture<InvalidOperationException>(() =>
                Record(
                    "EVENT-REV-UNDER",
                    new MisreportedReadOnlyCollection<CommercialRevisionRef>(2, Revision("REV-1"))));

            Contains("known Count does not match completed traversal cardinality", error.Message,
                "Source revision under-traversal must retain the final Count/cardinality check.");
        }

        private static void HonestCountedAndStreamingInputsRemainAccepted()
        {
            var countedLog = new CommercialAuditLog();
            countedLog.AppendBatch(new MisreportedReadOnlyCollection<CommercialAuditRecord>(1, Record("EVENT-COUNTED")));
            Equal(1, countedLog.Events.Count, "Exact known Count/traversal agreement must remain accepted.");

            var streamingLog = new CommercialAuditLog();
            streamingLog.AppendBatch(Stream(Record("EVENT-STREAM-1"), Record("EVENT-STREAM-2")));
            Equal(2, streamingLog.Events.Count, "Pure streaming audit input must remain accepted.");

            var countedRecord = Record(
                "EVENT-REV-COUNTED",
                new MisreportedReadOnlyCollection<CommercialRevisionRef>(1, Revision("REV-COUNTED")));
            Equal(1, countedRecord.SourceRevisions.Count, "Exact counted source revisions must remain accepted.");

            var streamingRecord = Record(
                "EVENT-REV-STREAM",
                Stream(Revision("REV-STREAM-1"), Revision("REV-STREAM-2")));
            Equal(2, streamingRecord.SourceRevisions.Count, "Pure streaming source revisions must remain accepted.");
        }

        private static CommercialAuditRecord Record(
            string eventId,
            IEnumerable<CommercialRevisionRef>? revisions = null)
        {
            return new CommercialAuditRecord(
                eventId,
                "estimate-line",
                "LINE-1",
                "updated",
                "",
                OccurredUtc,
                "",
                "",
                "",
                "",
                revisions ?? Array.Empty<CommercialRevisionRef>());
        }

        private static CommercialRevisionRef Revision(string revisionId)
        {
            return new CommercialRevisionRef("quantity", "QTY-1", revisionId);
        }

        private static IEnumerable<T> Stream<T>(params T[] items)
        {
            for (var i = 0; i < items.Length; i++)
                yield return items[i];
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + " was not thrown.");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Expected fragment='" + expected + "', actual='" + actual + "'.");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class MisreportedReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;

            internal MisreportedReadOnlyCollection(int advertisedCount, params T[] items)
            {
                Count = advertisedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count { get; }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++)
                    yield return _items[i];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class CommercialKnownCountOverrunRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CommercialKnownCountOverrunSmoke.Run();
        }
    }
}
