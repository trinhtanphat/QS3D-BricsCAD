using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxQuantityProvenanceGenerationSmoke
    {
        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-provenance-generation-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                RejectsStandardElementIdGenerationDrift(Path.Combine(root, "standard.xlsx"));
                RejectsEd2SourceHandleGenerationDrift(Path.Combine(root, "ed2.xlsx"));
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void RejectsStandardElementIdGenerationDrift(string path)
        {
            var row = ValidRow("E1", "AA1");
            ReplaceList(row, "ElementIds", new SameCountDriftingList("E1", "E2"));
            ExpectGenerationDrift(() => XlsxQuantityExporter.Export(path, new[] { row }));
            if (File.Exists(path)) throw new InvalidOperationException("Quantity XLSX provenance generation drift created output before failing closed.");
        }
        private static void RejectsEd2SourceHandleGenerationDrift(string path)
        {
            var detail = ValidRow("E1", "AA1");
            detail.FamilyId = "F1";
            detail.Material = "Concrete";
            var summary = ValidRow("E1", "AA1");
            summary.FamilyId = "F1";
            summary.Material = "Concrete";
            ReplaceList(detail, "SourceHandles", new SameCountDriftingList("AA1", "BB2"));
            ExpectGenerationDrift(() => XlsxQuantityExporter.ExportEd2(path, new[] { detail }, new[] { summary }));
            if (File.Exists(path)) throw new InvalidOperationException("ED2 provenance generation drift created output before failing closed.");
        }

        private static QuantityReportRow ValidRow(string elementId, string handle)
        {
            var row = new QuantityReportRow
            {
                Floor = "L1", Zone = "Z1", Category = "Beam", FamilyName = "B1",
                DrawingFingerprint = "drawing", Count = 1
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add(handle);
            return row;
        }

        private static void ReplaceList(QuantityReportRow row, string propertyName, IList<string> replacement)
        {
            var field = typeof(QuantityReportRow).GetField("<" + propertyName + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new InvalidOperationException("QuantityReportRow backing field not found: " + propertyName + ".");
            field.SetValue(row, replacement);
        }

        private static void ExpectGenerationDrift(Action action)
        {
            try { action(); }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("provenance values changed during snapshot", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Quantity XLSX provenance generation drift failed for the wrong reason.", ex);
                return;
            }
            throw new InvalidOperationException("Quantity XLSX accepted same-count provenance generation drift.");
        }

        private sealed class SameCountDriftingList : IList<string>
        {
            private readonly string _first;
            private readonly string _later;
            private int _reads;

            internal SameCountDriftingList(string first, string later) { _first = first; _later = later; }
            public int Count => 1;
            public bool IsReadOnly => true;
            public string this[int index]
            {
                get { if (index != 0) throw new ArgumentOutOfRangeException(nameof(index)); return ++_reads == 1 ? _first : _later; }
                set => throw new NotSupportedException();
            }
            public int IndexOf(string item) => string.Equals(item, this[0], StringComparison.Ordinal) ? 0 : -1;
            public bool Contains(string item) => IndexOf(item) == 0;
            public void CopyTo(string[] array, int arrayIndex) => array[arrayIndex] = this[0];
            public IEnumerator<string> GetEnumerator() { yield return this[0]; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public void Insert(int index, string item) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
            public void RemoveAt(int index) => throw new NotSupportedException();
        }
    }

    internal static class XlsxQuantityProvenanceGenerationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            XlsxQuantityProvenanceGenerationSmoke.Run();
        }
    }
}
