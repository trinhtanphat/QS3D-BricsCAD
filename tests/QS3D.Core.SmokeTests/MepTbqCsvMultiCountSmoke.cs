using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepTbqCsvMultiCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ConflictingGenericCountRejectsBeforeIndexerRead();
            ConflictingNonGenericCountRejectsBeforeIndexerRead();
            IndexerInducedGenericCountDriftRejectsBeforeRowAcceptance();
            StableThreeChannelRowsRemainAccepted();
        }

        private static void ConflictingGenericCountRejectsBeforeIndexerRead()
        {
            var source = new MultiCountRows(OneRow("GEN-CONFLICT"), 1, 2, 1, false);
            ThrowsCountIntegrity(() => new MepTbqProjectionService().SerializeCsv(source));
            Require(source.IndexerReads == 0,
                "Conflicting generic MEP/TBQ CSV Count evidence must reject before any row indexer read.");
        }

        private static void ConflictingNonGenericCountRejectsBeforeIndexerRead()
        {
            var source = new MultiCountRows(OneRow("NONGEN-CONFLICT"), 1, 1, 2, false);
            ThrowsCountIntegrity(() => new MepTbqProjectionService().SerializeCsv(source));
            Require(source.IndexerReads == 0,
                "Conflicting non-generic MEP/TBQ CSV Count evidence must reject before any row indexer read.");
        }

        private static void IndexerInducedGenericCountDriftRejectsBeforeRowAcceptance()
        {
            var source = new MultiCountRows(OneRow("DRIFT"), 1, 1, 1, true);
            ThrowsCountIntegrity(() => new MepTbqProjectionService().SerializeCsv(source));
            Require(source.IndexerReads == 1,
                "MEP/TBQ CSV secondary Count drift must reject immediately after the one admitted indexer read.");
        }

        private static void StableThreeChannelRowsRemainAccepted()
        {
            var row = OneRow("STABLE");
            var source = new MultiCountRows(row, 1, 1, 1, false);
            var service = new MepTbqProjectionService();
            var expected = service.SerializeCsv((IReadOnlyList<MepTbqReportRow>)new[] { row });
            var actual = service.SerializeCsv(source);
            Require(string.Equals(expected, actual, StringComparison.Ordinal),
                "Stable three-channel MEP/TBQ CSV rows must preserve deterministic serialization.");
            Require(source.IndexerReads == 1,
                "Stable three-channel MEP/TBQ CSV rows must read each admitted row exactly once.");
        }

        private static MepTbqReportRow OneRow(string suffix)
        {
            var groups = new MepQuantityService().Aggregate(new[]
            {
                new MepElement("MC-" + suffix, MepElementKind.Pipe, "CHW", "DN50-" + suffix, "L01", 1, 1d)
            });
            return new MepTbqProjectionService().BuildReport(groups)[0];
        }

        private static void ThrowsCountIntegrity(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                Require(ex.Message.IndexOf("Count", StringComparison.Ordinal) >= 0,
                    "Unexpected MEP/TBQ CSV multi-Count error: " + ex.Message);
                return;
            }
            throw new InvalidOperationException("Expected MEP/TBQ CSV multi-Count integrity rejection.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class MultiCountRows :
            IReadOnlyList<MepTbqReportRow>,
            ICollection<MepTbqReportRow>,
            ICollection
        {
            private readonly MepTbqReportRow _row;
            private readonly int _readOnlyCount;
            private readonly int _genericAdmissionCount;
            private readonly int _nonGenericCount;
            private readonly bool _driftGenericAfterIndexer;
            private bool _indexerObserved;

            internal MultiCountRows(
                MepTbqReportRow row,
                int readOnlyCount,
                int genericCount,
                int nonGenericCount,
                bool driftGenericAfterIndexer)
            {
                _row = row;
                _readOnlyCount = readOnlyCount;
                _genericAdmissionCount = genericCount;
                _nonGenericCount = nonGenericCount;
                _driftGenericAfterIndexer = driftGenericAfterIndexer;
            }

            public int Count => _readOnlyCount;
            int ICollection<MepTbqReportRow>.Count =>
                _driftGenericAfterIndexer && _indexerObserved ? _genericAdmissionCount + 1 : _genericAdmissionCount;
            int ICollection.Count => _nonGenericCount;

            internal int IndexerReads { get; private set; }
            public MepTbqReportRow this[int index]
            {
                get
                {
                    IndexerReads++;
                    _indexerObserved = true;
                    if (index != 0) throw new InvalidOperationException("Unexpected MEP/TBQ CSV multi-Count indexer read.");
                    return _row;
                }
            }

            public IEnumerator<MepTbqReportRow> GetEnumerator()
            {
                yield return _row;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            bool ICollection<MepTbqReportRow>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            void ICollection<MepTbqReportRow>.Add(MepTbqReportRow item) => throw new NotSupportedException();
            void ICollection<MepTbqReportRow>.Clear() => throw new NotSupportedException();
            bool ICollection<MepTbqReportRow>.Contains(MepTbqReportRow item) => ReferenceEquals(_row, item);
            void ICollection<MepTbqReportRow>.CopyTo(MepTbqReportRow[] array, int arrayIndex) => array[arrayIndex] = _row;
            bool ICollection<MepTbqReportRow>.Remove(MepTbqReportRow item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_row, index);
        }
    }
}
