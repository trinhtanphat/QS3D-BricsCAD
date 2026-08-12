using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningXlsxCellTextLimitSmoke
    {
        private const int MaxCellTextLength = 32767;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AcceptsExactLimit();
            RejectsOversizedDirectCellBeforeFilesystemMutation();
            RejectsOversizedElementIdsBeforeFilesystemMutation();
            RejectsOversizedHostIdsBeforeFilesystemMutation();
        }

        private static void AcceptsExactLimit()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-door-xlsx-cell-limit-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "exact-limit.xlsx");
            try
            {
                var row = new DoorOpeningScheduleRow
                {
                    Floor = new string('F', MaxCellTextLength),
                    Category = "Door",
                    FamilyName = "Door family",
                    Material = "Glass",
                    WidthM = 0.9d,
                    HeightM = 2.2d
                };
                row.ElementIds.Add(new string('E', 16383));
                row.ElementIds.Add(new string('I', 16383));
                row.HostIds.Add(new string('H', 16383));
                row.HostIds.Add(new string('W', 16383));

                DoorOpeningXlsxExporter.Export(path, new[] { row });
                if (!File.Exists(path))
                    throw new InvalidOperationException("Door/opening XLSX rejected cell text at the exact Excel limit.");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void RejectsOversizedDirectCellBeforeFilesystemMutation()
        {
            var directory = NewMissingDirectory();
            try
            {
                var row = new DoorOpeningScheduleRow
                {
                    Floor = "L1",
                    Category = "Door",
                    FamilyName = new string('F', MaxCellTextLength + 1),
                    Material = "Glass",
                    WidthM = 0.9d,
                    HeightM = 2.2d
                };

                Throws<ArgumentOutOfRangeException>(() =>
                    DoorOpeningXlsxExporter.Export(Path.Combine(directory, "direct.xlsx"), new[] { row }));
                AssertDirectoryWasNotCreated(directory, "oversized direct cell");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void RejectsOversizedElementIdsBeforeFilesystemMutation()
        {
            var directory = NewMissingDirectory();
            try
            {
                var row = OrdinaryRow();
                row.ElementIds.Add(new string('E', 16384));
                row.ElementIds.Add(new string('I', 16383));

                Throws<ArgumentOutOfRangeException>(() =>
                    DoorOpeningXlsxExporter.Export(Path.Combine(directory, "element-ids.xlsx"), new[] { row }));
                AssertDirectoryWasNotCreated(directory, "oversized Element IDs cell");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void RejectsOversizedHostIdsBeforeFilesystemMutation()
        {
            var directory = NewMissingDirectory();
            try
            {
                var row = OrdinaryRow();
                row.HostIds.Add(new string('H', 16384));
                row.HostIds.Add(new string('W', 16383));

                Throws<ArgumentOutOfRangeException>(() =>
                    DoorOpeningXlsxExporter.Export(Path.Combine(directory, "host-ids.xlsx"), new[] { row }));
                AssertDirectoryWasNotCreated(directory, "oversized Host IDs cell");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static DoorOpeningScheduleRow OrdinaryRow()
        {
            return new DoorOpeningScheduleRow
            {
                Floor = "L1",
                Category = "Door",
                FamilyName = "Door family",
                Material = "Glass",
                WidthM = 0.9d,
                HeightM = 2.2d
            };
        }

        private static string NewMissingDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-door-xlsx-cell-limit-" + Guid.NewGuid().ToString("N"));
            DeleteDirectory(directory);
            return directory;
        }

        private static void AssertDirectoryWasNotCreated(string directory, string scenario)
        {
            if (Directory.Exists(directory))
                throw new InvalidOperationException("Door/opening XLSX created filesystem state before rejecting " + scenario + ".");
        }

        private static void DeleteDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
