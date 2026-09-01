using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportTotalsTransientCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectTransientCountGrowthBeforeNextMoveNext();
            RejectTransientCountShrinkBeforeNextMoveNext();
            RejectTransientNegativeCountBeforeNextMoveNext();
            RejectTransientConflictingCountsBeforeNextMoveNext();
            AcceptStableCountedRows();
        }

        private static void RejectTransientCountGrowthBeforeNextMoveNext()
        {
            var source = new TransientCountRows(TransientCountMode.Grow);
            ExpectInvalid(() => QuantityReportTotals.FromRows(source), "Count changed during enumeration", "Transient Count growth must fail before the next MoveNext.");
            AssertStopsBeforeSecondMoveNext(source, "growth");
        }

        private static void RejectTransientCountShrinkBeforeNextMoveNext()
        {
            var source = new TransientCountRows(TransientCountMode.Shrink);
            ExpectInvalid(() => QuantityReportTotals.FromRows(source), "Count changed during enumeration", "Transient Count shrink must fail before the next MoveNext.");
            AssertStopsBeforeSecondMoveNext(source, "shrink");
        }

        private static void RejectTransientNegativeCountBeforeNextMoveNext()
        {
            var source = new TransientCountRows(TransientCountMode.Negative);
            ExpectInvalid(() => QuantityReportTotals.FromRows(source), "negative known count", "Transient negative Count must fail before the next MoveNext.");
            AssertStopsBeforeSecondMoveNext(source, "negative");
        }

        private static void RejectTransientConflictingCountsBeforeNextMoveNext()
        {
            var source = new TransientCountRows(TransientCountMode.Conflict);
            ExpectInvalid(() => QuantityReportTotals.FromRows(source), "conflicting known counts", "Transient conflicting Count surfaces must fail before the next MoveNext.");
            AssertStopsBeforeSecondMoveNext(source, "conflict");
        }

        private static void AcceptStableCountedRows()
        {
            var source = new TransientCountRows(TransientCountMode.None);
            var totals = QuantityReportTotals.FromRows(source);
            if (totals.Count != 3 || totals.GrossConcreteM3 != 3d || totals.NetConcreteM3 != 3d || totals.FormworkM2 != 3d)
                throw new InvalidOperationException("Stable counted rows must preserve ordinary quantity totals.");
            if (source.MoveNextCalls != 3 || source.CurrentReads != 2)
                throw new InvalidOperationException("Stable counted rows must traverse exactly two rows plus terminal MoveNext.");
        }

        private static void AssertStopsBeforeSecondMoveNext(TransientCountRows source, string label)
        {
            if (source.MoveNextCalls != 1 || source.CurrentReads != 1)
                throw new InvalidOperationException(
                    "Transient Count " + label + " must be detected after first Current and before second MoveNext. MoveNextCalls=" +
                    source.MoveNextCalls + ", CurrentReads=" + source.CurrentReads + ".");
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
            try
            {
                action();
            }
            catch (InvalidOperationException error)
            {
                if (error.Message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException(failure + " Actual diagnostic: " + error.Message, error);
            }

            throw new InvalidOperationException(failure);
        }

        private enum TransientCountMode
        {
            None,
            Grow,
            Shrink,
            Negative,
            Conflict
        }

        private sealed class TransientCountRows : ICollection<QuantityReportRow>, IReadOnlyCollection<QuantityReportRow>, ICollection
        {
            private readonly QuantityReportRow[] _rows = { Row(1), Row(2) };
            private readonly TransientCountMode _mode;
            private bool _transient;
            private bool _mutationArmed = true;

            internal TransientCountRows(TransientCountMode mode)
            {
                _mode = mode;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            int ICollection<QuantityReportRow>.Count => GenericCount;
            int IReadOnlyCollection<QuantityReportRow>.Count => ReadOnlyCount;
            int ICollection.Count => NonGenericCount;
            bool ICollection<QuantityReportRow>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            private int GenericCount => TransientCount(surface: 0);
            private int ReadOnlyCount => TransientCount(surface: 1);
            private int NonGenericCount => TransientCount(surface: 2);

            private int TransientCount(int surface)
            {
                if (!_transient) return 2;
                switch (_mode)
                {
                    case TransientCountMode.Grow:
                        return 3;
                    case TransientCountMode.Shrink:
                        return 1;
                    case TransientCountMode.Negative:
                        return -1;
                    case TransientCountMode.Conflict:
                        return surface == 1 ? 3 : 2;
                    default:
                        return 2;
                }
            }

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
                private readonly TransientCountRows _owner;
                private int _index = -1;

                internal Enumerator(TransientCountRows owner)
                {
                    _owner = owner;
                }

                public QuantityReportRow Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index == 0 && _owner._mutationArmed && _owner._mode != TransientCountMode.None)
                        {
                            _owner._mutationArmed = false;
                            _owner._transient = true;
                        }
                        return _owner._rows[_index];
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_owner._transient)
                        _owner._transient = false;
                    _index++;
                    return _index < _owner._rows.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
