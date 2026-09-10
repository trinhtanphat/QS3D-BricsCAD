using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxRebarScheduleBoundedWriteSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ValidWorkbookKeepsStrictUtf8WorksheetContract();
            CumulativeWorksheetBudgetFailsBeforeDestinationReplacement();
        }

        private static void ValidWorkbookKeepsStrictUtf8WorksheetContract()
        {
            var path = Path.Combine(Path.GetTempPath(), "rebar-xlsx-valid-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                XlsxRebarScheduleExporter.Export(path, new[] { CreateRow("E-界", "B1") });
                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml")
                        ?? throw new InvalidOperationException("Rebar XLSX bounded smoke: worksheet entry is missing.");
                    using (var reader = new StreamReader(entry.Open(), new UTF8Encoding(false, true)))
                    {
                        var xml = reader.ReadToEnd();
                        if (!xml.Contains("E-界", StringComparison.Ordinal))
                            throw new InvalidOperationException("Rebar XLSX bounded smoke: strict UTF-8 worksheet round-trip failed.");
                    }
                }
            }
            finally { TryDelete(path); }
        }

        private static void CumulativeWorksheetBudgetFailsBeforeDestinationReplacement()
        {
            var path = Path.Combine(Path.GetTempPath(), "rebar-xlsx-bounded-" + Guid.NewGuid().ToString("N") + ".xlsx");
            const string sentinel = "KEEP-EXISTING-REBAR-WORKBOOK";
            File.WriteAllText(path, sentinel, Encoding.UTF8);
            try { ExistingRebarWorkbookSurvivesBudgetFailure(path, sentinel); }
            finally
            {
                TryDelete(path);
                var directory = Path.GetDirectoryName(path)!;
                foreach (var temp in Directory.GetFiles(directory, "." + Path.GetFileName(path) + ".*.tmp")) TryDelete(temp);
            }
        }

        private static void ExistingRebarWorkbookSurvivesBudgetFailure(string path, string sentinel)
        {
            var rows = new List<RebarScheduleRow>();
            var hostile = new string('&', 32767);
            for (var i = 0; i < 40; i++) rows.Add(CreateRow("E-" + i.ToString(), hostile));

            try
            {
                XlsxRebarScheduleExporter.Export(path, rows);
                throw new InvalidOperationException("Rebar XLSX worksheet budget unexpectedly published.");
            }
            catch (InvalidDataException error) when (error.Message.Contains("Rebar XLSX worksheet exceeds", StringComparison.Ordinal)) { }

            var preserved = File.ReadAllText(path, Encoding.UTF8);
            if (!preserved.Contains(sentinel, StringComparison.Ordinal))
                throw new InvalidOperationException("Rebar XLSX bounded smoke: destination sentinel was replaced after failure.");
            var directory = Path.GetDirectoryName(path)!;
            if (Directory.GetFiles(directory, "." + Path.GetFileName(path) + ".*.tmp").Length != 0)
                throw new InvalidOperationException("Rebar XLSX bounded smoke: owned temp artifact leaked after failure.");
        }

        private static RebarScheduleRow CreateRow(string elementId, string hostileText)
        {
            return new RebarScheduleRow
            {
                ElementId = elementId,
                BarMark = hostileText,
                ShapeCode = hostileText,
                Notation = hostileText,
                DiameterMm = 16d,
                Quantity = 2,
                CuttingLengthM = 1.25d,
                TotalLengthM = 2.5d,
                UnitWeightKgM = 1.58d,
                NetWeightKg = 3.95d,
                WastePercent = 5d,
                TotalWeightKg = 4.1475d,
                FabricationStatus = hostileText,
                FabricationStandardCode = hostileText,
                FabricationDetailingRevision = hostileText
            };
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
