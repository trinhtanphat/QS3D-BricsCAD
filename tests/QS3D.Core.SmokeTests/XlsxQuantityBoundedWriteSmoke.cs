using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxQuantityBoundedWriteSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => CumulativeWorksheetBudgetFailsBeforeDestinationReplacement();

        private static void CumulativeWorksheetBudgetFailsBeforeDestinationReplacement()
        {
            var path = Path.Combine(Path.GetTempPath(), "quantity-xlsx-bounded-" + Guid.NewGuid().ToString("N") + ".xlsx");
            const string sentinel = "KEEP-EXISTING-QUANTITY-WORKBOOK";
            File.WriteAllText(path, sentinel, Encoding.UTF8);
            try { ExistingQuantityWorkbookSurvivesBudgetFailure(path, sentinel); }
            finally
            {
                TryDelete(path);
                var directory = Path.GetDirectoryName(path)!;
                foreach (var temp in Directory.GetFiles(directory, "." + Path.GetFileName(path) + ".*.tmp")) TryDelete(temp);
            }
        }
        private static void ExistingQuantityWorkbookSurvivesBudgetFailure(string path, string sentinel)
        {
            var rows = new List<QuantityReportRow>();
            var hostile = new string('界', 32767);
            for (var i = 0; i < 80; i++)
            {
                var row = new QuantityReportRow
                {
                    Floor = hostile,
                    Zone = hostile,
                    Category = hostile,
                    FamilyName = hostile,
                    DrawingFingerprint = hostile,
                    Count = 1
                };
                row.ElementIds.Add("element-" + i.ToString(CultureInfo.InvariantCulture));
                row.SourceHandles.Add((i + 1).ToString("X", CultureInfo.InvariantCulture));
                rows.Add(row);
            }

            try
            {
                XlsxQuantityExporter.Export(path, rows);
                throw new InvalidOperationException("Quantity XLSX worksheet budget unexpectedly published.");
            }
            catch (InvalidDataException error) when (error.Message.Contains("Quantity XLSX worksheet exceeds", StringComparison.Ordinal))
            {
            }
            var preserved = File.ReadAllText(path, Encoding.UTF8);
            if (!preserved.Contains(sentinel, StringComparison.Ordinal))
                throw new InvalidOperationException("Quantity XLSX bounded smoke: destination sentinel was replaced after failure.");
            var directory = Path.GetDirectoryName(path)!;
            var pattern = "." + Path.GetFileName(path) + ".*.tmp";
            if (Directory.GetFiles(directory, pattern).Length != 0)
                throw new InvalidOperationException("Quantity XLSX bounded smoke: owned temp artifact leaked after failure.");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
