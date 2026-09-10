using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarCsvBoundedPublicationSmoke
    {
        private const int MaxCsvBytes = 16 * 1024 * 1024;

        [ModuleInitializer]
        internal static void Initialize()
        {
            OversizedPayloadFailsClosed();
            OversizedExportPreservesDestination();
        }

        private static void OversizedPayloadFailsClosed()
        {
            var row = CanonicalRow(new string('x', MaxCsvBytes));
            Throws<InvalidDataException>(() => RebarCsvExporter.ToCsv(new[] { row }));
        }

        private static void OversizedExportPreservesDestination()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-rebar-csv-bound-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "bbs.csv");
            File.WriteAllText(path, "SENTINEL");
            try
            {
                var row = CanonicalRow(new string('x', MaxCsvBytes));
                Throws<InvalidDataException>(() => RebarCsvExporter.Export(path, new[] { row }));
                Equal("SENTINEL", File.ReadAllText(path));
                Equal(1, Directory.GetFiles(directory).Length);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static RebarScheduleRow CanonicalRow(string barMark)
        {
            return new RebarScheduleRow
            {
                ElementId = "E1",
                BarMark = barMark,
                ShapeCode = "00",
                Notation = "1D16",
                DiameterMm = 16d,
                Quantity = 1,
                CuttingLengthM = 2d,
                TotalLengthM = 2d,
                UnitWeightKgM = 1d,
                NetWeightKg = 2d,
                WastePercent = 0d,
                TotalWeightKg = 2d,
                FabricationStatus = "Approved",
                FabricationStandardCode = "STD",
                FabricationDetailingRevision = "REV"
            };
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
