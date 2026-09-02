using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimatingPortfolioCountIntegritySmoke
    {
        private const int MaximumLines = 10000;

        public static void Run()
        {
            NegativeKnownCountFailsBeforeEnumeration();
            OversizedKnownCountFailsBeforeEnumeration();
            ConflictingKnownCountsFailBeforeEnumeration();
            KnownCountUnderEnumerationFailsClosed();
            KnownCountOverEnumerationFailsClosed();
            HonestKnownCountRemainsAccepted();
            PureStreamExactBoundRemainsAccepted();
            PureStreamStopsAtItem10001();
            DuplicateIdentityRemainsCaseInsensitive();
            PricedTotalRejectsFinalUnrepresentableContribution();
            PricedTotalKeepsRepresentableContribution();
            BulkPreviewRejectsSwallowedAfterTotal();
            BulkPreviewKeepsRepresentableValueDelta();
            CommercialAuditAppendRejectsExistingEventIdAtomically();
            CommercialAuditBatchRejectsInternalDuplicateAtomically();
            CommercialAuditBatchRejectsExistingCollisionAtomically();
            CommercialAuditDistinctEventIdsRemainAccepted();
        }

        private static void NegativeKnownCountFailsBeforeEnumeration()
        {
            var source = new MultiCountCollection(new[] { Line("NEG") }, -1, -1, -1, throwOnEnumeration: true);
            ExpectInvalidOperation(
                () => new EstimatingPortfolio(source),
                "invalid negative line count",
                "Negative known estimating count must fail before enumeration.");
            if (source.EnumerationRequested)
                throw new Exception("Negative known estimating count requested the caller enumerator.");
        }

        private static void OversizedKnownCountFailsBeforeEnumeration()
        {
            var source = new NonGenericCountEnumerable(MaximumLines + 1);
            ExpectInvalidOperation(
                () => new EstimatingPortfolio(source),
                "exceeds the supported",
                "Oversized non-generic estimating Count must fail before enumeration.");
            if (source.EnumerationRequested)
                throw new Exception("Oversized non-generic estimating Count requested the caller enumerator.");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new MultiCountCollection(new[] { Line("CONFLICT") }, 1, 2, 1, throwOnEnumeration: true);
            ExpectInvalidOperation(
                () => new EstimatingPortfolio(source),
                "conflicting known line counts",
                "Conflicting estimating Counts must fail before enumeration.");
            if (source.EnumerationRequested)
                throw new Exception("Conflicting estimating Counts requested the caller enumerator.");
        }

        private static void KnownCountUnderEnumerationFailsClosed()
        {
            var source = new MultiCountCollection(new[] { Line("UNDER") }, 2, 2, 2, throwOnEnumeration: false);
            ExpectInvalidOperation(
                () => new EstimatingPortfolio(source),
                "count changed during enumeration",
                "Estimating portfolio must reject Count 2 -> traversal 1.");
            if (source.EnumerationRequestCount != 1)
                throw new Exception("Known-count under-enumeration must enumerate exactly once.");
        }

        private static void KnownCountOverEnumerationFailsClosed()
        {
            var source = new MultiCountCollection(
                new[] { Line("OVER-1"), Line("OVER-2") },
                1,
                1,
                1,
                throwOnEnumeration: false);
            ExpectInvalidOperation(
                () => new EstimatingPortfolio(source),
                "count changed during enumeration",
                "Estimating portfolio must reject Count 1 -> traversal 2.");
            if (source.EnumerationRequestCount != 1)
                throw new Exception("Known-count over-enumeration must enumerate exactly once.");
        }

        private static void HonestKnownCountRemainsAccepted()
        {
            var source = new MultiCountCollection(
                new[] { Line("b"), Line("A") },
                2,
                2,
                2,
                throwOnEnumeration: false);
            var portfolio = new EstimatingPortfolio(source);
            if (portfolio.Lines.Count != 2 ||
                portfolio.Lines[0].LineId != "A" ||
                portfolio.Lines[1].LineId != "b")
                throw new Exception("Honest known-count estimating input must remain accepted and deterministically sorted.");
            if (!ReferenceEquals(portfolio.GetLine("a"), portfolio.Lines[0]))
                throw new Exception("Estimating portfolio case-insensitive identity lookup changed unexpectedly.");
        }

        private static void PureStreamExactBoundRemainsAccepted()
        {
            var source = new StreamingEnumerable(MaximumLines);
            var portfolio = new EstimatingPortfolio(source);
            if (portfolio.Lines.Count != MaximumLines)
                throw new Exception("Pure streaming estimating input must accept the exact 10,000-line boundary.");
        }

        private static void PureStreamStopsAtItem10001()
        {
            var source = new StreamingEnumerable(MaximumLines + 1);
            ExpectInvalidOperation(
                () => new EstimatingPortfolio(source),
                "supports at most 10000 lines",
                "Pure streaming estimating input must reject item 10,001.");
            if (source.MoveNextCalls != MaximumLines + 1)
                throw new Exception("Estimating portfolio must stop immediately after observing streaming item 10,001.");
        }

        private static void DuplicateIdentityRemainsCaseInsensitive()
        {
            try
            {
                _ = new EstimatingPortfolio(new[] { Line("DUP"), Line("dup") });
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf("Duplicate estimating line id", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new Exception("Duplicate estimating identity diagnostic changed unexpectedly. Actual: " + ex.Message);
            }
            throw new Exception("Case-insensitive duplicate estimating line ids must remain rejected.");
        }

        private static void PricedTotalRejectsFinalUnrepresentableContribution()
        {
            var portfolio = new EstimatingPortfolio(new[]
            {
                PricedLine("A-HIGH", 70000000000000000000000000000m),
                PricedLine("B-TINY", 0.1m)
            });

            ExpectOverflow(
                () => _ = portfolio.PricedTotal,
                "exact aggregate cannot be represented as decimal",
                "Estimating portfolio total must reject a final exact aggregate that decimal cannot represent.");
        }

        private static void PricedTotalKeepsRepresentableContribution()
        {
            var portfolio = new EstimatingPortfolio(new[]
            {
                PricedLine("A-NORMAL", 100m),
                PricedLine("B-FRACTION", 0.1m)
            });

            if (portfolio.PricedTotal != 100.1m)
                throw new Exception("Representable estimating portfolio contribution changed unexpectedly.");
        }

        private static void BulkPreviewRejectsSwallowedAfterTotal()
        {
            var portfolio = new EstimatingPortfolio(new[]
            {
                PricedLine("DELTA-HIGH", 70000000000000000000000000000m)
            });
            var request = ReplacementRequest("DELTA-HIGH", 0.1m);
            var service = new EstimatingWorkflowService();

            ExpectOverflow(
                () => _ = service.PreviewBulkRateAssignment(portfolio, request),
                "precision loss",
                "Bulk preview value delta must reject a non-zero after total swallowed by decimal subtraction precision.");
        }

        private static void BulkPreviewKeepsRepresentableValueDelta()
        {
            var portfolio = new EstimatingPortfolio(new[]
            {
                PricedLine("DELTA-NORMAL", 100m)
            });
            var request = ReplacementRequest("DELTA-NORMAL", 0.1m);
            var preview = new EstimatingWorkflowService().PreviewBulkRateAssignment(portfolio, request);

            if (preview.TotalBefore != 100m || preview.TotalAfter != 0.1m || preview.ValueDelta != -99.9m)
                throw new Exception("Representable bulk preview value delta changed unexpectedly.");
        }

        private static void CommercialAuditAppendRejectsExistingEventIdAtomically()
        {
            var log = new CommercialAuditLog();
            var original = AuditRecord("EV-1", "entity-a");
            log.Append(original);

            ExpectInvalidOperation(
                () => log.Append(AuditRecord("EV-1", "entity-b")),
                "duplicate event id",
                "Commercial audit append must reject an existing exact EventId.");

            if (log.Events.Count != 1 || !ReferenceEquals(log.Events[0], original))
                throw new Exception("Rejected commercial audit append mutated the existing log.");
        }

        private static void CommercialAuditBatchRejectsInternalDuplicateAtomically()
        {
            var log = new CommercialAuditLog();
            log.Append(AuditRecord("BASE", "entity-base"));

            ExpectInvalidOperation(
                () => log.AppendBatch(new[]
                {
                    AuditRecord("BATCH-DUP", "entity-a"),
                    AuditRecord("BATCH-DUP", "entity-b")
                }),
                "duplicate event id",
                "Commercial audit batch must reject duplicate EventIds within the incoming batch.");

            if (log.Events.Count != 1 || log.Events[0].EventId != "BASE")
                throw new Exception("Rejected internal-duplicate commercial audit batch partially mutated the log.");
        }

        private static void CommercialAuditBatchRejectsExistingCollisionAtomically()
        {
            var log = new CommercialAuditLog();
            log.Append(AuditRecord("BASE", "entity-base"));

            ExpectInvalidOperation(
                () => log.AppendBatch(new[]
                {
                    AuditRecord("NEW-1", "entity-a"),
                    AuditRecord("BASE", "entity-b"),
                    AuditRecord("NEW-2", "entity-c")
                }),
                "duplicate event id",
                "Commercial audit batch must reject an EventId already present in the log.");

            if (log.Events.Count != 1 || log.Events[0].EventId != "BASE")
                throw new Exception("Existing-collision commercial audit batch partially mutated the log.");
        }

        private static void CommercialAuditDistinctEventIdsRemainAccepted()
        {
            var log = new CommercialAuditLog();
            log.Append(AuditRecord("Case-Sensitive", "entity-a"));
            log.AppendBatch(new[]
            {
                AuditRecord("case-sensitive", "entity-b"),
                AuditRecord("EV-3", "entity-c")
            });

            if (log.Events.Count != 3 ||
                log.Events[0].EventId != "Case-Sensitive" ||
                log.Events[1].EventId != "case-sensitive" ||
                log.Events[2].EventId != "EV-3")
                throw new Exception("Distinct exact commercial audit EventIds or insertion order changed unexpectedly.");
        }

        private static CommercialAuditRecord AuditRecord(string eventId, string entityId)
        {
            return new CommercialAuditRecord(
                eventId,
                "estimate-line",
                entityId,
                "rate-reviewed",
                "tester",
                new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc),
                string.Empty,
                "corr-1",
                "before",
                "after",
                Array.Empty<CommercialRevisionRef>());
        }

        private static BulkRateAssignmentRequest ReplacementRequest(string lineId, decimal rate)
        {
            return new BulkRateAssignmentRequest(
                new[] { lineId },
                "replacement-cost",
                "replacement-rate-source",
                "replacement-rate-revision",
                new[] { new UnitRateAssignment("m", rate) });
        }

        private static EstimatingLine Line(string id)
        {
            return new EstimatingLine(id, "quantity-source", "quantity-revision", 1m, "m");
        }

        private static EstimatingLine PricedLine(string id, decimal rate)
        {
            return new EstimatingLine(
                id,
                "quantity-source-" + id,
                "quantity-revision",
                1m,
                "m",
                "cost-" + id,
                "rate-source",
                "rate-revision",
                referencedRate: rate);
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessageFragment, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception(message + " Actual diagnostic: " + ex.Message);
                return;
            }
            throw new Exception(message);
        }

        private static void ExpectOverflow(Action action, string expectedMessageFragment, string message)
        {
            try
            {
                action();
            }
            catch (OverflowException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception(message + " Actual diagnostic: " + ex.Message);
                return;
            }
            throw new Exception(message);
        }

        private sealed class MultiCountCollection : ICollection<EstimatingLine>, IReadOnlyCollection<EstimatingLine>, ICollection
        {
            private readonly EstimatingLine[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            public MultiCountCollection(
                EstimatingLine[] items,
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                bool throwOnEnumeration)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            public bool EnumerationRequested { get; private set; }
            public int EnumerationRequestCount { get; private set; }
            int ICollection<EstimatingLine>.Count => _genericCount;
            int IReadOnlyCollection<EstimatingLine>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<EstimatingLine>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<EstimatingLine> GetEnumerator()
            {
                EnumerationRequested = true;
                EnumerationRequestCount++;
                if (_throwOnEnumeration)
                    throw new Exception("Enumerator must not be requested.");
                return ((IEnumerable<EstimatingLine>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<EstimatingLine>.Add(EstimatingLine item) => throw new NotSupportedException();
            void ICollection<EstimatingLine>.Clear() => throw new NotSupportedException();
            bool ICollection<EstimatingLine>.Contains(EstimatingLine item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<EstimatingLine>.CopyTo(EstimatingLine[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<EstimatingLine>.Remove(EstimatingLine item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
        }

        private sealed class NonGenericCountEnumerable : IEnumerable<EstimatingLine>, ICollection
        {
            private readonly int _count;

            public NonGenericCountEnumerable(int count)
            {
                _count = count;
            }

            public bool EnumerationRequested { get; private set; }
            public int Count => _count;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<EstimatingLine> GetEnumerator()
            {
                EnumerationRequested = true;
                throw new Exception("Enumerator must not be requested for oversized known-count input.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class StreamingEnumerable : IEnumerable<EstimatingLine>
        {
            private readonly int _actualCount;

            public StreamingEnumerable(int actualCount)
            {
                _actualCount = actualCount;
            }

            public int MoveNextCalls { get; private set; }

            public IEnumerator<EstimatingLine> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<EstimatingLine>
            {
                private readonly StreamingEnumerable _owner;
                private int _index = -1;

                public Enumerator(StreamingEnumerable owner)
                {
                    _owner = owner;
                }

                public EstimatingLine Current { get; private set; } = null!;
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index >= _owner._actualCount)
                        return false;
                    Current = Line("STREAM-" + _index);
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
