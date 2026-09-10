using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageXlsxBoundedWriteSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => CumulativeWorksheetBudgetFailsBeforeDestinationReplacement();

        private static void CumulativeWorksheetBudgetFailsBeforeDestinationReplacement()
        {
            var path = Path.Combine(Path.GetTempPath(), "material-usage-xlsx-bounded-" + Guid.NewGuid().ToString("N") + ".xlsx");
            const string sentinel = "KEEP-EXISTING-MATERIAL-USAGE-WORKBOOK";
            File.WriteAllText(path, sentinel, Encoding.UTF8);
            try { ExistingMaterialUsageWorkbookSurvivesBudgetFailure(path, sentinel); }
            finally
            {
                TryDelete(path);
                var directory = Path.GetDirectoryName(path)!;
                foreach (var temp in Directory.GetFiles(directory, "." + Path.GetFileName(path) + ".*.tmp")) TryDelete(temp);
            }
        }

        private static void ExistingMaterialUsageWorkbookSurvivesBudgetFailure(string path, string sentinel)
        {
            var rows = new List<MaterialUsageRow>();
            var hostile = new string('&', 32767);
            for (var i = 0; i < 24; i++)
            {
                var row = new MaterialUsageRow
                {
                    Floor = hostile,
                    MaterialName = hostile,
                    UnitHint = "m",
                    Component = hostile,
                    Category = hostile,
                    FamilyName = hostile,
                    ElementCount = 1,
                    LengthM = 1d,
                    AreaM2 = 1d,
                    VolumeM3 = 1d,
                    MassKg = 1d,
                    ProjectId = hostile,
                    DrawingFingerprint = hostile
                };
                row.ElementIds.Add(hostile);
                row.SourceHandles.Add(hostile);
                rows.Add(row);
            }

            try
            {
                MaterialUsageXlsxExporter.Export(path, rows);
                throw new InvalidOperationException("Material XLSX worksheet budget unexpectedly published.");
            }
            catch (InvalidDataException error) when (error.Message.Contains("Material XLSX worksheet exceeds", StringComparison.Ordinal)) { }

            var preserved = File.ReadAllText(path, Encoding.UTF8);
            if (!preserved.Contains(sentinel, StringComparison.Ordinal))
                throw new InvalidOperationException("Material XLSX bounded smoke: destination sentinel was replaced after failure.");
            var directory = Path.GetDirectoryName(path)!;
            var pattern = "." + Path.GetFileName(path) + ".*.tmp";
            if (Directory.GetFiles(directory, pattern).Length != 0)
                throw new InvalidOperationException("Material XLSX bounded smoke: owned temp artifact leaked after failure.");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}

