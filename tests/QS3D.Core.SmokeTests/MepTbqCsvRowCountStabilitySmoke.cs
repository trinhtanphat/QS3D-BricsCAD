using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepTbqCsvRowCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            GrowthAfterFirstRowRejectsBeforeUnexpectedIndexerRead();
            ShrinkAfterFirstRowRejectsBeforeMissingIndexerRead();
            IndexerInducedCountDriftPreemptsNullRowValidation();
            PostTraversalCountDriftRejects();
            NegativeCountRejectsBeforeIndexerRead();
            OversizedCountRejectsBeforeIndexerRead();
            NullRowValidationRemainsInsideAdmittedCount();
            StableRowsSerializeDeterministically();
        }

        private static void GrowthAfterFirstRowRejectsBeforeUnexpectedIndexerRead()
        {
            var source = new MutableCountRows(new[] { OneRow("A"), OneRow("B") }, 1, 2, mutateAfterIndexerRead: 1);
            ThrowsCountIntegrity(() => new MepTbqProjectionService().SerializeCsv(source));
            Require(source.IndexerReads == 1,
                "MEP/TBQ CSV Count growth must reject before reading a row beyond the admitted Count.");
        }

        private static void ShrinkAfterFirstRowRejectsBeforeMissingIndexerRead()
        {
            var source = new MutableCountRows(new[] { OneRow("A"), OneRow("B") }, 2, 1, mutateAfterIndexerRead: 1);
            ThrowsCountIntegrity(() => new MepTbqProjectionService().SerializeCsv(source));
            Require(source.IndexerReads == 1,
                "MEP/TBQ CSV Count shrink must reject before reading an index outside the new source generation.");
        }

        private static void IndexerInducedCountDriftPreemptsNullRowValidation()
        {
            var source = new MutableCountRows(
                new MepTbqReportRow[] { null! },
                before: 1,
                after: 2,
                mutateAfterIndexerRead: 1);
            ThrowsCountIntegrity(() => new MepTbqProjectionService().SerializeCsv(source));
            Require(source.IndexerReads == 1,
                "MEP/TBQ CSV indexer-induced Count drift must fail after exactly one admitted indexer read.");
        }

        private static void PostTraversalCountDriftRejects()
        {
            var source = new FinalReadDriftRows(OneRow("A"));
            ThrowsCountIntegrity(() => new MepTbqProjectionService().SerializeCsv(source));
            Require(source.IndexerReads == 1,
                "MEP/TBQ CSV post-traversal Count drift must occur after exactly the admitted row was read.");
        }

        private static void NegativeCountRejectsBeforeIndexerRead()
        {
            var source = new FixedCountRows(-1, OneRow("A"));
            ThrowsCountIntegrity(() => new MepTbqProjectionService().SerializeCsv(source));
            Require(source.IndexerReads == 0,
                "Negative MEP/TBQ CSV Count evidence must reject before any row indexer read.");
        }

        private static void OversizedCountRejectsBeforeIndexerRead()
        {
            var source = new FixedCountRows(10001, OneRow("A"));
            Throws<InvalidOperationException>(
                () => new MepTbqProjectionService().SerializeCsv(source),
                "MEP/TBQ CSV oversized row Count");
            Require(source.IndexerReads == 0,
                "Oversized MEP/TBQ CSV Count evidence must reject before any row indexer read.");
        }

        private static void NullRowValidationRemainsInsideAdmittedCount()
        {
            var source = new FixedCountRows(1, new MepTbqReportRow[] { null! });
            Throws<ArgumentException>(
                () => new MepTbqProjectionService().SerializeCsv(source),
                "MEP/TBQ CSV null row validation");
            Require(source.IndexerReads == 1,
                "Null-row validation must still occur for the admitted MEP/TBQ CSV row.");
        }

        private static void StableRowsSerializeDeterministically()
        {
            var rows = new[] { OneRow("A"), OneRow("B") };
            var service = new MepTbqProjectionService();
            var expected = service.SerializeCsv((IReadOnlyList<MepTbqReportRow>)rows);
            var source = new FixedCountRows(2, rows);
            var actual = service.SerializeCsv(source);
            Require(string.Equals(expected, actual, StringComparison.Ordinal),
                "Stable MEP/TBQ CSV row sources must preserve deterministic serialization output.");
            Require(source.IndexerReads == 2,
                "Stable MEP/TBQ CSV row source must read each admitted row exactly once.");
        }

        private static MepTbqReportRow OneRow(string suffix)
        {
            var groups = new MepQuantityService().Aggregate(new[]
            {
                new MepElement("CSV-" + suffix, MepElementKind.Pipe, "CHW", "DN50-" + suffix, "L01", 1, 1d)
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
                Require(ex.Message.IndexOf("CSV row Count", StringComparison.Ordinal) >= 0,
                    "Unexpected MEP/TBQ CSV Count-integrity error: " + ex.Message);
                return;
            }
            throw new InvalidOperationException("Expected MEP/TBQ CSV row Count-integrity rejection.");
        }

        private static void Throws<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException(label + ": expected " + typeof(T).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class MutableCountRows : IReadOnlyList<MepTbqReportRow>
        {
            private readonly MepTbqReportRow[] _rows;
            private readonly int _before;
            private readonly int _after;
            private readonly int _mutateAfterIndexerRead;
            private bool _mutated;

            internal MutableCountRows(
                MepTbqReportRow[] rows,
                int before,
                int after,
                int mutateAfterIndexerRead)
            {
                _rows = rows;
                _before = before;
                _after = after;
                _mutateAfterIndexerRead = mutateAfterIndexerRead;
            }

            public int Count => _mutated ? _after : _before;
            internal int IndexerReads { get; private set; }

            public MepTbqReportRow this[int index]
            {
                get
                {
                    IndexerReads++;
                    if (IndexerReads >= _mutateAfterIndexerRead) _mutated = true;
                    return _rows[index];
                }
            }

            public IEnumerator<MepTbqReportRow> GetEnumerator()
            {
                for (var i = 0; i < _rows.Length; i++) yield return _rows[i];
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class FinalReadDriftRows : IReadOnlyList<MepTbqReportRow>
        {
            private readonly MepTbqReportRow _row;
            private int _countReads;

            internal FinalReadDriftRows(MepTbqReportRow row) => _row = row;

            public int Count
            {
                get
                {
                    _countReads++;
                    return _countReads >= 3 ? 2 : 1;
                }
            }

            internal int IndexerReads { get; private set; }
            public MepTbqReportRow this[int index]
            {
                get
                {
                    IndexerReads++;
                    if (index != 0) throw new InvalidOperationException("Unexpected MEP/TBQ CSV indexer read.");
                    return _row;
                }
            }

            public IEnumerator<MepTbqReportRow> GetEnumerator() { yield return _row; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class FixedCountRows : IReadOnlyList<MepTbqReportRow>
        {
            private readonly int _count;
            private readonly MepTbqReportRow[] _rows;

            internal FixedCountRows(int count, params MepTbqReportRow[] rows)
            {
                _count = count;
                _rows = rows;
            }

            public int Count => _count;
            internal int IndexerReads { get; private set; }
            public MepTbqReportRow this[int index]
            {
                get
                {
                    IndexerReads++;
                    return _rows[index];
                }
            }

            public IEnumerator<MepTbqReportRow> GetEnumerator()
            {
                for (var i = 0; i < _rows.Length; i++) yield return _rows[i];
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
