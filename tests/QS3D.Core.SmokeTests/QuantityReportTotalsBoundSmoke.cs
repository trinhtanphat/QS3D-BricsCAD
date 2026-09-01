using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportTotalsBoundSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectOverLimitKnownCountBeforeEnumeration();
            RejectFirstStreamingRowBeyondLimitBeforeCurrent();
            AcceptExactStreamingLimit();
        }

        private static void RejectOverLimitKnownCountBeforeEnumeration()
        {
            var source = new OverLimitKnownCountRows(10001);
            ExpectInvalid(
                () => QuantityReportTotals.FromRows(source),
                "at most 10000 rows",
                "Over-limit known Count must fail at admission.");
            if (source.GetEnumeratorCalls != 0)
                throw new InvalidOperationException("Over-limit known Count must fail before enumeration starts.");
        }

        private static void RejectFirstStreamingRowBeyondLimitBeforeCurrent()
        {
            var source = new StreamingRows(10001);
            ExpectInvalid(
                () => QuantityReportTotals.FromRows(source),
                "at most 10000 rows",
                "Streaming input must fail on the first row beyond the supported bound.");
            if (source.MoveNextCalls != 10001 || source.CurrentReads != 10000)
            {
                throw new InvalidOperationException(
                    "Streaming overflow must be detected after MoveNext exposes row 10001 and before Current is read. MoveNextCalls=" +
                    source.MoveNextCalls + ", CurrentReads=" + source.CurrentReads + ".");
            }
        }

        private static void AcceptExactStreamingLimit()
        {
            var source = new StreamingRows(10000);
            var totals = QuantityReportTotals.FromRows(source);
            if (totals.Count != 10000 || totals.GrossConcreteM3 != 10000d || totals.NetConcreteM3 != 10000d)
                throw new InvalidOperationException("Exactly 10000 streaming rows must preserve ordinary totals.");
            if (source.MoveNextCalls != 10001 || source.CurrentReads != 10000)
                throw new InvalidOperationException("Exact-bound streaming input must enumerate 10000 rows plus terminal MoveNext exactly once.");
        }

        private static QuantityReportRow Row()
        {
            return new QuantityReportRow
            {
                Count = 1,
                GrossConcreteM3 = 1d,
                DeductionM3 = 0d,
                NetConcreteM3 = 1d,
                FormworkM2 = 0d,
                LengthM = 0d,
                DoorAreaM2 = 0d
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

        private sealed class OverLimitKnownCountRows : ICollection<QuantityReportRow>
        {
            private readonly int _count;

            internal OverLimitKnownCountRows(int count)
            {
                _count = count;
            }

            internal int GetEnumeratorCalls { get; private set; }
            public int Count => _count;
            public bool IsReadOnly => true;

            public IEnumerator<QuantityReportRow> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Enumeration must not start for an over-limit known Count.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<QuantityReportRow>.Add(QuantityReportRow item) => throw new NotSupportedException();
            void ICollection<QuantityReportRow>.Clear() => throw new NotSupportedException();
            bool ICollection<QuantityReportRow>.Contains(QuantityReportRow item) => false;
            void ICollection<QuantityReportRow>.CopyTo(QuantityReportRow[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<QuantityReportRow>.Remove(QuantityReportRow item) => throw new NotSupportedException();
        }

        private sealed class StreamingRows : IEnumerable<QuantityReportRow>
        {
            private readonly int _rowCount;
            private readonly QuantityReportRow _row = Row();

            internal StreamingRows(int rowCount)
            {
                _rowCount = rowCount;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<QuantityReportRow> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<QuantityReportRow>
            {
                private readonly StreamingRows _owner;
                private int _index = -1;

                internal Enumerator(StreamingRows owner)
                {
                    _owner = owner;
                }

                public QuantityReportRow Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._row;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._rowCount;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
