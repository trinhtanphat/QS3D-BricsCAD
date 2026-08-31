using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationWorkbookCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            GrowthAfterFirstRowRejectsBeforeUnexpectedIndexerRead();
            ShrinkAfterFirstRowRejectsBeforeMissingIndexerRead();
            PostTraversalCountDriftRejects();
            EmptyCountRejectsBeforeIndexerRead();
            OversizedCountRejectsBeforeIndexerRead();
            StableRowsExportDeterministically();
        }

        private static void GrowthAfterFirstRowRejectsBeforeUnexpectedIndexerRead()
        {
            var source = new MutableCountRows(new[] { Row("A", "10", "20"), Row("B", "30", "40") }, 1, 2, 1);
            ThrowsCountIntegrity(() => Export(source));
            Require(source.IndexerReads == 1,
                "Coordination workbook Count growth must reject before reading beyond the admitted Count.");
        }

        private static void ShrinkAfterFirstRowRejectsBeforeMissingIndexerRead()
        {
            var source = new MutableCountRows(new[] { Row("A", "10", "20"), Row("B", "30", "40") }, 2, 1, 1);
            ThrowsCountIntegrity(() => Export(source));
            Require(source.IndexerReads == 1,
                "Coordination workbook Count shrink must reject before reading an index from another source generation.");
        }

        private static void PostTraversalCountDriftRejects()
        {
            var source = new FinalReadDriftRows(Row("A", "10", "20"));
            ThrowsCountIntegrity(() => Export(source));
            Require(source.IndexerReads == 1,
                "Coordination workbook post-traversal Count drift must occur after exactly the admitted row was read.");
        }

        private static void EmptyCountRejectsBeforeIndexerRead()
        {
            var source = new FixedCountRows(0, Row("A", "10", "20"));
            Throws<InvalidDataException>(() => Export(source), "Coordination workbook empty Count");
            Require(source.IndexerReads == 0,
                "Empty Coordination workbook Count must reject before any row indexer read.");
        }

        private static void OversizedCountRejectsBeforeIndexerRead()
        {
            var source = new FixedCountRows(1048576, Row("A", "10", "20"));
            Throws<InvalidDataException>(() => Export(source), "Coordination workbook oversized Count");
            Require(source.IndexerReads == 0,
                "Oversized Coordination workbook Count must reject before any row indexer read.");
        }

        private static void StableRowsExportDeterministically()
        {
            var rows = new[] { Row("B", "30", "40"), Row("A", "10", "20") };
            var expected = Export((IReadOnlyList<CoordinationClashExportRow>)rows);
            var source = new FixedCountRows(2, rows);
            var actual = Export(source);
            Require(ByteEqual(expected, actual),
                "Stable Coordination workbook sources must preserve deterministic XLSX output.");
            Require(source.IndexerReads == 2,
                "Stable Coordination workbook source must read each admitted row exactly once.");
        }

        private static CoordinationClashExportRow Row(string suffix, string left, string right)
            => CoordinationClashExportRow.CreateExactHard(
                "DWG-COUNT-STABILITY", left + suffix, right + suffix,
                "LEFT-" + suffix, "RIGHT-" + suffix, "Pipe", "Duct", "L01", "count stability");

        private static byte[] Export(IReadOnlyList<CoordinationClashExportRow> rows)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-coordination-count-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "coordination.xlsx");
            try
            {
                CoordinationWorkbookExporter.Export(path, rows);
                return File.ReadAllBytes(path);
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static bool ByteEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            for (var i = 0; i < left.Length; i++)
                if (left[i] != right[i]) return false;
            return true;
        }

        private static void ThrowsCountIntegrity(Action action)
        {
            try { action(); }
            catch (InvalidDataException ex)
            {
                Require(ex.Message.IndexOf("row Count", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Unexpected Coordination workbook Count-integrity error: " + ex.Message);
                return;
            }
            throw new InvalidOperationException("Expected Coordination workbook row Count-integrity rejection.");
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

        private sealed class MutableCountRows : IReadOnlyList<CoordinationClashExportRow>
        {
            private readonly CoordinationClashExportRow[] _rows;
            private readonly int _before;
            private readonly int _after;
            private readonly int _mutateAfterIndexerRead;
            private bool _mutated;

            internal MutableCountRows(CoordinationClashExportRow[] rows, int before, int after, int mutateAfterIndexerRead)
            {
                _rows = rows;
                _before = before;
                _after = after;
                _mutateAfterIndexerRead = mutateAfterIndexerRead;
            }

            public int Count => _mutated ? _after : _before;
            internal int IndexerReads { get; private set; }

            public CoordinationClashExportRow this[int index]
            {
                get
                {
                    IndexerReads++;
                    if (IndexerReads >= _mutateAfterIndexerRead) _mutated = true;
                    return _rows[index];
                }
            }

            public IEnumerator<CoordinationClashExportRow> GetEnumerator()
            {
                for (var i = 0; i < _rows.Length; i++) yield return _rows[i];
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class FinalReadDriftRows : IReadOnlyList<CoordinationClashExportRow>
        {
            private readonly CoordinationClashExportRow _row;
            private int _countReads;

            internal FinalReadDriftRows(CoordinationClashExportRow row) => _row = row;

            public int Count
            {
                get
                {
                    _countReads++;
                    return _countReads >= 3 ? 2 : 1;
                }
            }

            internal int IndexerReads { get; private set; }
            public CoordinationClashExportRow this[int index]
            {
                get
                {
                    IndexerReads++;
                    if (index != 0) throw new InvalidOperationException("Unexpected Coordination workbook row indexer read.");
                    return _row;
                }
            }

            public IEnumerator<CoordinationClashExportRow> GetEnumerator() { yield return _row; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class FixedCountRows : IReadOnlyList<CoordinationClashExportRow>
        {
            private readonly int _count;
            private readonly CoordinationClashExportRow[] _rows;

            internal FixedCountRows(int count, params CoordinationClashExportRow[] rows)
            {
                _count = count;
                _rows = rows;
            }

            public int Count => _count;
            internal int IndexerReads { get; private set; }

            public CoordinationClashExportRow this[int index]
            {
                get
                {
                    IndexerReads++;
                    return _rows[index];
                }
            }

            public IEnumerator<CoordinationClashExportRow> GetEnumerator()
            {
                for (var i = 0; i < _rows.Length; i++) yield return _rows[i];
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
