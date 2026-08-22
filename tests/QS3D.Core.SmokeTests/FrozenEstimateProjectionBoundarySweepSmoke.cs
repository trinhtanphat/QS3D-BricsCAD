using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class FrozenEstimateProjectionBoundarySweepSmoke
    {
        private static readonly DateTime T0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Run()
        {
            ExactBoundaryAndStreamingLimitFailClosed();
            KnownCountContractsFailClosedBeforeUnsafeTraversal();
            TraversalMismatchNullAndDuplicateInputsFailClosed();
            OrderingAndSnapshotIsolationAreDeterministic();
            ProjectedRowPreservesCommercialAndProvenanceFields();
        }

        private static void ExactBoundaryAndStreamingLimitFailClosed()
        {
            var context = Context();
            var exact = new EstimateLine[10000];
            for (var i = 0; i < exact.Length; i++)
                exact[i] = Line(context, "L" + i.ToString("D5"));

            var projection = FrozenEstimateProjection.Create(exact);
            Equal(10000, projection.Rows.Count, "exact 10,000 line boundary remains accepted");
            Equal("L00000", projection.Rows[0].EstimateLineId, "exact-bound first row ordering");
            Equal("L09999", projection.Rows[9999].EstimateLineId, "exact-bound last row ordering");

            var knownOversize = new MultiCountSource(
                Array.Empty<EstimateLine>(), 10001, 10001, 10001, throwOnEnumeration: true);
            Throws<InvalidOperationException>(
                () => FrozenEstimateProjection.Create(knownOversize),
                "known 10,001 line source");
            True(!knownOversize.EnumeratorRequested,
                "known oversize source must fail before requesting an enumerator");

            var streamed = new CountingEnumerable(context, 10001);
            Throws<InvalidOperationException>(
                () => FrozenEstimateProjection.Create(streamed),
                "unknown-count boundary+1 source");
            Equal(10001, streamed.Yielded,
                "unknown-count source must stop exactly at the 10,001st yielded line");
        }

        private static void KnownCountContractsFailClosedBeforeUnsafeTraversal()
        {
            var context = Context();
            var one = new[] { Line(context, "L1") };

            var negative = new MultiCountSource(one, -1, -1, -1, throwOnEnumeration: true);
            Throws<InvalidOperationException>(
                () => FrozenEstimateProjection.Create(negative),
                "negative known count");
            True(!negative.EnumeratorRequested,
                "negative known count must fail before enumeration");

            var conflict = new MultiCountSource(one, 1, 2, 1, throwOnEnumeration: true);
            Throws<InvalidOperationException>(
                () => FrozenEstimateProjection.Create(conflict),
                "conflicting known counts");
            True(!conflict.EnumeratorRequested,
                "conflicting known counts must fail before enumeration");

            var pure = PureEnumerable(one);
            var pureProjection = FrozenEstimateProjection.Create(pure);
            Equal(1, pureProjection.Rows.Count,
                "pure IEnumerable without known-count evidence remains accepted");
        }

        private static void TraversalMismatchNullAndDuplicateInputsFailClosed()
        {
            var context = Context();
            var one = new[] { Line(context, "L1") };

            var advertisedLong = new MultiCountSource(one, 2, 2, 2, throwOnEnumeration: false);
            Throws<InvalidOperationException>(
                () => FrozenEstimateProjection.Create(advertisedLong),
                "advertised count greater than traversal");
            True(advertisedLong.EnumeratorRequested,
                "in-bound advertised count source should be traversed before mismatch detection");
            Equal(1, advertisedLong.Yielded,
                "short traversal mismatch should consume only the available line");

            var two = new[] { Line(context, "L1"), Line(context, "L2") };
            var advertisedShort = new MultiCountSource(two, 1, 1, 1, throwOnEnumeration: false);
            Throws<InvalidOperationException>(
                () => FrozenEstimateProjection.Create(advertisedShort),
                "advertised count smaller than traversal");
            Equal(2, advertisedShort.Yielded,
                "small advertised count mismatch is detected after the observed traversal");

            Throws<ArgumentException>(
                () => FrozenEstimateProjection.Create(new EstimateLine[] { Line(context, "L1"), null! }),
                "null estimate line");

            Throws<ArgumentException>(
                () => FrozenEstimateProjection.Create(new[] { Line(context, "Case-ID"), Line(context, "case-id") }),
                "case-insensitive duplicate EstimateLineId");
        }

        private static void OrderingAndSnapshotIsolationAreDeterministic()
        {
            var context = Context();
            var source = new List<EstimateLine>
            {
                Line(context, "zeta"),
                Line(context, "Beta"),
                Line(context, "alpha"),
                Line(context, "ALPHA-2")
            };

            var forward = FrozenEstimateProjection.Create(source);
            var reverse = FrozenEstimateProjection.Create(source.AsEnumerable().Reverse());
            Equal(forward.Rows.Count, reverse.Rows.Count, "ordering cardinality");
            for (var i = 0; i < forward.Rows.Count; i++)
                Equal(forward.Rows[i].EstimateLineId, reverse.Rows[i].EstimateLineId,
                    "row ordering must not depend on caller traversal order at index " + i);

            Equal("alpha", forward.Rows[0].EstimateLineId, "case-insensitive primary ordering");
            Equal("ALPHA-2", forward.Rows[1].EstimateLineId, "ordinal tie-shape ordering");
            Equal("Beta", forward.Rows[2].EstimateLineId, "middle ordering control");
            Equal("zeta", forward.Rows[3].EstimateLineId, "last ordering control");

            source.Clear();
            Equal(4, forward.Rows.Count,
                "projection rows must remain detached from caller collection mutation");
            Equal("alpha", forward.Rows[0].EstimateLineId,
                "projection contents remain stable after caller list mutation");
        }

        private static void ProjectedRowPreservesCommercialAndProvenanceFields()
        {
            var context = Context();
            var line = EstimateLine.Create(
                "LINE-TRACE",
                context.Snapshot,
                "SEM-1",
                "SRC-1",
                "QTY-1",
                context.RateBook,
                new CostCode("COST-1"),
                "USD",
                T0.AddDays(3),
                commercialAdjustmentQuantity: 0.5m,
                commercialAdjustmentReason: "waste allowance");

            var row = FrozenEstimateProjection.Create(new[] { line }).Rows[0];
            Equal("LINE-TRACE", row.EstimateLineId, "estimate line id");
            Equal("SEM-1", row.SemanticIdentity, "semantic identity");
            Equal("SRC-1", row.SourceIdentity, "source identity");
            Equal("QTY-1", row.QuantityKey, "quantity key");
            Equal("RB-1", row.RateBookId, "rate-book id");
            Equal("RATE-1", row.RateItemId, "rate item id");
            Equal("v7", row.RateVersion, "rate version");
            Equal(T0.AddDays(3), row.RateAsOfUtc, "rate as-of provenance");
            Equal("COST-1", row.CostCode, "cost code");
            Equal("m3", row.Unit, "unit");
            Equal("USD", row.Currency, "currency");
            Equal(2.5m, row.MeasuredQuantity, "measured quantity");
            Equal(0.5m, row.CommercialAdjustmentQuantity, "commercial adjustment quantity");
            Equal("waste allowance", row.CommercialAdjustmentReason ?? string.Empty,
                "commercial adjustment reason");
            Equal(3.0m, row.EstimatingQuantity, "estimating quantity");
            Equal(12.5m, row.UnitRate, "unit rate");
            Equal(37.5m, row.FinalAmount, "final amount");
        }

        private static EstimateContext Context()
        {
            var trace = new MeasurementTrace(
                "SEM-1",
                "SRC-1",
                "QTY-1",
                Array.Empty<MeasurementTraceFact>(),
                2.5d,
                Array.Empty<MeasurementTraceAdjustment>(),
                2.5d,
                "m3",
                "none");
            var snapshot = new MeasurementSnapshot(new[] { trace });
            var rate = new RateItem(
                "RATE-1",
                new CostCode("COST-1"),
                "m3",
                "USD",
                12.5m,
                T0,
                "v7");
            var rateBook = new RateBook("RB-1", new[] { rate });
            return new EstimateContext(snapshot, rateBook);
        }

        private static EstimateLine Line(EstimateContext context, string id)
        {
            return EstimateLine.Create(
                id,
                context.Snapshot,
                "SEM-1",
                "SRC-1",
                "QTY-1",
                context.RateBook,
                new CostCode("COST-1"),
                "USD",
                T0.AddDays(1));
        }

        private static IEnumerable<EstimateLine> PureEnumerable(IEnumerable<EstimateLine> source)
        {
            foreach (var line in source)
                yield return line;
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
                throw new Exception("FrozenEstimateProjection regression: expected " + typeof(T).Name + " for " + message + ".");
            }
            catch (T)
            {
            }
        }

        private static void True(bool condition, string message)
        {
            if (!condition)
                throw new Exception("FrozenEstimateProjection regression: " + message + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new Exception("FrozenEstimateProjection regression: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(decimal expected, decimal actual, string message)
        {
            if (expected != actual)
                throw new Exception("FrozenEstimateProjection regression: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(DateTime expected, DateTime actual, string message)
        {
            if (expected != actual)
                throw new Exception("FrozenEstimateProjection regression: " + message + ". Expected=" + expected.ToString("O") + ", actual=" + actual.ToString("O") + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("FrozenEstimateProjection regression: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class EstimateContext
        {
            internal EstimateContext(MeasurementSnapshot snapshot, RateBook rateBook)
            {
                Snapshot = snapshot;
                RateBook = rateBook;
            }

            internal MeasurementSnapshot Snapshot { get; }
            internal RateBook RateBook { get; }
        }

        private sealed class CountingEnumerable : IEnumerable<EstimateLine>
        {
            private readonly EstimateContext _context;
            private readonly int _count;

            internal CountingEnumerable(EstimateContext context, int count)
            {
                _context = context;
                _count = count;
            }

            internal int Yielded { get; private set; }

            public IEnumerator<EstimateLine> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    Yielded++;
                    yield return Line(_context, "S" + i.ToString("D5"));
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class MultiCountSource :
            ICollection<EstimateLine>,
            IReadOnlyCollection<EstimateLine>,
            ICollection
        {
            private readonly IReadOnlyList<EstimateLine> _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            internal MultiCountSource(
                IReadOnlyList<EstimateLine> items,
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

            internal bool EnumeratorRequested { get; private set; }
            internal int Yielded { get; private set; }

            int ICollection<EstimateLine>.Count => _genericCount;
            int IReadOnlyCollection<EstimateLine>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<EstimateLine>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<EstimateLine> GetEnumerator()
            {
                EnumeratorRequested = true;
                if (_throwOnEnumeration)
                    throw new Exception("Enumerator must not be requested for this control.");
                for (var i = 0; i < _items.Count; i++)
                {
                    Yielded++;
                    yield return _items[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            bool ICollection<EstimateLine>.Contains(EstimateLine item) => throw new NotSupportedException();
            void ICollection<EstimateLine>.CopyTo(EstimateLine[] array, int arrayIndex) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            void ICollection<EstimateLine>.Add(EstimateLine item) => throw new NotSupportedException();
            bool ICollection<EstimateLine>.Remove(EstimateLine item) => throw new NotSupportedException();
            void ICollection<EstimateLine>.Clear() => throw new NotSupportedException();
        }
    }
}
