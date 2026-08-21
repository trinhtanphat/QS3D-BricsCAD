using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningXlsxCountIntegritySmoke
    {
        internal static void Run()
        {
            AssertRowCountDriftFailsBeforeExistingDestinationReplacement();
            AssertRowCountDriftFailsBeforeFilesystemCreation();
            AssertProvenanceIsExported();
            AssertOversizedSourceHandlesFailBeforeFilesystemCreation();
        }

        private static void AssertRowCountDriftFailsBeforeExistingDestinationReplacement()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-xlsx-count-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "door-opening.xlsx");
                const string sentinel = "preserve-existing-door-opening-destination";
                File.WriteAllText(destination, sentinel);

                AssertRowCountDrift(destination);

                if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal))
                    throw new InvalidOperationException("Door/opening XLSX row-count drift replaced an existing destination file.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertRowCountDriftFailsBeforeFilesystemCreation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-xlsx-count-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                AssertRowCountDrift(Path.Combine(untouchedDirectory, "door-opening.xlsx"));
                if (Directory.Exists(untouchedDirectory))
                    throw new InvalidOperationException("Door/opening XLSX row-count drift touched the filesystem before failing.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertProvenanceIsExported()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-xlsx-provenance-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "door-opening.xlsx");
                var row = ValidRow();
                row.ProjectId = "project-door-opening";
                row.DrawingFingerprint = "drawing-fingerprint-door-opening";
                row.SourceHandles.Add("AB12");
                row.SourceHandles.Add("CD34");

                DoorOpeningXlsxExporter.Export(destination, new[] { row });

                using (var archive = ZipFile.OpenRead(destination))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml")
                        ?? throw new InvalidOperationException("Door/opening XLSX is missing sheet1.xml.");
                    string xml;
                    using (var reader = new StreamReader(entry.Open())) xml = reader.ReadToEnd();
                    AssertContains(xml, "<dimension ref=\"A1:P2\"/>", "expanded provenance worksheet range");
                    AssertContains(xml, "Project ID", "Project ID header");
                    AssertContains(xml, "Drawing Fingerprint", "Drawing Fingerprint header");
                    AssertContains(xml, "Source Handles", "Source Handles header");
                    AssertContains(xml, "project-door-opening", "Project ID value");
                    AssertContains(xml, "drawing-fingerprint-door-opening", "Drawing Fingerprint value");
                    AssertContains(xml, "AB12;CD34", "Source Handles value");
                }
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertOversizedSourceHandlesFailBeforeFilesystemCreation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-xlsx-provenance-bound-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                var row = ValidRow();
                row.SourceHandles.Add(new string('A', 32768));
                try
                {
                    DoorOpeningXlsxExporter.Export(Path.Combine(untouchedDirectory, "door-opening.xlsx"), new[] { row });
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    if (ex.Message.IndexOf("Source Handles", StringComparison.OrdinalIgnoreCase) < 0)
                        throw new InvalidOperationException("Door/opening XLSX oversized source-handle failure must identify the provenance field.", ex);
                    if (Directory.Exists(untouchedDirectory))
                        throw new InvalidOperationException("Door/opening XLSX oversized source handles touched the filesystem before failing.");
                    return;
                }

                throw new InvalidOperationException("Door/opening XLSX exporter accepted source handles exceeding Excel's cell text limit.");
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
                DoorOpeningXlsxExporter.Export(destination, new CountDriftingRows(ValidRow()));
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("row count changed during snapshot", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Door/opening XLSX row-count drift must identify snapshot count instability.", ex);
                return;
            }

            throw new InvalidOperationException("Door/opening XLSX exporter accepted a source whose row count changed during snapshot.");
        }

        private static void AssertContains(string text, string expected, string label)
        {
            if (text.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Door/opening XLSX did not preserve expected " + label + ".");
        }

        private static DoorOpeningScheduleRow ValidRow()
        {
            var row = new DoorOpeningScheduleRow
            {
                Floor = "L1",
                Category = "Door",
                FamilyName = "D1",
                Material = "Timber",
                WidthM = 0.9d,
                HeightM = 2.1d,
                SillHeightM = 0d,
                ThicknessM = 0.05d,
                Count = 1,
                OpeningAreaM2 = 1.89d,
                HostCount = 1
            };
            row.ElementIds.Add("E1");
            row.HostIds.Add("H1");
            return row;
        }

        private sealed class CountDriftingRows : IReadOnlyList<DoorOpeningScheduleRow>
        {
            private readonly DoorOpeningScheduleRow _row;
            private int _countReads;

            internal CountDriftingRows(DoorOpeningScheduleRow row)
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

            public DoorOpeningScheduleRow this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    return _row;
                }
            }

            public IEnumerator<DoorOpeningScheduleRow> GetEnumerator()
            {
                yield return _row;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}