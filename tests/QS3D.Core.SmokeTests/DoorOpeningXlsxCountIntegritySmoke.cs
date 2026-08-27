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
            AssertConflictingTopLevelKnownCountsFailBeforeTraversal();
            AssertConflictingNestedKnownCountsFailBeforeFilesystemCreation();
            AssertRowCountDriftFailsBeforeExistingDestinationReplacement();
            AssertRowCountDriftFailsBeforeFilesystemCreation();
            AssertProvenanceIsExported();
            AssertOversizedSourceHandlesFailBeforeFilesystemCreation();
            AssertCardinalityMismatchFailsBeforePublication();
            AssertInvalidProvenanceFailsBeforePublication();
            AssertInvalidProvenanceFailsBeforeFilesystemCreation();
            AssertDisplayTextSanitizationRemainsCompatible();
            AssertZeroHostRowRemainsValid();
        }

        private static void AssertConflictingTopLevelKnownCountsFailBeforeTraversal()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-xlsx-known-count-" + Guid.NewGuid().ToString("N"));
            try
            {
                var rows = new ConflictingKnownCountRows(ValidRow());
                ExpectInvalidOperation(
                    () => DoorOpeningXlsxExporter.Export(Path.Combine(root, "door-opening.xlsx"), rows),
                    "conflicting known collection counts");
                if (rows.IndexerReads != 0)
                    throw new InvalidOperationException("Door/opening XLSX traversed rows before rejecting conflicting known counts.");
                if (Directory.Exists(root))
                    throw new InvalidOperationException("Door/opening XLSX conflicting top-level counts touched the filesystem.");
            }
            finally { try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { } }
        }

        private static void AssertConflictingNestedKnownCountsFailBeforeFilesystemCreation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-xlsx-nested-known-count-" + Guid.NewGuid().ToString("N"));
            try
            {
                var row = ValidRow();
                row.SourceHandles = new ConflictingKnownCountStrings("AB12");
                ExpectInvalidOperation(
                    () => DoorOpeningXlsxExporter.Export(Path.Combine(root, "door-opening.xlsx"), new[] { row }),
                    "conflicting known collection counts");
                if (Directory.Exists(root))
                    throw new InvalidOperationException("Door/opening XLSX conflicting nested counts touched the filesystem.");
            }
            finally { try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { } }
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
            finally { try { Directory.Delete(root, true); } catch { } }
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
            finally { try { Directory.Delete(root, true); } catch { } }
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
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidOperationException("Door/opening XLSX is missing sheet1.xml.");
                    string xml; using (var reader = new StreamReader(entry.Open())) xml = reader.ReadToEnd();
                    AssertContains(xml, "<dimension ref=\"A1:P2\"/>", "expanded provenance worksheet range");
                    AssertContains(xml, "Project ID", "Project ID header");
                    AssertContains(xml, "Drawing Fingerprint", "Drawing Fingerprint header");
                    AssertContains(xml, "Source Handles", "Source Handles header");
                    AssertContains(xml, "project-door-opening", "Project ID value");
                    AssertContains(xml, "drawing-fingerprint-door-opening", "Drawing Fingerprint value");
                    AssertContains(xml, "AB12;CD34", "Source Handles value");
                }
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        private static void AssertOversizedSourceHandlesFailBeforeFilesystemCreation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-xlsx-provenance-bound-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                var row = ValidRow(); row.SourceHandles.Add(new string('A', 32768));
                try { DoorOpeningXlsxExporter.Export(Path.Combine(untouchedDirectory, "door-opening.xlsx"), new[] { row }); }
                catch (ArgumentOutOfRangeException ex)
                {
                    if (ex.Message.IndexOf("Source Handles", StringComparison.OrdinalIgnoreCase) < 0)
                        throw new InvalidOperationException("Door/opening XLSX oversized source-handle failure must identify the provenance field.", ex);
                    if (Directory.Exists(untouchedDirectory)) throw new InvalidOperationException("Door/opening XLSX oversized source handles touched the filesystem before failing.");
                    return;
                }
                throw new InvalidOperationException("Door/opening XLSX exporter accepted source handles exceeding Excel's cell text limit.");
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        private static void AssertCardinalityMismatchFailsBeforePublication()
        {
            AssertInvalidRowPreservesExistingDestination("count-element-mismatch", row => row.Count = row.ElementIds.Count + 1, "Count must match Element IDs count");
            AssertInvalidRowPreservesExistingDestination("host-count-mismatch", row => row.HostCount = row.HostIds.Count + 1, "HostCount must match Host IDs count");
        }

        private static void AssertInvalidProvenanceFailsBeforePublication()
        {
            AssertInvalidRowPreservesExistingDestination("project-control", row => row.ProjectId = "project\u0001bad", "Project ID XML control");
            AssertInvalidRowPreservesExistingDestination("fingerprint-control", row => row.DrawingFingerprint = "drawing\u0001bad", "Drawing Fingerprint XML control");
            AssertInvalidRowPreservesExistingDestination("element-control", row => row.ElementIds[0] = "E\u0001bad", "Element ID XML control");
            AssertInvalidRowPreservesExistingDestination("host-control", row => row.HostIds[0] = "H\u0001bad", "Host ID XML control");
            AssertInvalidRowPreservesExistingDestination("source-handle-control", row => row.SourceHandles.Add("AB\u0001bad"), "Source Handle XML control");
        }

        private static void AssertInvalidProvenanceFailsBeforeFilesystemCreation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-xlsx-invalid-provenance-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                var row = ValidRow(); row.DrawingFingerprint = "drawing\u0001bad";
                ExpectArgumentException(() => DoorOpeningXlsxExporter.Export(Path.Combine(untouchedDirectory, "door-opening.xlsx"), new[] { row }), "Door/opening XLSX accepted an XML-invalid drawing fingerprint.");
                if (Directory.Exists(untouchedDirectory)) throw new InvalidOperationException("Door/opening XLSX invalid provenance touched the filesystem before failing.");
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        private static void AssertDisplayTextSanitizationRemainsCompatible()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-xlsx-display-sanitize-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "door-opening.xlsx"); var row = ValidRow(); row.FamilyName = "Invalid\u0001Family"; DoorOpeningXlsxExporter.Export(destination, new[] { row });
                using (var archive = ZipFile.OpenRead(destination))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidOperationException("Door/opening XLSX is missing sheet1.xml.");
                    string xml; using (var reader = new StreamReader(entry.Open())) xml = reader.ReadToEnd();
                    if (xml.IndexOf('\u0001') >= 0) throw new InvalidOperationException("Door/opening XLSX retained an XML-invalid display control character.");
                    if (xml.IndexOf('\uFFFD') < 0) throw new InvalidOperationException("Door/opening XLSX no longer preserves display-text sanitization compatibility.");
                }
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        private static void AssertZeroHostRowRemainsValid()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-xlsx-zero-host-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "door-opening.xlsx"); var row = ValidRow(); row.HostIds.Clear(); row.HostCount = 0; DoorOpeningXlsxExporter.Export(destination, new[] { row });
                if (!File.Exists(destination)) throw new InvalidOperationException("Door/opening XLSX rejected a valid unhosted row.");
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        private static void AssertInvalidRowPreservesExistingDestination(string suffix, Action<DoorOpeningScheduleRow> mutate, string label)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-xlsx-" + suffix + "-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "door-opening.xlsx"); const string sentinel = "preserve-existing-door-opening-destination"; File.WriteAllText(destination, sentinel); var row = ValidRow(); mutate(row);
                ExpectArgumentException(() => DoorOpeningXlsxExporter.Export(destination, new[] { row }), "Door/opening XLSX accepted invalid input: " + label + ".");
                if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal)) throw new InvalidOperationException("Door/opening XLSX modified an existing destination after rejecting " + label + ".");
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        private static void AssertRowCountDrift(string destination)
        {
            try { DoorOpeningXlsxExporter.Export(destination, new CountDriftingRows(ValidRow())); }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("row count changed during snapshot", StringComparison.OrdinalIgnoreCase) < 0) throw new InvalidOperationException("Door/opening XLSX row-count drift must identify snapshot count instability.", ex);
                return;
            }
            throw new InvalidOperationException("Door/opening XLSX exporter accepted a source whose row count changed during snapshot.");
        }

        private static void ExpectInvalidOperation(Action action, string expected)
        {
            try { action(); }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw;
            }
            throw new InvalidOperationException("Door/opening XLSX accepted conflicting collection count metadata.");
        }

        private static void ExpectArgumentException(Action action, string message) { try { action(); } catch (ArgumentException) { return; } throw new InvalidOperationException(message); }
        private static void AssertContains(string text, string expected, string label) { if (text.IndexOf(expected, StringComparison.Ordinal) < 0) throw new InvalidOperationException("Door/opening XLSX did not preserve expected " + label + "."); }

        private static DoorOpeningScheduleRow ValidRow()
        {
            var row = new DoorOpeningScheduleRow { ProjectId = "project", DrawingFingerprint = "drawing-fingerprint", Floor = "L1", Category = "Door", FamilyName = "D1", Material = "Timber", WidthM = 0.9d, HeightM = 2.1d, SillHeightM = 0d, ThicknessM = 0.05d, Count = 1, OpeningAreaM2 = 1.89d, HostCount = 1 };
            row.ElementIds.Add("E1"); row.HostIds.Add("H1"); return row;
        }

        private sealed class ConflictingKnownCountRows : IReadOnlyList<DoorOpeningScheduleRow>, ICollection<DoorOpeningScheduleRow>, ICollection
        {
            private readonly DoorOpeningScheduleRow _row;
            internal ConflictingKnownCountRows(DoorOpeningScheduleRow row) { _row = row; }
            public int Count => 1;
            int ICollection<DoorOpeningScheduleRow>.Count => 2;
            int ICollection.Count => 3;
            public int IndexerReads { get; private set; }
            public DoorOpeningScheduleRow this[int index] { get { IndexerReads++; if (index != 0) throw new ArgumentOutOfRangeException(nameof(index)); return _row; } }
            bool ICollection<DoorOpeningScheduleRow>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            void ICollection<DoorOpeningScheduleRow>.Add(DoorOpeningScheduleRow item) => throw new NotSupportedException();
            void ICollection<DoorOpeningScheduleRow>.Clear() => throw new NotSupportedException();
            bool ICollection<DoorOpeningScheduleRow>.Contains(DoorOpeningScheduleRow item) => ReferenceEquals(item, _row);
            void ICollection<DoorOpeningScheduleRow>.CopyTo(DoorOpeningScheduleRow[] array, int arrayIndex) => array[arrayIndex] = _row;
            bool ICollection<DoorOpeningScheduleRow>.Remove(DoorOpeningScheduleRow item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_row, index);
            public IEnumerator<DoorOpeningScheduleRow> GetEnumerator() { yield return _row; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ConflictingKnownCountStrings : IList<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly string _value;
            internal ConflictingKnownCountStrings(string value) { _value = value; }
            public int Count => 1;
            int IReadOnlyCollection<string>.Count => 2;
            int ICollection.Count => 3;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            public string this[int index] { get { if (index != 0) throw new ArgumentOutOfRangeException(nameof(index)); return _value; } set => throw new NotSupportedException(); }
            public int IndexOf(string item) => string.Equals(item, _value, StringComparison.Ordinal) ? 0 : -1;
            public void Insert(int index, string item) => throw new NotSupportedException();
            public void RemoveAt(int index) => throw new NotSupportedException();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => IndexOf(item) == 0;
            public void CopyTo(string[] array, int arrayIndex) => array[arrayIndex] = _value;
            public bool Remove(string item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_value, index);
            public IEnumerator<string> GetEnumerator() { yield return _value; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class CountDriftingRows : IReadOnlyList<DoorOpeningScheduleRow>
        {
            private readonly DoorOpeningScheduleRow _row; private int _countReads;
            internal CountDriftingRows(DoorOpeningScheduleRow row) { _row = row; }
            public int Count { get { _countReads++; return _countReads == 1 ? 1 : 2; } }
            public DoorOpeningScheduleRow this[int index] { get { if (index != 0) throw new ArgumentOutOfRangeException(nameof(index)); return _row; } }
            public IEnumerator<DoorOpeningScheduleRow> GetEnumerator() { yield return _row; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
