using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarCsvNumericFidelitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            RoundTripFidelityIsCultureInvariant();
            SignedZeroIsCanonical();
        }

        private static void RoundTripFidelityIsCultureInvariant()
        {
            const double expected = 0.123456789d;
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var culture = CultureInfo.GetCultureInfo("fr-FR");
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var fields = DataFields(RebarCsvExporter.ToCsv(new[] { Row(expected, 0d) }));
                var actual = double.Parse(fields[6], NumberStyles.Float, CultureInfo.InvariantCulture);
                Equal(
                    BitConverter.DoubleToInt64Bits(expected),
                    BitConverter.DoubleToInt64Bits(actual),
                    "Rebar CSV cutting length no longer round-trips bit-exactly.");
                Equal("0.123456789", fields[6], "Rebar CSV numeric text changed culture or precision.");
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        private static void SignedZeroIsCanonical()
        {
            var fields = DataFields(RebarCsvExporter.ToCsv(new[] { Row(1d, -0d) }));
            Equal("0", fields[10], "Rebar CSV must serialize signed zero canonically.");
        }

        private static RebarScheduleRow Row(double cuttingLengthM, double wastePercent)
        {
            return new RebarScheduleRow
            {
                ElementId = "E-1",
                BarMark = "B-1",
                ShapeCode = "00",
                Notation = "1T16",
                DiameterMm = 16d,
                Quantity = 1,
                CuttingLengthM = cuttingLengthM,
                TotalLengthM = cuttingLengthM,
                UnitWeightKgM = 1d,
                NetWeightKg = cuttingLengthM,
                WastePercent = wastePercent,
                TotalWeightKg = cuttingLengthM,
                FabricationStatus = "Ready",
                FabricationStandardCode = "STD",
                FabricationDetailingRevision = "R1"
            };
        }

        private static string[] DataFields(string csv)
        {
            var lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length != 2)
                throw new InvalidOperationException("Expected exactly one Rebar CSV data row.");
            var fields = lines[1].Split(',');
            if (fields.Length != 15)
                throw new InvalidOperationException("Unexpected Rebar CSV column count: " + fields.Length + ".");
            return fields;
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
