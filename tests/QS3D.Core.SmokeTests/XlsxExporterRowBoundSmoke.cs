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
    internal static class XlsxExporterRowBoundSmoke
    {
        public static void Run()
        {
            VerifyRejectsBeforeInspection<MaterialUsageRow>(MaterialUsageXlsxExporter.Export, "Material XLSX");
            VerifyRejectsBeforeInspection<DoorOpeningScheduleRow>(DoorOpeningXlsxExporter.Export, "Door/opening XLSX");
            VerifyRejectsBeforeInspection<CurtainWallScheduleRow>(CurtainWallXlsxExporter.Export, "Curtain XLSX");
            VerifyMaterialBoundaryWhitespacePreserved();
            VerifyCurtainBoundaryWhitespacePreserved();
        }

        private static void VerifyMaterialBoundaryWhitespacePreserved()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-space-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "out.xlsx");
            try
            {
                MaterialUsageXlsxExporter.Export(
                    path,
                    new[]
                    {
                        new MaterialUsageRow
                        {
                            Floor = "  Level 1  ",
                            MaterialName = "Concrete & Steel",
                            UnitHint = string.Empty,
                            Component = "Material",
                            Category = "Wall",
                            FamilyName = "Standard",
                            ElementCount = 1,
                            ElementIds = { "E-WHITESPACE-1" }
                        }
                    });

                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml");
                    if (entry == null) throw new Exception("Material XLSX is missing sheet1.xml.");
                    string xml;
                    using (var reader = new StreamReader(entry.Open(), Encoding.UTF8)) xml = reader.ReadToEnd();
                    if (xml.IndexOf("<t xml:space=\"preserve\">  Level 1  </t>", StringComparison.Ordinal) < 0)
                        throw new Exception("Material XLSX must preserve leading/trailing cell whitespace with xml:space=\"preserve\".");
                    if (xml.IndexOf("<t>Concrete &amp; Steel</t>", StringComparison.Ordinal) < 0)
                        throw new Exception("Material XLSX must retain ordinary inline-text escaping without adding xml:space unnecessarily.");
                }
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static void VerifyCurtainBoundaryWhitespacePreserved()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-curtain-xlsx-space-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "out.xlsx");
            try
            {
                var row = new CurtainWallScheduleRow
                {
                    Floor = "  Level 2  ",
                    FamilyName = "Glass & Frame",
                    WallCount = 1,
                    PanelCount = 1,
                    MinimumClearPanelWidthM = 0d,
                    MaximumClearPanelWidthM = 0d,
                    MinimumClearPanelHeightM = 0d,
                    MaximumClearPanelHeightM = 0d
                };
                row.ElementIds.Add("CW-WHITESPACE-1");
                row.SourceHandles.Add("CW-WHITESPACE-HANDLE-1");
                CurtainWallXlsxExporter.Export(path, new[] { row });

                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml");
                    if (entry == null) throw new Exception("Curtain XLSX is missing sheet1.xml.");
                    string xml;
                    using (var reader = new StreamReader(entry.Open(), Encoding.UTF8)) xml = reader.ReadToEnd();
                    if (xml.IndexOf("<t xml:space=\"preserve\">  Level 2  </t>", StringComparison.Ordinal) < 0)
                        throw new Exception("Curtain XLSX must preserve leading/trailing cell whitespace with xml:space=\"preserve\".");
                    if (xml.IndexOf("<t>Glass &amp; Frame</t>", StringComparison.Ordinal) < 0)
                        throw new Exception("Curtain XLSX must retain ordinary inline-text escaping without adding xml:space unnecessarily.");
                }
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static void VerifyRejectsBeforeInspection<T>(Action<string, IReadOnlyList<T>> export, string label)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-row-bound-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "out.xlsx");
            try
            {
                try
                {
                    export(path, new OversizedRows<T>());
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                        throw new Exception(label + " must reject the oversized row list itself.", ex);
                    if (Directory.Exists(directory))
                        throw new Exception(label + " must reject oversized rows before creating the output directory.");
                    return;
                }
                catch (Exception ex)
                {
                    throw new Exception(label + " must reject oversized rows with ArgumentOutOfRangeException before inspecting any row. Received " + ex.GetType().Name + ".", ex);
                }

                throw new Exception(label + " accepted a data-row count that exceeds one worksheet after reserving the header row.");
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private sealed class OversizedRows<T> : IReadOnlyList<T>
        {
            public int Count { get { return 1048576; } }

            public T this[int index]
            {
                get { throw new InvalidOperationException("Oversized XLSX rows must be rejected before the exporter indexes the list."); }
            }

            public IEnumerator<T> GetEnumerator()
            {
                throw new InvalidOperationException("Oversized XLSX rows must be rejected before the exporter enumerates the list.");
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
