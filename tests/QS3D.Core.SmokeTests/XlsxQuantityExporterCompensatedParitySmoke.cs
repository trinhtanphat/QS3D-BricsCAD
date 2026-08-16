using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxQuantityExporterCompensatedParitySmoke
    {
        internal static void Run()
        {
            AcceptsRepresentableCompensatedEd2Totals();
            RejectsNaiveRoundedEd2Totals();
        }

        private static void AcceptsRepresentableCompensatedEd2Totals()
        {
            var detail = PrecisionDetails();
            var summary = Summary(10000000000000002d);
            var path = TempXlsxPath("accept");
            try
            {
                XlsxQuantityExporter.ExportEd2(path, detail, new[] { summary });
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                    throw new InvalidOperationException("Compensated ED2 parity must produce a non-empty XLSX package.");
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static void RejectsNaiveRoundedEd2Totals()
        {
            var detail = PrecisionDetails();
            var naiveSummary = Summary(10000000000000000d);
            var path = TempXlsxPath("reject");
            try
            {
                Throws<InvalidDataException>(() => XlsxQuantityExporter.ExportEd2(path, detail, new[] { naiveSummary }));
                if (File.Exists(path))
                    throw new InvalidOperationException("ED2 parity rejection must occur before publishing an XLSX package.");
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static IReadOnlyList<QuantityReportRow> PrecisionDetails()
        {
            return new[]
            {
                Detail("precision-1", "1", 10000000000000000d),
                Detail("precision-2", "2", 1d),
                Detail("precision-3", "3", 1d)
            };
        }

        private static QuantityReportRow Detail(string elementId, string handle, double value)
        {
            var row = IdentityRow();
            row.Count = 1;
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add(handle);
            PopulateQuantities(row, value);
            row.MassKg = value;
            return row;
        }

        private static QuantityReportRow Summary(double value)
        {
            var row = IdentityRow();
            row.Count = 3;
            row.ElementIds.Add("precision-1");
            row.ElementIds.Add("precision-2");
            row.ElementIds.Add("precision-3");
            row.SourceHandles.Add("1");
            row.SourceHandles.Add("2");
            row.SourceHandles.Add("3");
            PopulateQuantities(row, value);
            row.MassKg = value;
            return row;
        }

        private static QuantityReportRow IdentityRow()
        {
            return new QuantityReportRow
            {
                Floor = "L1",
                Zone = "Z1",
                Category = "Beam",
                FamilyId = "F-PRECISION",
                FamilyName = "Precision Beam",
                ElementName = "Precision Beam",
                Material = "Concrete",
                DrawingFingerprint = "PRECISION-FINGERPRINT"
            };
        }

        private static void PopulateQuantities(QuantityReportRow row, double value)
        {
            row.GrossConcreteM3 = value;
            row.DeductionM3 = value;
            row.NetConcreteM3 = value;
            row.FormworkM2 = value;
            row.LengthM = value;
            row.OuterPerimeterM = value;
            row.InnerPerimeterM = value;
            row.DoorAreaM2 = value;
            row.SideAreaM2 = value;
            row.BottomAreaM2 = value;
            row.TopAreaM2 = value;
            row.OtherAreaM2 = value;
        }

        private static string TempXlsxPath(string suffix)
        {
            return Path.Combine(
                Path.GetTempPath(),
                "qs3d-ed2-compensated-" + suffix + "-" + Guid.NewGuid().ToString("N") + ".xlsx");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup must not hide the regression result.
            }
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
                throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
            }
            catch (TException)
            {
            }
        }
    }

    internal static class XlsxQuantityExporterCompensatedParityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            XlsxQuantityExporterCompensatedParitySmoke.Run();
        }
    }
}
