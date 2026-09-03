using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialAuditBatchGenerationStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SameCountReplacementIsRejected();
            SameCountReorderIsRejected();
            StableCountedBatchRemainsAccepted();
            StreamingBatchRemainsSinglePassCompatible();
            Console.WriteLine("PASS commercial audit batch generation stability");
        }

        private static void SameCountReplacementIsRejected()
        {
            var source = new SameCountDriftCollection<CommercialAuditRecord>(
                new[] { Record("EV-1", "ENTITY-A"), Record("EV-2", "ENTITY-B") },
                new[] { Record("EV-1", "ENTITY-A"), Record("EV-3", "ENTITY-C") });

            ExpectContentDrift(source, "same-count audit replacement");
        }

        private static void SameCountReorderIsRejected()
        {
            var first = Record("EV-10", "ENTITY-X");
            var second = Record("EV-11", "ENTITY-Y");
            var source = new SameCountDriftCollection<CommercialAuditRecord>(
                new[] { first, second },
                new[] { second, first });

            ExpectContentDrift(source, "same-count audit reorder");
        }

        private static void StableCountedBatchRemainsAccepted()
        {
            var log = new CommercialAuditLog();
            log.AppendBatch(new List<CommercialAuditRecord>
            {
                Record("EV-20", "ENTITY-A"),
                Record("EV-21", "ENTITY-B")
            });
            Require(log.Events.Count == 2, "stable counted audit batch changed");
            Require(log.Events[0].EventId == "EV-20" && log.Events[1].EventId == "EV-21",
                "stable counted audit order changed");
        }

        private static void StreamingBatchRemainsSinglePassCompatible()
        {
            var log = new CommercialAuditLog();
            log.AppendBatch(Yield(Record("EV-30", "ENTITY-S")));
            Require(log.Events.Count == 1 && log.Events[0].EventId == "EV-30",
                "streaming audit batch changed");
        }

        private static void ExpectContentDrift(IEnumerable<CommercialAuditRecord> source, string label)
        {
            var log = new CommercialAuditLog();
            try
            {
                log.AppendBatch(source);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("content changed during enumeration", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Require(log.Events.Count == 0, label + " partially mutated the audit log");
                    return;
                }
                throw new InvalidOperationException(label + " failed for the wrong reason: " + ex.Message, ex);
            }

            throw new InvalidOperationException(label + " was accepted unexpectedly.");
        }

        private static CommercialAuditRecord Record(string eventId, string entityId)
        {
            return new CommercialAuditRecord(
                eventId,
                "estimate",
                entityId,
                "update",
                "tester",
                new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc),
                "reason-" + eventId,
                "corr-" + eventId,
                "before-" + entityId,
                "after-" + entityId,
                new[]
                {
                    new CommercialRevisionRef("quantity", "Q-" + entityId, "R-1"),
                    new CommercialRevisionRef("rate", "RATE-" + entityId, "R-2")
                });
        }

        private static IEnumerable<CommercialAuditRecord> Yield(CommercialAuditRecord value)
        {
            yield return value;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class SameCountDriftCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly T[] _first;
            private readonly T[] _second;
            private int _enumerations;

            internal SameCountDriftCollection(T[] first, T[] second)
            {
                if (first == null) throw new ArgumentNullException(nameof(first));
                if (second == null) throw new ArgumentNullException(nameof(second));
                if (first.Length != second.Length)
                    throw new ArgumentException("Drift generations must preserve Count.");
                _first = first;
                _second = second;
            }

            public int Count => _first.Length;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                var generation = _enumerations++ == 0 ? _first : _second;
                return ((IEnumerable<T>)generation).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => ((ICollection<T>)_first).Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _first.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }
    }
}
