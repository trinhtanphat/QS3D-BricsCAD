using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarProcurementCsvCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GrowthRejectsBeforeUnexpectedCurrentRead();
            ShrinkRejectsBeforeSecondCurrentRead();
            CurrentDriftWinsBeforeNullRowValidation();
            UnderYieldRejectsAgainstAdmittedCount();
            ConflictingInterfacesRejectBeforeEnumeration();
            OversizedKnownCountRejectsBeforeEnumeration();
            StableKnownCountPreservesOutput();
            PureStreamingSourceRemainsSupported();
        }

        private static void GrowthRejectsBeforeUnexpectedCurrentRead()
        {
            var row = CanonicalSummary();
            var source = new HostileCollection(new[] { row, row }, 1, 1, 1, mutateAfterCurrent: 1, mutatedCount: 2);
            ThrowsCountIntegrity(() => RebarProcurementCsvExporter.ToCsv(source));
            Equal(1, source.CurrentReads);
        }

        private static void ShrinkRejectsBeforeSecondCurrentRead()
        {
            var row = CanonicalSummary();
            var source = new HostileCollection(new[] { row, row }, 2, 2, 2, mutateAfterCurrent: 1, mutatedCount: 1);
            ThrowsCountIntegrity(() => RebarProcurementCsvExporter.ToCsv(source));
            Equal(1, source.CurrentReads);
        }

        private static void CurrentDriftWinsBeforeNullRowValidation()
        {
            var source = new HostileCollection(new RebarProcurementSummary[] { null! }, 1, 1, 1, mutateAfterCurrent: 1, mutatedCount: 2);
            ThrowsCountIntegrity(() => RebarProcurementCsvExporter.ToCsv(source));
            Equal(1, source.CurrentReads);
            Equal(1, source.MoveNextCalls);
        }

        private static void UnderYieldRejectsAgainstAdmittedCount()
        {
            var source = new HostileCollection(new[] { CanonicalSummary() }, 2, 2, 2);
            ThrowsCountIntegrity(() => RebarProcurementCsvExporter.ToCsv(source));
            Equal(1, source.CurrentReads);
        }

        private static void ConflictingInterfacesRejectBeforeEnumeration()
        {
            var source = new HostileCollection(new[] { CanonicalSummary() }, 1, 2, 1);
            ThrowsCountIntegrity(() => RebarProcurementCsvExporter.ToCsv(source));
            Equal(0, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void OversizedKnownCountRejectsBeforeEnumeration()
        {
            var source = new HostileCollection(new[] { CanonicalSummary() }, 10001, 10001, 10001);
            Throws<ArgumentOutOfRangeException>(() => RebarProcurementCsvExporter.ToCsv(source));
            Equal(0, source.MoveNextCalls);
        }

        private static void StableKnownCountPreservesOutput()
        {
            var row = CanonicalSummary();
            var expected = RebarProcurementCsvExporter.ToCsv(new[] { row, row });
            var source = new HostileCollection(new[] { row, row }, 2, 2, 2);
            var actual = RebarProcurementCsvExporter.ToCsv(source);
            True(string.Equals(expected, actual, StringComparison.Ordinal));
            Equal(2, source.CurrentReads);
        }

        private static void PureStreamingSourceRemainsSupported()
        {
            var row = CanonicalSummary();
            var actual = RebarProcurementCsvExporter.ToCsv(Stream(row, row));
            Equal(3, actual.Count(ch => ch == '\n'));
        }

        private static IEnumerable<RebarProcurementSummary> Stream(params RebarProcurementSummary[] rows)
        {
            foreach (var row in rows) yield return row;
        }

        private static RebarProcurementSummary CanonicalSummary()
        {
            var demand = new RebarStockDemand(
                "COUNT-G1",
                "CB400",
                16d,
                12d,
                new[] { new RebarCutRequirement("COUNT-C1", 3d, 1) },
                new RebarCutAllowancePolicy());
            return RebarProcurementReportBuilder.Build(new[] { RebarCuttingOptimizer.Plan(demand) })[0];
        }

        private static void ThrowsCountIntegrity(Action action)
        {
            try { action(); }
            catch (InvalidOperationException ex)
            {
                True(ex.Message.IndexOf("Count", StringComparison.Ordinal) >= 0);
                return;
            }
            throw new InvalidOperationException("Expected rebar procurement CSV Count-integrity rejection.");
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

        private sealed class HostileCollection : ICollection<RebarProcurementSummary>, IReadOnlyCollection<RebarProcurementSummary>, ICollection
        {
            private readonly RebarProcurementSummary[] _rows;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly int _mutateAfterCurrent;
            private readonly int _mutatedCount;
            private bool _mutated;

            internal HostileCollection(
                RebarProcurementSummary[] rows,
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                int mutateAfterCurrent = int.MaxValue,
                int mutatedCount = 0)
            {
                _rows = rows;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _mutateAfterCurrent = mutateAfterCurrent;
                _mutatedCount = mutatedCount;
            }

            int ICollection<RebarProcurementSummary>.Count => _mutated ? _mutatedCount : _genericCount;
            int IReadOnlyCollection<RebarProcurementSummary>.Count => _mutated ? _mutatedCount : _readOnlyCount;
            int ICollection.Count => _mutated ? _mutatedCount : _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<RebarProcurementSummary> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(RebarProcurementSummary item) => Array.IndexOf(_rows, item) >= 0;
            public void CopyTo(RebarProcurementSummary[] array, int arrayIndex) => _rows.CopyTo(array, arrayIndex);
            void ICollection.CopyTo(Array array, int index) => _rows.CopyTo(array, index);
            public void Add(RebarProcurementSummary item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(RebarProcurementSummary item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<RebarProcurementSummary>
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

                public RebarProcurementSummary Current
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
