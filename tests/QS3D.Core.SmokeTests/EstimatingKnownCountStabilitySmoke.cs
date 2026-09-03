using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimatingKnownCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectPortfolioTransientGrowthBeforeSecondMoveNext();
            RejectSelectedLineTransientShrinkBeforeSecondMoveNext();
            RejectUnitRateTransientNegativeBeforeSecondMoveNext();
            RejectUnitRateTransientConflictBeforeSecondMoveNext();
            PreserveStableCountedInputs();
            PreserveStreamingInputs();
        }

        private static void RejectPortfolioTransientGrowthBeforeSecondMoveNext()
        {
            var source = new TransientCountCollection<EstimatingLine>(
                new[] { Line("L-1"), Line("L-2") }, TransientCountMode.Grow);

            ExpectInvalid(
                () => new EstimatingPortfolio(source),
                "known line count changed during enumeration",
                "portfolio transient growth");

            AssertStopsBeforeSecondMoveNext(source, "portfolio transient growth");
        }

        private static void RejectSelectedLineTransientShrinkBeforeSecondMoveNext()
        {
            var ids = new TransientCountCollection<string>(
                new[] { "L-1", "L-2" }, TransientCountMode.Shrink);

            ExpectInvalid(
                () => new BulkRateAssignmentRequest(
                    ids,
                    "CC-1",
                    "RATE-SOURCE",
                    "R1",
                    new[] { new UnitRateAssignment("m", 10m) }),
                "selected-line known count changed during enumeration",
                "selected-line transient shrink");

            AssertStopsBeforeSecondMoveNext(ids, "selected-line transient shrink");
        }

        private static void RejectUnitRateTransientNegativeBeforeSecondMoveNext()
        {
            var rates = new TransientCountCollection<UnitRateAssignment>(
                new[] { new UnitRateAssignment("m", 10m), new UnitRateAssignment("m2", 20m) },
                TransientCountMode.Negative);

            ExpectInvalid(
                () => new BulkRateAssignmentRequest(
                    new[] { "L-1" },
                    "CC-1",
                    "RATE-SOURCE",
                    "R1",
                    rates),
                "invalid negative unit-rate count",
                "unit-rate transient negative");

            AssertStopsBeforeSecondMoveNext(rates, "unit-rate transient negative");
        }

        private static void RejectUnitRateTransientConflictBeforeSecondMoveNext()
        {
            var rates = new TransientCountCollection<UnitRateAssignment>(
                new[] { new UnitRateAssignment("m", 10m), new UnitRateAssignment("m2", 20m) },
                TransientCountMode.Conflict);

            ExpectInvalid(
                () => new BulkRateAssignmentRequest(
                    new[] { "L-1" },
                    "CC-1",
                    "RATE-SOURCE",
                    "R1",
                    rates),
                "conflicting known unit-rate counts",
                "unit-rate transient conflict");

            AssertStopsBeforeSecondMoveNext(rates, "unit-rate transient conflict");
        }

        private static void PreserveStableCountedInputs()
        {
            var lines = new TransientCountCollection<EstimatingLine>(
                new[] { Line("L-2"), Line("L-1") }, TransientCountMode.None);
            var portfolio = new EstimatingPortfolio(lines);
            Equal(2, portfolio.Lines.Count, "stable portfolio count");
            Equal("L-1", portfolio.Lines[0].LineId, "stable portfolio ordering");
            Equal(6, lines.MoveNextCalls, "stable portfolio admission-plus-replay MoveNext calls");
            Equal(4, lines.CurrentReads, "stable portfolio admission-plus-replay Current reads");

            var ids = new TransientCountCollection<string>(new[] { "L-1", "L-2" }, TransientCountMode.None);
            var rates = new TransientCountCollection<UnitRateAssignment>(
                new[] { new UnitRateAssignment("m", 10m), new UnitRateAssignment("m2", 20m) },
                TransientCountMode.None);
            var request = new BulkRateAssignmentRequest(ids, "CC-1", "RATE-SOURCE", "R1", rates);
            Equal(2, request.LineIds.Count, "stable selected-line count");
            Equal(2, request.UnitRates.Count, "stable unit-rate count");
            Equal(6, ids.MoveNextCalls, "stable selected-line admission-plus-replay MoveNext calls");
            Equal(4, ids.CurrentReads, "stable selected-line admission-plus-replay Current reads");
            Equal(6, rates.MoveNextCalls, "stable unit-rate admission-plus-replay MoveNext calls");
            Equal(4, rates.CurrentReads, "stable unit-rate admission-plus-replay Current reads");
        }

        private static void PreserveStreamingInputs()
        {
            var portfolio = new EstimatingPortfolio(Stream(Line("L-1"), Line("L-2")));
            Equal(2, portfolio.Lines.Count, "streaming portfolio count");

            var request = new BulkRateAssignmentRequest(
                Stream("L-1", "L-2"),
                "CC-1",
                "RATE-SOURCE",
                "R1",
                Stream(new UnitRateAssignment("m", 10m), new UnitRateAssignment("m2", 20m)));
            Equal(2, request.LineIds.Count, "streaming selected-line count");
            Equal(2, request.UnitRates.Count, "streaming unit-rate count");
        }

        private static EstimatingLine Line(string id) => new EstimatingLine(id, "Q-1", "R1", 1m, "m");

        private static IEnumerable<T> Stream<T>(params T[] values)
        {
            for (var i = 0; i < values.Length; i++) yield return values[i];
        }

        private static void AssertStopsBeforeSecondMoveNext<T>(TransientCountCollection<T> source, string label)
        {
            Equal(1, source.MoveNextCalls, label + " MoveNext calls");
            Equal(1, source.CurrentReads, label + " Current reads");
        }

        private static void ExpectInvalid(Action action, string expectedFragment, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedFragment, StringComparison.Ordinal) >= 0) return;
                throw new Exception("EstimatingKnownCountStabilitySmoke " + label + " expected diagnostic containing '" + expectedFragment + "' but got '" + ex.Message + "'.");
            }
            throw new Exception("EstimatingKnownCountStabilitySmoke " + label + " expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("EstimatingKnownCountStabilitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private enum TransientCountMode
        {
            None,
            Grow,
            Shrink,
            Negative,
            Conflict,
        }

        private enum CountSurface
        {
            Generic,
            ReadOnly,
            NonGeneric,
        }

        private sealed class TransientCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly IReadOnlyList<T> _items;
            private readonly TransientCountMode _mode;
            private bool _transientArmed;

            internal TransientCountCollection(IReadOnlyList<T> items, TransientCountMode mode)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
                _mode = mode;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            int ICollection<T>.Count => ReadCount(CountSurface.Generic);
            int IReadOnlyCollection<T>.Count => ReadCount(CountSurface.ReadOnly);
            int ICollection.Count => ReadCount(CountSurface.NonGeneric);
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => throw new NotSupportedException();
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private int ReadCount(CountSurface surface)
            {
                if (!_transientArmed || _mode == TransientCountMode.None) return _items.Count;
                switch (_mode)
                {
                    case TransientCountMode.Grow:
                        return _items.Count + 1;
                    case TransientCountMode.Shrink:
                        return _items.Count - 1;
                    case TransientCountMode.Negative:
                        return -1;
                    case TransientCountMode.Conflict:
                        return surface == CountSurface.ReadOnly ? _items.Count + 2 : _items.Count + 1;
                    default:
                        return _items.Count;
                }
            }

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly TransientCountCollection<T> _owner;
                private int _index = -1;

                internal ProbeEnumerator(TransientCountCollection<T> owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_owner._transientArmed) _owner._transientArmed = false;
                    _index++;
                    return _index < _owner._items.Count;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index == 0 && _owner._mode != TransientCountMode.None) _owner._transientArmed = true;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
