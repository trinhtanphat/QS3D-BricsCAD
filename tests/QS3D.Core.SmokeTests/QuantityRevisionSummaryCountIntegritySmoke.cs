using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRevisionSummaryCountIntegritySmoke
    {
        internal static void Run()
        {
            NegativeNonGenericCountFailsBeforeEnumeration();
            ConflictingKnownCountsFailBeforeEnumeration();
            CountGreaterThanTraversalFailsClosed();
            CountLessThanTraversalFailsClosed();
            HonestCountedRowsPreserveSummary();
            PureStreamPreservesSummary();
        }

        private static void NegativeNonGenericCountFailsBeforeEnumeration()
        {
            var source = new NonGenericCountSequence(-1);
            ExpectInvalidOperation(() => new QuantityRevisionReport().Summarize(source));
            if (source.EnumeratorEntered)
                throw new Exception("Quantity revision summary must reject negative known Count before enumeration.");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new ConflictingCountSequence();
            ExpectInvalidOperation(() => new QuantityRevisionReport().Summarize(source));
            if (source.EnumeratorEntered)
                throw new Exception("Quantity revision summary must reject conflicting known Counts before enumeration.");
        }

        private static void CountGreaterThanTraversalFailsClosed()
        {
            var source = new CountedSequence(2, new[] { Row("LengthM", 1d, 2d) });
            ExpectInvalidOperation(() => new QuantityRevisionReport().Summarize(source));
        }

        private static void CountLessThanTraversalFailsClosed()
        {
            var source = new CountedSequence(1, new[]
            {
                Row("LengthM", 1d, 2d),
                Row("LengthM", 2d, 4d)
            });
            ExpectInvalidOperation(() => new QuantityRevisionReport().Summarize(source));
        }

        private static void HonestCountedRowsPreserveSummary()
        {
            var source = new CountedSequence(2, new[]
            {
                Row("LengthM", 1d, 3d),
                Row("lengthm", 2d, 4d)
            });
            var result = new QuantityRevisionReport().Summarize(source);
            if (result.Count != 1 || result[0].Before != 3d || result[0].After != 7d)
                throw new Exception("Quantity revision honest counted input did not preserve case-insensitive summary semantics.");
        }

        private static void PureStreamPreservesSummary()
        {
            var result = new QuantityRevisionReport().Summarize(PureRows());
            if (result.Count != 1 || result[0].Before != 3d || result[0].After != 6d)
                throw new Exception("Quantity revision pure IEnumerable input did not preserve summary semantics.");
        }

        private static IEnumerable<QuantityRevisionRow> PureRows()
        {
            yield return Row("AreaM2", 1d, 2d);
            yield return Row("AreaM2", 2d, 4d);
        }

        private static QuantityRevisionRow Row(string quantityName, double before, double after)
        {
            return new QuantityRevisionRow
            {
                ElementId = "E1",
                Category = "Slab",
                QuantityName = quantityName,
                Change = "Changed",
                Before = before,
                After = after
            };
        }

        private static void ExpectInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new Exception("Expected InvalidOperationException.");
        }

        private sealed class NonGenericCountSequence : IEnumerable<QuantityRevisionRow>, ICollection
        {
            internal NonGenericCountSequence(int count)
            {
                Count = count;
            }

            public int Count { get; }
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            internal bool EnumeratorEntered { get; private set; }

            public IEnumerator<QuantityRevisionRow> GetEnumerator()
            {
                EnumeratorEntered = true;
                throw new Exception("Enumeration must not begin for an invalid known Count.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ConflictingCountSequence : ICollection<QuantityRevisionRow>, IReadOnlyCollection<QuantityRevisionRow>
        {
            int ICollection<QuantityRevisionRow>.Count => 1;
            int IReadOnlyCollection<QuantityRevisionRow>.Count => 2;
            bool ICollection<QuantityRevisionRow>.IsReadOnly => true;
            internal bool EnumeratorEntered { get; private set; }

            public IEnumerator<QuantityRevisionRow> GetEnumerator()
            {
                EnumeratorEntered = true;
                throw new Exception("Enumeration must not begin for conflicting known Counts.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<QuantityRevisionRow>.Add(QuantityRevisionRow item) => throw new NotSupportedException();
            void ICollection<QuantityRevisionRow>.Clear() => throw new NotSupportedException();
            bool ICollection<QuantityRevisionRow>.Contains(QuantityRevisionRow item) => false;
            void ICollection<QuantityRevisionRow>.CopyTo(QuantityRevisionRow[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<QuantityRevisionRow>.Remove(QuantityRevisionRow item) => throw new NotSupportedException();
        }

        private sealed class CountedSequence : IReadOnlyCollection<QuantityRevisionRow>
        {
            private readonly IReadOnlyList<QuantityRevisionRow> _items;

            internal CountedSequence(int count, IReadOnlyList<QuantityRevisionRow> items)
            {
                Count = count;
                _items = items;
            }

            public int Count { get; }

            public IEnumerator<QuantityRevisionRow> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
