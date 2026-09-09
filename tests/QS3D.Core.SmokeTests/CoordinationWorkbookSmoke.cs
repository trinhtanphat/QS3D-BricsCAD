using System;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationWorkbookSmoke
    {
        private const int BoundedDataRows = 10000;

        internal static void Run()
        {
            RejectsSourceCardinalityAboveBoundBeforeArchiveCommit();
            RejectsOversizedWorksheetBeforeArchiveCommit();
            Console.WriteLine("PASS coordination legacy workbook bounded write");
        }

        private static void RejectsSourceCardinalityAboveBoundBeforeArchiveCommit()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-coordination-legacy-row-bound-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var destination = Path.Combine(directory, "bounded.xlsx");
            var sentinel = new byte[] { 0x51, 0x53, 0x33, 0x44, 0x2D, 0x52, 0x4F, 0x57 };
            File.WriteAllBytes(destination, sentinel);
            try
            {
                var rows = BuildRows(BoundedDataRows + 1, string.Empty);
                ExpectInvalidData(() => CoordinationWorkbookExporter.Export(destination, rows), "source cardinality above bounded export limit");
                AssertBytesEqual(sentinel, File.ReadAllBytes(destination), "destination sentinel after row-bound rejection");
                AssertNoOwnedTemps(directory, destination);
            }
            finally
            {
                TryDeleteDirectory(directory);
            }
        }

        private static void RejectsOversizedWorksheetBeforeArchiveCommit()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-coordination-legacy-byte-bound-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var destination = Path.Combine(directory, "bounded.xlsx");
            var sentinel = new byte[] { 0x51, 0x53, 0x33, 0x44, 0x2D, 0x58, 0x4D, 0x4C };
            File.WriteAllBytes(destination, sentinel);
            try
            {
                var nearExcelCellLimit = new string('X', 32767);
                var rows = BuildRows(1100, nearExcelCellLimit);
                ExpectInvalidData(() => CoordinationWorkbookExporter.Export(destination, rows), "oversized worksheet byte budget");
                AssertBytesEqual(sentinel, File.ReadAllBytes(destination), "destination sentinel after XML-budget rejection");
                AssertNoOwnedTemps(directory, destination);
            }
            finally
            {
                TryDeleteDirectory(directory);
            }
        }

        private static List<CoordinationClashExportRow> BuildRows(int count, string comment)
        {
            var rows = new List<CoordinationClashExportRow>(count);
            for (var index = 0; index < count; index++)
            {
                var left = (index * 2 + 1).ToString("X");
                var right = (index * 2 + 2).ToString("X");
                rows.Add(CoordinationClashExportRow.CreateExactHard(
                    "LEGACY-COORDINATION-BOUNDED-WRITE",
                    left,
                    right,
                    leftElementId: "A" + index,
                    rightElementId: "B" + index,
                    comment: comment));
            }
            return rows;
        }

        private static void ExpectInvalidData(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidDataException)
            {
                return;
            }
            throw new InvalidOperationException("Expected InvalidDataException for " + label + ".");
        }

        private static void AssertNoOwnedTemps(string directory, string destination)
        {
            var pattern = Path.GetFileName(destination) + ".*.tmp";
            var leftovers = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
            if (leftovers.Length != 0)
                throw new InvalidOperationException("Expected no owned temp files, found: " + string.Join(", ", leftovers));
        }

        private static void AssertBytesEqual(byte[] expected, byte[] actual, string label)
        {
            if (expected.Length != actual.Length)
                throw new InvalidOperationException(label + " length changed.");
            for (var index = 0; index < expected.Length; index++)
            {
                if (expected[index] != actual[index])
                    throw new InvalidOperationException(label + " changed at byte " + index + ".");
            }
        }

        private static void TryDeleteDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
            catch
            {
                // Best-effort cleanup only; assertions above own correctness.
            }
        }
    }
}
