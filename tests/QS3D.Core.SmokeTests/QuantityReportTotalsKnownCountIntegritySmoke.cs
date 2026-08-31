using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportTotalsKnownCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectKnownCountOverrunBeforeCurrent();
            RejectKnownCountUnderYield();
            RejectPostTraversalCountDrift();
            RejectPostTraversalNegativeCount();
            RejectPostTraversalCountConflict();
            AcceptStableMultiInterfaceCount();
            AcceptPureStreamingRows();
        }

        private static void RejectKnownCountOverrunBeforeCurrent()
        {
            var source = new CurrentCountingReadOnlyRows(actualCount: 2, reportedCount: 1, throwOnUnexpectedCurrent: true);
            ExpectInvalid(() => QuantityReportTotals.FromRows(source), "enumeration produced 2", "Count=1 must reject row 2 before reading Current.");
            if (source.MoveNextCalls != 2 || source.CurrentReads != 1)
                throw new InvalidOperationException("Quantity totals known-Count overrun must stop at N+1 MoveNext without reading N+1 Current.");
        }

        private static void RejectKnownCountUnderYield()
        {
            var source = new MultiCountRows(new[] { Row(1) }, 2, 2, 2);
            ExpectInvalid(() => QuantityReportTotals.FromRows(source), "enumeration produced 1", "Quantity totals must reject Count=2 with one yielded row.");
        }

        private static void RejectPostTraversalCountDrift()
        {
            var source = new MultiCountRows(new[] { Row(1) }, 1, 1, 1, finalGeneric: 2, finalReadOnly: 2, finalNonGeneric: 2);
            ExpectInvalid(() => QuantityReportTotals.FromRows(source), "Count changed during enumeration", "Quantity totals must reject post-traversal Count drift.");
        }

        private static void RejectPostTraversalNegativeCount()
        {
            var source = new MultiCountRows(new[] { Row(1) }, 1, 1, 1, finalGeneric: -1, finalReadOnly: -1, finalNonGeneric: -1);
            ExpectInvalid(() => QuantityReportTotals.FromRows(source), "negative known count", "Quantity totals must reject negative Count evidence rebound after traversal.");
        }

        private static void RejectPostTraversalCountConflict()
        {
            var source = new MultiCountRows(new[] { Row(1) }, 1, 1, 1, finalGeneric: 1, finalReadOnly: 2, finalNonGeneric: 1);
            ExpectInvalid(() => QuantityReportTotals.FromRows(source), "conflicting known counts", "Quantity totals must reject conflicting Count evidence rebound after traversal.");
        }

        private static void AcceptStableMultiInterfaceCount()
        {
            var source = new MultiCountRows(new[] { Row(2), Row(3) }, 2, 2, 2);
            var totals = QuantityReportTotals.FromRows(source);
            if (totals.Count != 5 || totals.GrossConcreteM3 != 5d)
                throw new InvalidOperationException("Stable multi-interface counted rows must preserve ordinary quantity totals.");
        }

        private static void AcceptPureStreamingRows()
        {
            var totals = QuantityReportTotals.FromRows(StreamRows());
            if (totals.Count != 3 || totals.GrossConcreteM3 != 3d)
                throw new InvalidOperationException("Pure streaming quantity-report rows must remain supported.");
        }

        private static IEnumerable<QuantityReportRow> StreamRows()
        {
            yield return Row(1);
            yield return Row(2);
        }

        private static QuantityReportRow Row(int value)
        {
            return new QuantityReportRow
            {
                Count = value,
                GrossConcreteM3 = value,
                DeductionM3 = 0d,
                NetConcreteM3 = value,
                FormworkM2 = value,
                LengthM = value,
                DoorAreaM2 = value
            };
        }

        private static void ExpectInvalid(Action action, string fragment, string failure)
        {
            try { action(); }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(failure + " Actual diagnostic: " + ex.Message, ex);
                return;
            }
            throw new InvalidOperationException(failure);
        }

        private sealed class MultiCountRows : ICollection<QuantityReportRow>, IReadOnlyCollection<QuantityReportRow>, ICollection
        {
            private readonly QuantityReportRow[] _rows;
            private readonly int _generic;
            private readonly int _readOnly;
            private readonly int _nonGeneric;
            private readonly int? _finalGeneric;
            private readonly int? _finalReadOnly;
            private readonly int? _finalNonGeneric;

            internal MultiCountRows(QuantityReportRow[] rows, int generic, int readOnly, int nonGeneric, int? finalGeneric = null, int? finalReadOnly = null, int? finalNonGeneric = null)
            {
                _rows = rows;
                _generic = generic;
                _readOnly = readOnly;
                _nonGeneric = nonGeneric;
                _finalGeneric = finalGeneric;
                _finalReadOnly = finalReadOnly;
                _finalNonGeneric = finalNonGeneric;
            }

            internal bool TraversalCompleted { get; private set; }
            int ICollection<QuantityReportRow>.Count => TraversalCompleted && _finalGeneric.HasValue ? _finalGeneric.Value : _generic;
            int IReadOnlyCollection<QuantityReportRow>.Count => TraversalCompleted && _finalReadOnly.HasValue ? _finalReadOnly.Value : _readOnly;
            int ICollection.Count => TraversalCompleted && _finalNonGeneric.HasValue ? _finalNonGeneric.Value : _nonGeneric;
            bool ICollection<QuantityReportRow>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            public IEnumerator<QuantityReportRow> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<QuantityReportRow>.Add(QuantityReportRow item) => throw new NotSupportedException();
            void ICollection<QuantityReportRow>.Clear() => throw new NotSupportedException();
            bool ICollection<QuantityReportRow>.Contains(QuantityReportRow item) => Array.IndexOf(_rows, item) >= 0;
            void ICollection<QuantityReportRow>.CopyTo(QuantityReportRow[] array, int arrayIndex) => _rows.CopyTo(array, arrayIndex);
            bool ICollection<QuantityReportRow>.Remove(QuantityReportRow item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _rows.CopyTo(array, index);

            private sealed class Enumerator : IEnumerator<QuantityReportRow>
            {
                private readonly MultiCountRows _owner;
                private int _index = -1;
                internal Enumerator(MultiCountRows owner) { _owner = owner; }
                public QuantityReportRow Current => _owner._rows[_index];
                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _index++;
                    if (_index < _owner._rows.Length) return true;
                    _owner.TraversalCompleted = true;
                    return false;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class CurrentCountingReadOnlyRows : IReadOnlyCollection<QuantityReportRow>
        {
            private readonly int _actualCount;
            private readonly bool _throwOnUnexpectedCurrent;
            internal CurrentCountingReadOnlyRows(int actualCount, int reportedCount, bool throwOnUnexpectedCurrent)
            {
                _actualCount = actualCount;
                Count = reportedCount;
                _throwOnUnexpectedCurrent = throwOnUnexpectedCurrent;
            }
            public int Count { get; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<QuantityReportRow> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<QuantityReportRow>
            {
                private readonly CurrentCountingReadOnlyRows _owner;
                private int _index = -1;
                internal Enumerator(CurrentCountingReadOnlyRows owner) { _owner = owner; }
                public QuantityReportRow Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._throwOnUnexpectedCurrent && _owner.CurrentReads > _owner.Count)
                            throw new InvalidOperationException("Unexpected QuantityReportTotals Current read beyond admitted Count.");
                        return Row(1);
                    }
                }
                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._actualCount;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
