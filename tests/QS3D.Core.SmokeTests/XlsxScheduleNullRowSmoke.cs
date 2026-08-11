using System;
using System.IO;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxScheduleNullRowSmoke
    {
        internal static void Run()
        {
            AssertRejectsNullRow(
                "door-opening",
                path => DoorOpeningXlsxExporter.Export(path, new DoorOpeningScheduleRow[] { null! }));
            AssertRejectsNullRow(
                "material-usage",
                path => MaterialUsageXlsxExporter.Export(path, new MaterialUsageRow[] { null! }));
            AssertRejectsNullRow(
                "curtain-wall",
                path => CurtainWallXlsxExporter.Export(path, new CurtainWallScheduleRow[] { null! }));
            AssertRejectsNullRow(
                "room-finish",
                path => RoomFinishXlsxExporter.Export(path, new RoomFinishScheduleRow[] { null! }));
        }

        private static void AssertRejectsNullRow(string exportName, Action<string> export)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-null-row-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, exportName + ".xlsx");
                var sentinel = "preserve-existing-destination-" + exportName;
                File.WriteAllText(destination, sentinel);

                AssertNullRowArgument(exportName, destination, export);
                if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal))
                    throw new InvalidOperationException(exportName + " null-row validation replaced an existing destination file.");

                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                AssertNullRowArgument(exportName, Path.Combine(untouchedDirectory, "invalid.xlsx"), export);
                if (Directory.Exists(untouchedDirectory))
                    throw new InvalidOperationException(exportName + " null-row validation touched the filesystem before failing.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertNullRowArgument(string exportName, string path, Action<string> export)
        {
            try
            {
                export(path);
            }
            catch (ArgumentException ex)
            {
                if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                    throw new InvalidOperationException(exportName + " null-row validation must identify the rows argument.", ex);
                if (ex.Message.IndexOf("row index: 0", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(exportName + " null-row validation must identify the zero-based row index.", ex);
                return;
            }

            throw new InvalidOperationException(exportName + " exporter accepted a null schedule row.");
        }
    }
}
