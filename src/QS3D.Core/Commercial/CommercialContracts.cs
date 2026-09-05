using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Commercial
{
    public sealed class CommercialRevisionRef
    {
        public CommercialRevisionRef(string sourceKind, string sourceId, string revisionId)
        {
            SourceKind = CommercialGuard.RequireToken(sourceKind, nameof(sourceKind));
            SourceId = CommercialGuard.RequireToken(sourceId, nameof(sourceId));
            RevisionId = CommercialGuard.RequireToken(revisionId, nameof(revisionId));
        }

        public string SourceKind { get; }
        public string SourceId { get; }
        public string RevisionId { get; }

        public override string ToString() => SourceKind + ":" + SourceId + "@" + RevisionId;
    }

    public sealed class CommercialAuditRecord
    {
        public CommercialAuditRecord(
            string eventId,
            string entityType,
            string entityId,
            string action,
            string actor,
            DateTime occurredUtc,
            string reason,
            string correlationId,
            string beforeSummary,
            string afterSummary,
            IEnumerable<CommercialRevisionRef> sourceRevisions)
        {
            EventId = CommercialGuard.RequireToken(eventId, nameof(eventId));
            EntityType = CommercialGuard.RequireToken(entityType, nameof(entityType));
            EntityId = CommercialGuard.RequireToken(entityId, nameof(entityId));
            Action = CommercialGuard.RequireToken(action, nameof(action));
            Actor = CommercialGuard.RequireOptionalCanonicalText(actor, nameof(actor));
            OccurredUtc = CommercialGuard.RequireUtc(occurredUtc, nameof(occurredUtc));
            Reason = CommercialGuard.RequireOptionalCanonicalText(reason, nameof(reason));
            CorrelationId = CommercialGuard.RequireOptionalToken(correlationId, nameof(correlationId));
            BeforeSummary = CommercialGuard.RequireOptionalCanonicalText(beforeSummary, nameof(beforeSummary));
            AfterSummary = CommercialGuard.RequireOptionalCanonicalText(afterSummary, nameof(afterSummary));
            SourceRevisions = CommercialGuard.SnapshotStableGeneration(
                sourceRevisions,
                nameof(sourceRevisions),
                64,
                CommercialRevisionStateEquals);
        }

        private static bool CommercialRevisionStateEquals(CommercialRevisionRef left, CommercialRevisionRef right)
        {
            return left != null && right != null &&
                string.Equals(left.SourceKind, right.SourceKind, StringComparison.Ordinal) &&
                string.Equals(left.SourceId, right.SourceId, StringComparison.Ordinal) &&
                string.Equals(left.RevisionId, right.RevisionId, StringComparison.Ordinal);
        }

        public string EventId { get; }
        public string EntityType { get; }
        public string EntityId { get; }
        public string Action { get; }
        public string Actor { get; }
        public DateTime OccurredUtc { get; }
        public string Reason { get; }
        public string CorrelationId { get; }
        public string BeforeSummary { get; }
        public string AfterSummary { get; }
        public IReadOnlyList<CommercialRevisionRef> SourceRevisions { get; }
    }

    public sealed class CommercialAuditLog
    {
        private const int MaximumEvents = 10000;
        private readonly List<CommercialAuditRecord> _events = new List<CommercialAuditRecord>();

        public IReadOnlyList<CommercialAuditRecord> Events =>
            new ReadOnlyCollection<CommercialAuditRecord>(_events.ToArray());

        public void Append(CommercialAuditRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (_events.Count >= MaximumEvents)
                throw new InvalidOperationException("Commercial audit log supports at most 10000 events.");
            RequireUniqueEventId(record.EventId, ExistingEventIds());
            _events.Add(record);
        }

        public void AppendBatch(IEnumerable<CommercialAuditRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));

            var remainingCapacity = MaximumEvents - _events.Count;
            var knownCount = TryGetKnownCount(records, out var conflictingKnownCounts, out var negativeKnownCount);
            if (knownCount.HasValue && knownCount.Value > remainingCapacity)
                throw new InvalidOperationException("Commercial audit log supports at most 10000 events.");
            if (negativeKnownCount)
                throw new InvalidOperationException("Commercial audit batch source exposes an invalid negative known Count value.");
            if (conflictingKnownCounts)
                throw new InvalidOperationException("Commercial audit batch source exposes conflicting known Count values.");

            var eventIds = ExistingEventIds();
            var snapshot = new List<CommercialAuditRecord>();
            using (var enumerator = records.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownCountDuringTraversal(records, knownCount);
                    if (!enumerator.MoveNext())
                        break;
                    RequireStableKnownCountDuringTraversal(records, knownCount);
                    CommercialGuard.RequireCanProcessNext(knownCount, snapshot.Count, "Commercial audit batch source");
                    if (snapshot.Count == remainingCapacity)
                        throw new InvalidOperationException("Commercial audit log supports at most 10000 events.");

                    var record = enumerator.Current;
                    RequireStableKnownCountDuringTraversal(records, knownCount);
                    if (record == null) throw new ArgumentException("Commercial audit batch contains a null record.", nameof(records));
                    RequireUniqueEventId(record.EventId, eventIds);
                    snapshot.Add(record);
                }
            }

            if (knownCount.HasValue && snapshot.Count != knownCount.Value)
                throw new InvalidOperationException(
                    "Commercial audit batch source known Count does not match completed traversal cardinality.");
            RequireStableKnownCount(records, knownCount);
            RequireStableAuditBatchGeneration(records, knownCount, snapshot);

            _events.AddRange(snapshot);
        }

        private static void RequireStableAuditBatchGeneration(
            IEnumerable<CommercialAuditRecord> records,
            int? admittedCount,
            IReadOnlyList<CommercialAuditRecord> snapshot)
        {
            if (!admittedCount.HasValue)
                return;

            var index = 0;
            using (var enumerator = records.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownCountDuringTraversal(records, admittedCount);
                    if (!enumerator.MoveNext())
                        break;
                    RequireStableKnownCountDuringTraversal(records, admittedCount);
                    if (index >= snapshot.Count)
                        throw new InvalidOperationException("Commercial audit batch content changed during enumeration.");

                    var current = enumerator.Current;
                    RequireStableKnownCountDuringTraversal(records, admittedCount);
                    if (current == null || !CommercialAuditRecordStateEquals(snapshot[index], current))
                        throw new InvalidOperationException("Commercial audit batch content changed during enumeration.");
                    index++;
                }
            }

            if (index != snapshot.Count)
                throw new InvalidOperationException("Commercial audit batch content changed during enumeration.");
            RequireStableKnownCount(records, admittedCount);
        }

        private static bool CommercialAuditRecordStateEquals(CommercialAuditRecord left, CommercialAuditRecord right)
        {
            if (left == null || right == null ||
                !string.Equals(left.EventId, right.EventId, StringComparison.Ordinal) ||
                !string.Equals(left.EntityType, right.EntityType, StringComparison.Ordinal) ||
                !string.Equals(left.EntityId, right.EntityId, StringComparison.Ordinal) ||
                !string.Equals(left.Action, right.Action, StringComparison.Ordinal) ||
                !string.Equals(left.Actor, right.Actor, StringComparison.Ordinal) ||
                left.OccurredUtc != right.OccurredUtc ||
                !string.Equals(left.Reason, right.Reason, StringComparison.Ordinal) ||
                !string.Equals(left.CorrelationId, right.CorrelationId, StringComparison.Ordinal) ||
                !string.Equals(left.BeforeSummary, right.BeforeSummary, StringComparison.Ordinal) ||
                !string.Equals(left.AfterSummary, right.AfterSummary, StringComparison.Ordinal) ||
                left.SourceRevisions.Count != right.SourceRevisions.Count)
                return false;

            for (var i = 0; i < left.SourceRevisions.Count; i++)
            {
                var leftRevision = left.SourceRevisions[i];
                var rightRevision = right.SourceRevisions[i];
                if (leftRevision == null || rightRevision == null ||
                    !string.Equals(leftRevision.SourceKind, rightRevision.SourceKind, StringComparison.Ordinal) ||
                    !string.Equals(leftRevision.SourceId, rightRevision.SourceId, StringComparison.Ordinal) ||
                    !string.Equals(leftRevision.RevisionId, rightRevision.RevisionId, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private HashSet<string> ExistingEventIds()
        {
            var eventIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < _events.Count; i++)
            {
                var existing = _events[i];
                if (existing == null)
                    throw new InvalidOperationException("Commercial audit log contains a null existing event.");
                if (!eventIds.Add(existing.EventId))
                    throw new InvalidOperationException(
                        "Commercial audit log contains duplicate event id: " + existing.EventId + ".");
            }
            return eventIds;
        }

        private static void RequireUniqueEventId(string eventId, HashSet<string> eventIds)
        {
            if (!eventIds.Add(eventId))
                throw new InvalidOperationException("Commercial audit log contains duplicate event id: " + eventId + ".");
        }

        private static void RequireStableKnownCountDuringTraversal(
            IEnumerable<CommercialAuditRecord> records,
            int? admittedCount)
        {
            if (!admittedCount.HasValue)
                return;

            var reboundCount = TryGetKnownCount(records, out var conflictingKnownCounts, out var negativeKnownCount);
            if (negativeKnownCount)
                throw new InvalidOperationException("Commercial audit batch source exposes an invalid negative known Count value during traversal.");
            if (conflictingKnownCounts)
                throw new InvalidOperationException("Commercial audit batch source exposes conflicting known Count values during traversal.");
            if (!reboundCount.HasValue || reboundCount.Value != admittedCount.Value)
                throw new InvalidOperationException("Commercial audit batch source known Count changed during traversal.");
        }

        private static void RequireStableKnownCount(
            IEnumerable<CommercialAuditRecord> records,
            int? admittedCount)
        {
            if (!admittedCount.HasValue)
                return;

            var reboundCount = TryGetKnownCount(records, out var conflictingKnownCounts, out var negativeKnownCount);
            if (negativeKnownCount)
                throw new InvalidOperationException("Commercial audit batch source exposes an invalid negative known Count value after traversal.");
            if (conflictingKnownCounts)
                throw new InvalidOperationException("Commercial audit batch source exposes conflicting known Count values after traversal.");
            if (!reboundCount.HasValue || reboundCount.Value != admittedCount.Value)
                throw new InvalidOperationException("Commercial audit batch source known Count changed during traversal.");
        }

        private static int? TryGetKnownCount(
            IEnumerable<CommercialAuditRecord> records,
            out bool conflictingKnownCounts,
            out bool negativeKnownCount)
        {
            conflictingKnownCounts = false;
            negativeKnownCount = false;
            int? knownCount = null;

            if (records is ICollection<CommercialAuditRecord> genericCollection)
                knownCount = ObserveKnownCount(knownCount, genericCollection.Count, ref conflictingKnownCounts, ref negativeKnownCount);
            if (records is IReadOnlyCollection<CommercialAuditRecord> readOnlyCollection)
                knownCount = ObserveKnownCount(knownCount, readOnlyCollection.Count, ref conflictingKnownCounts, ref negativeKnownCount);
            if (records is System.Collections.ICollection nonGenericCollection)
                knownCount = ObserveKnownCount(knownCount, nonGenericCollection.Count, ref conflictingKnownCounts, ref negativeKnownCount);

            return knownCount;
        }

        private static int ObserveKnownCount(
            int? current,
            int observed,
            ref bool conflictingKnownCounts,
            ref bool negativeKnownCount)
        {
            if (observed < 0)
                negativeKnownCount = true;
            if (current.HasValue && current.Value != observed)
                conflictingKnownCounts = true;
            return !current.HasValue || observed > current.Value ? observed : current.Value;
        }
    }

    internal static class CommercialGuard
    {
        internal static string RequireToken(string value, string paramName)
        {
            if (value == null) throw new ArgumentNullException(paramName);
            if (value.Length == 0 || string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A non-empty canonical token is required.", paramName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Token must not contain surrounding whitespace.", paramName);
            RejectMalformedUtf16(value, paramName, "Token");
            for (var i = 0; i < value.Length; i++)
                if (char.IsControl(value[i]))
                    throw new ArgumentException("Token must not contain control characters.", paramName);
            return value;
        }

        internal static string RequireOptionalToken(string value, string paramName)
        {
            if (value == null) return string.Empty;
            if (value.Length == 0) return string.Empty;
            return RequireToken(value, paramName);
        }

        internal static string RequireCanonicalText(string value, string paramName)
        {
            if (value == null) throw new ArgumentNullException(paramName);
            if (value.Length == 0 || string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A non-empty canonical text value is required.", paramName);
            return RequireOptionalCanonicalText(value, paramName);
        }

        internal static string RequireOptionalCanonicalText(string value, string paramName)
        {
            if (value == null) return string.Empty;
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Text must not contain surrounding whitespace.", paramName);
            RejectMalformedUtf16(value, paramName, "Text");
            for (var i = 0; i < value.Length; i++)
                if (char.IsControl(value[i]))
                    throw new ArgumentException("Text must not contain control characters.", paramName);
            return value;
        }

        private static void RejectMalformedUtf16(string value, string paramName, string label)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];
                if (char.IsHighSurrogate(current))
                {
                    if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                        throw new ArgumentException(label + " must contain well-formed UTF-16.", paramName);
                    i++;
                    continue;
                }

                if (char.IsLowSurrogate(current))
                    throw new ArgumentException(label + " must contain well-formed UTF-16.", paramName);
            }
        }

        internal static DateTime RequireUtc(DateTime value, string paramName)
        {
            if (value.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Timestamp must use DateTimeKind.Utc.", paramName);
            return value;
        }

        internal static IReadOnlyList<T> Snapshot<T>(
            IEnumerable<T> source,
            string paramName,
            int maximum)
            where T : class
        {
            if (source == null) throw new ArgumentNullException(paramName);

            var knownCount = SnapshotKnownCount(source, paramName, maximum);
            var result = knownCount.HasValue
                ? new List<T>(knownCount.Value)
                : new List<T>();
            using (var enumerator = source.GetEnumerator())
            {
                while (true)
                {
                    RequireStableSnapshotKnownCountDuringTraversal(source, knownCount, paramName, maximum);
                    if (!enumerator.MoveNext())
                        break;
                    RequireStableSnapshotKnownCountDuringTraversal(source, knownCount, paramName, maximum);
                    RequireCanProcessNext(knownCount, result.Count, paramName);
                    if (result.Count == maximum)
                        throw new InvalidOperationException(paramName + " supports at most " + maximum + " entries.");

                    var item = enumerator.Current;
                    RequireStableSnapshotKnownCountDuringTraversal(source, knownCount, paramName, maximum);
                    if (item == null)
                        throw new ArgumentException(paramName + " contains a null item.", paramName);
                    result.Add(item);
                }
            }

            if (knownCount.HasValue && result.Count != knownCount.Value)
                throw new InvalidOperationException(paramName + " known Count does not match completed traversal cardinality.");
            RequireStableSnapshotKnownCount(source, knownCount, paramName, maximum);

            return new ReadOnlyCollection<T>(result.ToArray());
        }

        internal static IReadOnlyList<T> SnapshotStableGeneration<T>(
            IEnumerable<T> source,
            string paramName,
            int maximum,
            Func<T, T, bool> semanticEquals)
            where T : class
        {
            if (semanticEquals == null) throw new ArgumentNullException(nameof(semanticEquals));
            if (source == null) throw new ArgumentNullException(paramName);

            var admittedCount = SnapshotKnownCount(source, paramName, maximum);
            var snapshot = Snapshot(source, paramName, maximum);
            RequireStableSnapshotKnownCount(source, admittedCount, paramName, maximum);
            RequireStableSnapshotGeneration(source, admittedCount, snapshot, semanticEquals, paramName, maximum);
            return snapshot;
        }

        internal static void RequireCanProcessNext(int? knownCount, int observedCount, string label)
        {
            if (knownCount.HasValue && observedCount >= knownCount.Value)
                throw new InvalidOperationException(label + " known Count was exceeded during traversal.");
        }

        private static void RequireStableSnapshotGeneration<T>(
            IEnumerable<T> source,
            int? admittedCount,
            IReadOnlyList<T> snapshot,
            Func<T, T, bool> semanticEquals,
            string paramName,
            int maximum)
            where T : class
        {
            if (!admittedCount.HasValue || semanticEquals == null)
                return;

            var index = 0;
            using (var enumerator = source.GetEnumerator())
            {
                while (true)
                {
                    RequireStableSnapshotKnownCountDuringTraversal(source, admittedCount, paramName, maximum);
                    if (!enumerator.MoveNext())
                        break;
                    RequireStableSnapshotKnownCountDuringTraversal(source, admittedCount, paramName, maximum);
                    if (index >= snapshot.Count)
                        throw new InvalidOperationException(paramName + " content changed during enumeration.");

                    var current = enumerator.Current;
                    RequireStableSnapshotKnownCountDuringTraversal(source, admittedCount, paramName, maximum);
                    if (current == null || !semanticEquals(snapshot[index], current))
                        throw new InvalidOperationException(paramName + " content changed during enumeration.");
                    index++;
                }
            }

            if (index != snapshot.Count)
                throw new InvalidOperationException(paramName + " content changed during enumeration.");
            RequireStableSnapshotKnownCount(source, admittedCount, paramName, maximum);
        }

        private static void RequireStableSnapshotKnownCountDuringTraversal<T>(
            IEnumerable<T> source,
            int? admittedCount,
            string paramName,
            int maximum)
            where T : class
        {
            if (!admittedCount.HasValue)
                return;

            var reboundCount = SnapshotKnownCount(source, paramName, maximum);
            if (!reboundCount.HasValue || reboundCount.Value != admittedCount.Value)
                throw new InvalidOperationException(paramName + " known Count changed during traversal.");
        }

        private static void RequireStableSnapshotKnownCount<T>(
            IEnumerable<T> source,
            int? admittedCount,
            string paramName,
            int maximum)
            where T : class
        {
            if (!admittedCount.HasValue)
                return;

            var reboundCount = SnapshotKnownCount(source, paramName, maximum);
            if (!reboundCount.HasValue || reboundCount.Value != admittedCount.Value)
                throw new InvalidOperationException(paramName + " known Count changed during traversal.");
        }

        private static int? SnapshotKnownCount<T>(IEnumerable<T> source, string paramName, int maximum)
        {
            int? knownCount = null;
            if (source is ICollection<T> genericCollection)
                AcceptKnownCount(genericCollection.Count, paramName, maximum, ref knownCount);
            if (source is IReadOnlyCollection<T> readOnlyCollection)
                AcceptKnownCount(readOnlyCollection.Count, paramName, maximum, ref knownCount);
            if (source is System.Collections.ICollection nonGenericCollection)
                AcceptKnownCount(nonGenericCollection.Count, paramName, maximum, ref knownCount);
            return knownCount;
        }

        private static void AcceptKnownCount(int count, string paramName, int maximum, ref int? knownCount)
        {
            if (count < 0)
                throw new InvalidOperationException(paramName + " exposes an invalid negative known Count value.");
            if (count > maximum)
                throw new InvalidOperationException(paramName + " supports at most " + maximum + " entries.");
            if (knownCount.HasValue && knownCount.Value != count)
                throw new InvalidOperationException(paramName + " exposes conflicting known Count values.");
            knownCount = count;
        }

        internal static decimal Multiply(decimal left, decimal right, string label)
        {
            decimal result;
            try
            {
                result = checked(left * right);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException(label + " overflowed decimal arithmetic.", ex);
            }
            if (left != 0m && right != 0m && result == 0m)
                throw new OverflowException(label + " underflowed decimal arithmetic.");
            return result;
        }

        internal static decimal Add(decimal left, decimal right, string label)
        {
            return CommercialExactDecimalAccumulator.AddExact(left, right, label);
        }

        internal static decimal Subtract(decimal left, decimal right, string label)
        {
            return CommercialExactDecimalAccumulator.SubtractExact(left, right, label);
        }
    }
}
