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
            AcceptsOrderSensitiveCompensatedEd2Totals();
            RejectsNaiveRoundedEd2Totals();
            PreservesNullableMassSemantics();
            AcceptsOrdinaryEd2Totals();
            RejectsNonFiniteDetailBeforePublication();
            RejectsAggregateOverflowBeforePublication();
        }

        private static void AcceptsRepresentableCompensatedEd2Totals()
        {
            var detail = PrecisionDetails(10000000000000000d, 1d, 1d);
            var summary = Summary(10000000000000002d, 10000000000000002d);
            ExportMustSucceed(detail, summary, "huge-small-small");
        }

        private static void AcceptsOrderSensitiveCompensatedEd2Totals()
        {
            var detail = PrecisionDetails(1d, 10000000000000000d, 1d);
            var summary = Summary(10000000000000002d, 10000000000000002d);
            ExportMustSucceed(detail, summary, "small-huge-small");
        }

        private static void RejectsNaiveRoundedEd2Totals()
        {
            var detail = PrecisionDetails(10000000000000000d, 1d, 1d);
            var naiveSummary = Summary(10000000000000000d, 10000000000000000d);
            ExportMustFail<InvalidDataException>(detail, naiveSummary, "naive-rounded");
        }

        private static void PreservesNullableMassSemantics()
        {
            var detail = new[]
            {
                Detail("precision-1", "1", 2d, 2d),
                Detail("precision-2", "2", 3d, null),
                Detail("precision-3", "3", 5d, 5d)
            };
            var summary = Summary(10d, null);
            ExportMustSucceed(detail, summary, "nullable-mass");

            var wrongSummary = Summary(10d, 7d);
            ExportMustFail<InvalidDataException>(detail, wrongSummary, "nullable-mass-wrong-summary");
        }

        private static void AcceptsOrdinaryEd2Totals()
        {
            var detail = PrecisionDetails(1.25d, 2.5d, 3.75d);
            var summary = Summary(7.5d, 7.5d);
            ExportMustSucceed(detail, summary, "ordinary");
        }

        private static void RejectsNonFiniteDetailBeforePublication()
        {
            var detail = PrecisionDetails(1d, 2d, 3d);
            detail[1].GrossConcreteM3 = double.PositiveInfinity;
            var summary = Summary(6d, 6d);
            ExportMustFail<InvalidDataException>(detail, summary, "nonfinite-detail");
        }

        private static void RejectsAggregateOverflowBeforePublication()
        {
            var detail = PrecisionDetails(double.MaxValue, double.MaxValue, 1d);
            var summary = Summary(double.MaxValue, double.MaxValue);
            ExportMustFail<InvalidDataException>(detail, summary, "aggregate-overflow");
        }

        private static QuantityReportRow[] PrecisionDetails(double first, double second, double third)
        {
            return new[]
            {
                Detail("precision-1", "1", first, first),
                Detail("precision-2", "2", second, second),
                Detail("precision-3", "3", third, third)
            };
        }

        private static QuantityReportRow Detail(string elementId, string handle, double value, double? massKg)
        {
            var row = IdentityRow();
            row.Count = 1;
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add(handle);
            PopulateQuantities(row, value);
            row.MassKg = massKg;
            return row;
        }

        private static QuantityReportRow Summary(double value, double? massKg)
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
            row.MassKg = massKg;
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

        private static void ExportMustSucceed(
            IReadOnlyList<QuantityReportRow> detail,
            QuantityReportRow summary,
            string suffix)
        {
            var path = TempXlsxPath(suffix);
            try
            {
                XlsxQuantityExporter.ExportEd2(path, detail, new[] { summary });
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                    throw new InvalidOperationException("Successful compensated ED2 parity must publish a non-empty XLSX package.");
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static void ExportMustFail<TException>(
            IReadOnlyList<QuantityReportRow> detail,
            QuantityReportRow summary,
            string suffix)
            where TException : Exception
        {
            var path = TempXlsxPath(suffix);
            try
            {
                Throws<TException>(() => XlsxQuantityExporter.ExportEd2(path, detail, new[] { summary }));
                if (File.Exists(path))
                    throw new InvalidOperationException("ED2 parity rejection must occur before publishing an XLSX package.");
            }
            finally
            {
                TryDelete(path);
            }
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
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
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
