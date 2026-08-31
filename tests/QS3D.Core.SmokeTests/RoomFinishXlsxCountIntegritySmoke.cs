using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishXlsxCountIntegritySmoke
    {
        internal static void Run()
        {
            AssertRowCountDriftFailsBeforeExistingDestinationReplacement();
            AssertRowCountDriftFailsBeforeFilesystemCreation();
            AssertProvenanceIsExportedWithoutChangingLegacyColumns();
            AssertExactProvenanceCellBoundaryIsAccepted();
            AssertOversizeProvenancePreservesExistingDestination();
            AssertInvalidProvenanceControlPreservesExistingDestination();
            AssertXmlValidControlProvenanceFailsBeforeFilesystemCreation();
            AssertXmlValidControlProvenancePreservesExistingDestination();
        }

        private static void AssertRowCountDriftFailsBeforeExistingDestinationReplacement()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-count-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "room-finish.xlsx");
                const string sentinel = "preserve-existing-room-finish-destination";
                File.WriteAllText(destination, sentinel);

                AssertRowCountDrift(destination);

                if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal))
                    throw new InvalidOperationException("Room-finish XLSX row-count drift replaced an existing destination file.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertRowCountDriftFailsBeforeFilesystemCreation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-count-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                AssertRowCountDrift(Path.Combine(untouchedDirectory, "room-finish.xlsx"));
                if (Directory.Exists(untouchedDirectory))
                    throw new InvalidOperationException("Room-finish XLSX row-count drift touched the filesystem before failing.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertProvenanceIsExportedWithoutChangingLegacyColumns()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-provenance-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "room-finish.xlsx");
                var row = ValidRow();
                row.ProjectId = "PROJECT<&";
                row.DrawingFingerprint = "DRAWING-001";
                row.SourceHandles.Clear();
                row.SourceHandles.Add("H<&1");
                row.SourceHandles.Add("H2");

                RoomFinishXlsxExporter.Export(destination, new[] { row });
                var sheet = ReadWorksheet(destination);

                AssertContains(sheet, "A1:O2", "Room-finish XLSX worksheet range must include appended provenance columns.");
                AssertContains(sheet, ">Tầng</t>", "Existing Floor header must remain the first workbook column.");
                AssertContains(sheet, ">Element IDs</t>", "Existing Element IDs column must remain present.");
                AssertContains(sheet, ">Room IDs</t>", "Existing Room IDs column must remain present.");
                AssertContains(sheet, ">Project ID</t>", "Project provenance header is missing.");
                AssertContains(sheet, ">Drawing fingerprint</t>", "Drawing provenance header is missing.");
                AssertContains(sheet, ">Source Handles</t>", "Source-handle provenance header is missing.");
                AssertContains(sheet, ">PROJECT&lt;&amp;</t>", "Project provenance must be XML escaped.");
                AssertContains(sheet, ">DRAWING-001</t>", "Drawing fingerprint provenance is missing.");
                AssertContains(sheet, ">H&lt;&amp;1;H2</t>", "Source handles must preserve deterministic model order and XML escaping.");
                AssertContains(sheet, ">E1</t>", "Existing element traceability value must remain present.");
                AssertContains(sheet, ">R1</t>", "Existing room traceability value must remain present.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertExactProvenanceCellBoundaryIsAccepted()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-boundary-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "room-finish.xlsx");
                var row = ValidRow();
                row.ProjectId = new string('P', 32767);
                RoomFinishXlsxExporter.Export(destination, new[] { row });
                if (!File.Exists(destination))
                    throw new InvalidOperationException("Room-finish XLSX rejected the exact Excel provenance cell-text boundary.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertOversizeProvenancePreservesExistingDestination()
        {
            AssertProvenanceValidationPreservesExistingDestination(
                row => row.ProjectId = new string('P', 32768),
                typeof(ArgumentOutOfRangeException),
                "Room-finish XLSX must reject provenance beyond Excel's 32,767-character cell limit.");
        }

        private static void AssertInvalidProvenanceControlPreservesExistingDestination()
        {
            AssertProvenanceValidationPreservesExistingDestination(
                row => row.DrawingFingerprint = "DRAWING\u0001INVALID",
                typeof(ArgumentException),
                "Room-finish XLSX must reject XML control characters in provenance rather than silently sanitizing source identity.");
        }

        private static void AssertXmlValidControlProvenanceFailsBeforeFilesystemCreation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-control-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                var row = ValidRow();
                row.ElementIds[0] = "E\t1";
                Exception? observed = null;
                try
                {
                    RoomFinishXlsxExporter.Export(Path.Combine(untouchedDirectory, "room-finish.xlsx"), new[] { row });
                }
                catch (Exception ex)
                {
                    observed = ex;
                }

                if (!(observed is ArgumentException))
                    throw new InvalidOperationException("Room-finish XLSX must reject XML-valid control characters in semantic Element IDs before filesystem mutation.", observed);
                if (Directory.Exists(untouchedDirectory))
                    throw new InvalidOperationException("Room-finish XLSX control-bearing provenance touched the filesystem before failing.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertXmlValidControlProvenancePreservesExistingDestination()
        {
            AssertProvenanceValidationPreservesExistingDestination(
                row => row.ProjectId = "PROJECT\n1",
                typeof(ArgumentException),
                "Room-finish XLSX must reject XML-valid control characters in ProjectId provenance without replacing an existing destination.");
        }

        private static void AssertProvenanceValidationPreservesExistingDestination(
            Action<RoomFinishScheduleRow> mutate,
            Type expectedExceptionType,
            string failureMessage)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-atomic-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "room-finish.xlsx");
                const string sentinel = "preserve-existing-room-finish-destination";
                File.WriteAllText(destination, sentinel);
                var row = ValidRow();
                mutate(row);

                Exception? observed = null;
                try
                {
                    RoomFinishXlsxExporter.Export(destination, new[] { row });
                }
                catch (Exception ex)
                {
                    observed = ex;
                }

                if (observed == null || !expectedExceptionType.IsAssignableFrom(observed.GetType()))
                    throw new InvalidOperationException(failureMessage, observed);
                if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal))
                    throw new InvalidOperationException("Room-finish XLSX provenance validation replaced an existing destination file.");
                var directory = Path.GetDirectoryName(destination) ?? string.Empty;
                var prefix = Path.GetFileName(destination) + ".tmp-";
                if (directory.Length > 0 && Directory.Exists(directory))
                {
                    foreach (var file in Directory.GetFiles(directory))
                    {
                        if (Path.GetFileName(file).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Room-finish XLSX provenance validation left a temporary package behind.");
                    }
                }
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertRowCountDrift(string destination)
        {
            try
            {
                RoomFinishXlsxExporter.Export(destination, new CountDriftingRows(ValidRow()));
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("row count changed during snapshot", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Room-finish XLSX row-count drift must identify snapshot count instability.", ex);
                return;
            }

            throw new InvalidOperationException("Room-finish XLSX exporter accepted a source whose row count changed during snapshot.");
        }

        private static string ReadWorksheet(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false, Encoding.UTF8))
            {
                var entry = archive.GetEntry("xl/worksheets/sheet1.xml");
                if (entry == null) throw new InvalidOperationException("Room-finish XLSX worksheet entry is missing.");
                using (var reader = new StreamReader(entry.Open(), Encoding.UTF8)) return reader.ReadToEnd();
            }
        }

        private static void AssertContains(string text, string expected, string message)
        {
            if (text.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message);
        }

        private static RoomFinishScheduleRow ValidRow()
        {
            var row = new RoomFinishScheduleRow
            {
                ProjectId = "PROJECT-1",
                DrawingFingerprint = "DRAWING-1",
                Floor = "L1",
                Room = "R1",
                Category = "FloorFinish",
                FamilyName = "Finish",
                Material = "Tile",
                UnitHint = "m²",
                Count = 1,
                PrimaryQuantity = 12.5d,
                LengthM = 3.5d,
                AreaM2 = 12.5d
            };
            row.ElementIds.Add("E1");
            row.RoomIds.Add("R1");
            row.SourceHandles.Add("H1");
            return row;
        }

        private sealed class CountDriftingRows : IReadOnlyList<RoomFinishScheduleRow>
        {
            private readonly RoomFinishScheduleRow _row;
            private int _countReads;

            internal CountDriftingRows(RoomFinishScheduleRow row)
            {
                _row = row;
            }

            public int Count
            {
                get
                {
                    _countReads++;
                    return _countReads == 1 ? 1 : 2;
                }
            }

            public RoomFinishScheduleRow this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    return _row;
                }
            }

            public IEnumerator<RoomFinishScheduleRow> GetEnumerator()
            {
                yield return _row;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}