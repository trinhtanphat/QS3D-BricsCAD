using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BbsCsvCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GrowthRejectsBeforeUnexpectedCurrentRead();
            ShrinkRejectsBeforeSecondCurrentRead();
            UnderYieldRejectsAgainstAdmittedCount();
            ConflictingInterfacesRejectBeforeEnumeration();
            OversizedKnownCountRejectsBeforeEnumeration();
            RowMutationRejectsAfterTraversalBeforeProjection();
            StableKnownCountPreservesOutput();
            PureStreamingSourceRemainsSupported();
        }

        private static void GrowthRejectsBeforeUnexpectedCurrentRead()
        {
            var row = CanonicalRow();
            var source = new HostileCollection(new[] { row, row }, 1, 1, 1, mutateAfterCurrent: 1, mutatedCount: 2);
            ThrowsCountIntegrity(() => RebarCsvExporter.ToCsv(source));
            Equal(1, source.CurrentReads);
        }

        private static void ShrinkRejectsBeforeSecondCurrentRead()
        {
            var row = CanonicalRow();
            var source = new HostileCollection(new[] { row, row }, 2, 2, 2, mutateAfterCurrent: 1, mutatedCount: 1);
            ThrowsCountIntegrity(() => RebarCsvExporter.ToCsv(source));
            Equal(1, source.CurrentReads);
        }

        private static void UnderYieldRejectsAgainstAdmittedCount()
        {
            var source = new HostileCollection(new[] { CanonicalRow() }, 2, 2, 2);
            ThrowsCountIntegrity(() => RebarCsvExporter.ToCsv(source));
            Equal(1, source.CurrentReads);
        }

        private static void ConflictingInterfacesRejectBeforeEnumeration()
        {
            var source = new HostileCollection(new[] { CanonicalRow() }, 1, 2, 1);
            ThrowsCountIntegrity(() => RebarCsvExporter.ToCsv(source));
            Equal(0, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void OversizedKnownCountRejectsBeforeEnumeration()
        {
            var source = new HostileCollection(new[] { CanonicalRow() }, 10001, 10001, 10001);
            Throws<ArgumentOutOfRangeException>(() => RebarCsvExporter.ToCsv(source));
            Equal(0, source.MoveNextCalls);
        }

        private static void RowMutationRejectsAfterTraversalBeforeProjection()
        {
            var first = CanonicalRow();
            first.BarMark = "ORIGINAL";
            var second = CanonicalRow();
            second.ElementId = "COUNT-E2";
            second.BarMark = "COUNT-B2";
            var source = new RowMutatingEnumerable(first, second);

            ThrowsRowIntegrity(() => RebarCsvExporter.ToCsv(source));
            Equal("MUTATED", first.BarMark);
            Equal(2, source.CurrentReads);
        }

        private static void StableKnownCountPreservesOutput()
        {
            var row = CanonicalRow();
            var expected = RebarCsvExporter.ToCsv(new[] { row, row });
            var source = new HostileCollection(new[] { row, row }, 2, 2, 2);
            var actual = RebarCsvExporter.ToCsv(source);
            True(string.Equals(expected, actual, StringComparison.Ordinal));
            Equal(2, source.CurrentReads);
        }

        private static void PureStreamingSourceRemainsSupported()
        {
            var actual = RebarCsvExporter.ToCsv(Stream(CanonicalRow(), CanonicalRow()));
            Equal(3, actual.Count(ch => ch == '\n'));
        }

        private static IEnumerable<RebarScheduleRow> Stream(params RebarScheduleRow[] rows)
        {
            foreach (var row in rows) yield return row;
        }

        private static RebarScheduleRow CanonicalRow()
        {
            return new RebarScheduleRow
            {
                ElementId = "COUNT-E1",
                BarMark = "COUNT-B1",
                ShapeCode = "00",
                Notation = "1D16",
                DiameterMm = 16d,
                Quantity = 1,
                CuttingLengthM = 2d,
                TotalLengthM = 2d,
                UnitWeightKgM = 1d,
                NetWeightKg = 2d,
                WastePercent = 0d,
                TotalWeightKg = 2d,
                FabricationStatus = "Approved",
                FabricationStandardCode = "STD",
                FabricationDetailingRevision = "REV"
            };
        }

        private static void ThrowsCountIntegrity(Action action)
        {
            try { action(); }
            catch (InvalidOperationException ex)
            {
                True(ex.Message.IndexOf("Count", StringComparison.Ordinal) >= 0);
                return;
            }
            throw new InvalidOperationException("Expected BBS CSV Count-integrity rejection.");
        }

        private static void ThrowsRowIntegrity(Action action)
        {
            try { action(); }
            catch (InvalidOperationException ex)
            {
                True(ex.Message.IndexOf("row values changed", StringComparison.OrdinalIgnoreCase) >= 0);
                return;
            }
            throw new InvalidOperationException("Expected BBS CSV row-stability rejection.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("Expected true.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }

        private sealed class RowMutatingEnumerable : IEnumerable<RebarScheduleRow>
        {
            private readonly RebarScheduleRow _first;
            private readonly RebarScheduleRow _second;

            internal RowMutatingEnumerable(RebarScheduleRow first, RebarScheduleRow second)
            {
                _first = first;
                _second = second;
            }

            internal int CurrentReads { get; private set; }

            public IEnumerator<RebarScheduleRow> GetEnumerator()
            {
                CurrentReads++;
                yield return _first;
                _first.BarMark = "MUTATED";
                CurrentReads++;
                yield return _second;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class HostileCollection : ICollection<RebarScheduleRow>, IReadOnlyCollection<RebarScheduleRow>, ICollection
        {
            private readonly RebarScheduleRow[] _rows;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly int _mutateAfterCurrent;
            private readonly int _mutatedCount;
            private bool _mutated;

            internal HostileCollection(RebarScheduleRow[] rows, int genericCount, int readOnlyCount, int nonGenericCount, int mutateAfterCurrent = int.MaxValue, int mutatedCount = 0)
            {
                _rows = rows;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _mutateAfterCurrent = mutateAfterCurrent;
                _mutatedCount = mutatedCount;
            }

            int ICollection<RebarScheduleRow>.Count => _mutated ? _mutatedCount : _genericCount;
            int IReadOnlyCollection<RebarScheduleRow>.Count => _mutated ? _mutatedCount : _readOnlyCount;
            int ICollection.Count => _mutated ? _mutatedCount : _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<RebarScheduleRow> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(RebarScheduleRow item) => Array.IndexOf(_rows, item) >= 0;
            public void CopyTo(RebarScheduleRow[] array, int arrayIndex) => _rows.CopyTo(array, arrayIndex);
            void ICollection.CopyTo(Array array, int index) => _rows.CopyTo(array, index);
            public void Add(RebarScheduleRow item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(RebarScheduleRow item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<RebarScheduleRow>
            {
                private readonly HostileCollection _owner;
                private int _index = -1;
                internal Enumerator(HostileCollection owner) => _owner = owner;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._rows.Length;
                }
                public RebarScheduleRow Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        var row = _owner._rows[_index];
                        if (_owner.CurrentReads >= _owner._mutateAfterCurrent) _owner._mutated = true;
                        return row;
                    }
                }
                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
