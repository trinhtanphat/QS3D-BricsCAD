using System;
using System.IO;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarCsvStrictUtf8Smoke
    {
        internal static void Run()
        {
            RejectsInvalidUtf16BeforeEncoding();
            PreservesExistingTargetOnInvalidUtf16();
        }

        private static void RejectsInvalidUtf16BeforeEncoding()
        {
            var row = ValidRow();
            row.ElementId = "bad-\uD800-id";

            Throws<EncoderFallbackException>(() => RebarCsvExporter.ToCsv(new[] { row }));
        }

        private static void PreservesExistingTargetOnInvalidUtf16()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-rebar-csv-strict-utf8-" + Guid.NewGuid().ToString("N") + ".csv");
            var sentinel = new byte[] { 0x51, 0x53, 0x33, 0x44 };
            try
            {
                File.WriteAllBytes(path, sentinel);
                var row = ValidRow();
                row.FabricationStatus = "bad-\uD800-status";

                Throws<EncoderFallbackException>(() => RebarCsvExporter.Export(path, new[] { row }));

                var after = File.ReadAllBytes(path);
                Equal(sentinel.Length, after.Length);
                for (var index = 0; index < sentinel.Length; index++)
                    Equal(sentinel[index], after[index]);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static RebarScheduleRow ValidRow()
        {
            return new RebarScheduleRow
            {
                ElementId = "R1",
                BarMark = "B1",
                ShapeCode = "00",
                Notation = "4D16",
                DiameterMm = 16d,
                Quantity = 4,
                CuttingLengthM = 2d,
                TotalLengthM = 8d,
                UnitWeightKgM = 1.58d,
                NetWeightKg = 12.64d,
                WastePercent = 3d,
                TotalWeightKg = 13.0192d,
                FabricationStatus = "Qualified",
                FabricationStandardCode = "STD",
                FabricationDetailingRevision = "R1"
            };
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

            throw new InvalidOperationException("Expected exception: " + typeof(TException).FullName);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}
