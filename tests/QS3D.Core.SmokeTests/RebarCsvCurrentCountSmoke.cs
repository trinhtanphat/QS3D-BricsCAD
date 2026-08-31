using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarCsvCurrentCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CurrentInducedCountDriftWinsBeforeNullRowSemantics();
            StableCurrentIsReadExactlyOnce();
        }

        private static void CurrentInducedCountDriftWinsBeforeNullRowSemantics()
        {
            var rows = new CurrentMutatesCountRows();
            try
            {
                RebarCsvExporter.ToCsv(rows);
            }
            catch (InvalidOperationException error)
            {
                if (error.Message.IndexOf("Count changed during serialization", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("BBS Current-induced Count drift returned the wrong integrity diagnostic: " + error.Message, error);
                if (rows.CurrentReads != 1)
                    throw new InvalidOperationException("BBS hostile Current must be observed exactly once. CurrentReads=" + rows.CurrentReads + ".");
                return;
            }
            catch (Exception error)
            {
                throw new InvalidOperationException(
                    "BBS Current-induced Count drift must be rejected before null-row semantics. Actual=" + error.GetType().Name + ": " + error.Message,
                    error);
            }

            throw new InvalidOperationException("BBS Current-induced Count drift must be rejected.");
        }

        private static void StableCurrentIsReadExactlyOnce()
        {
            var rows = new StableCountRows(ValidRow());
            var csv = RebarCsvExporter.ToCsv(rows);
            if (rows.CurrentReads != 1)
                throw new InvalidOperationException("Stable BBS row Current must be read exactly once. CurrentReads=" + rows.CurrentReads + ".");
            if (csv.IndexOf("\"BBS-CURRENT-STABLE\"", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Stable BBS Current control did not serialize the expected element identity.");
        }

        private static RebarScheduleRow ValidRow()
        {
            return new RebarScheduleRow
            {
                ElementId = "BBS-CURRENT-STABLE",
                BarMark = "B1",
                ShapeCode = "00",
                Notation = "1D16",
                DiameterMm = 16d,
                Quantity = 1,
                CuttingLengthM = 1d,
                TotalLengthM = 1d,
                UnitWeightKgM = 1d,
                NetWeightKg = 1d,
                WastePercent = 0d,
                TotalWeightKg = 1d
            };
        }

        private sealed class CurrentMutatesCountRows : ICollection<RebarScheduleRow>
        {
            private int _count = 1;
            internal int CurrentReads { get; private set; }
            public int Count => _count;
            public bool IsReadOnly => true;

            public IEnumerator<RebarScheduleRow> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<RebarScheduleRow>.Add(RebarScheduleRow item) => throw new NotSupportedException();
            void ICollection<RebarScheduleRow>.Clear() => throw new NotSupportedException();
            bool ICollection<RebarScheduleRow>.Contains(RebarScheduleRow item) => false;
            void ICollection<RebarScheduleRow>.CopyTo(RebarScheduleRow[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<RebarScheduleRow>.Remove(RebarScheduleRow item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<RebarScheduleRow>
            {
                private readonly CurrentMutatesCountRows _owner;
                private bool _moved;

                internal Enumerator(CurrentMutatesCountRows owner) => _owner = owner;

                public RebarScheduleRow Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._count = 2;
                        return null!;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_moved) return false;
                    _moved = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StableCountRows : ICollection<RebarScheduleRow>
        {
            private readonly RebarScheduleRow _row;
            internal int CurrentReads { get; private set; }
            public int Count => 1;
            public bool IsReadOnly => true;

            internal StableCountRows(RebarScheduleRow row) => _row = row;

            public IEnumerator<RebarScheduleRow> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<RebarScheduleRow>.Add(RebarScheduleRow item) => throw new NotSupportedException();
            void ICollection<RebarScheduleRow>.Clear() => throw new NotSupportedException();
            bool ICollection<RebarScheduleRow>.Contains(RebarScheduleRow item) => ReferenceEquals(item, _row);
            void ICollection<RebarScheduleRow>.CopyTo(RebarScheduleRow[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<RebarScheduleRow>.Remove(RebarScheduleRow item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<RebarScheduleRow>
            {
                private readonly StableCountRows _owner;
                private bool _moved;

                internal Enumerator(StableCountRows owner) => _owner = owner;

                public RebarScheduleRow Current
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
                    if (_moved) return false;
                    _moved = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
