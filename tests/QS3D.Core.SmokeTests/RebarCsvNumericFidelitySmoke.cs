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
            EveryDoubleColumnUsesRoundTripText();
            ExtremeFiniteValuesRoundTrip();
            SignedZeroIsCanonical();
            ExistingNumericValidationRemainsFailClosed();
        }

        private static void RoundTripFidelityIsCultureInvariant()
        {
            const double expected = 0.123456789d;
            WithCulture("fr-FR", () =>
            {
                var fields = DataFields(RebarCsvExporter.ToCsv(new[] { Row(expected, 0d) }));
                BitEqual(expected, fields[6], "CuttingLengthM");
                Equal("0.123456789", fields[6], "Rebar CSV numeric text changed culture or precision.");
            });
        }

        private static void EveryDoubleColumnUsesRoundTripText()
        {
            var row = Row(0.123456789d, 0.987654321d);
            row.DiameterMm = 16.000000000000004d;
            row.TotalLengthM = 123456789.12345679d;
            row.UnitWeightKgM = 0.456789123456789d;
            row.NetWeightKg = 987654321.9876543d;
            row.TotalWeightKg = 1.2345678901234567d;

            WithCulture("de-DE", () =>
            {
                var fields = DataFields(RebarCsvExporter.ToCsv(new[] { row }));
                BitEqual(row.DiameterMm, fields[4], "DiameterMm");
                BitEqual(row.CuttingLengthM, fields[6], "CuttingLengthM");
                BitEqual(row.TotalLengthM, fields[7], "TotalLengthM");
                BitEqual(row.UnitWeightKgM, fields[8], "UnitWeightKgM");
                BitEqual(row.NetWeightKg, fields[9], "NetWeightKg");
                BitEqual(row.WastePercent, fields[10], "WastePercent");
                BitEqual(row.TotalWeightKg, fields[11], "TotalWeightKg");
            });
        }

        private static void ExtremeFiniteValuesRoundTrip()
        {
            var tiny = double.Epsilon;
            var row = Row(tiny, tiny);
            row.DiameterMm = double.MaxValue;
            row.TotalLengthM = double.MaxValue;
            row.UnitWeightKgM = tiny;
            row.NetWeightKg = double.MaxValue;
            row.TotalWeightKg = double.MaxValue;

            var fields = DataFields(RebarCsvExporter.ToCsv(new[] { row }));
            BitEqual(row.DiameterMm, fields[4], "DiameterMm max finite");
            BitEqual(row.CuttingLengthM, fields[6], "CuttingLengthM subnormal");
            BitEqual(row.TotalLengthM, fields[7], "TotalLengthM max finite");
            BitEqual(row.UnitWeightKgM, fields[8], "UnitWeightKgM subnormal");
            BitEqual(row.NetWeightKg, fields[9], "NetWeightKg max finite");
            BitEqual(row.WastePercent, fields[10], "WastePercent subnormal");
            BitEqual(row.TotalWeightKg, fields[11], "TotalWeightKg max finite");
        }

        private static void SignedZeroIsCanonical()
        {
            var row = Row(1d, -0d);
            row.NetWeightKg = -0d;
            row.TotalWeightKg = -0d;
            var fields = DataFields(RebarCsvExporter.ToCsv(new[] { row }));
            Equal("0", fields[9], "NetWeightKg signed zero must serialize canonically.");
            Equal("0", fields[10], "WastePercent signed zero must serialize canonically.");
            Equal("0", fields[11], "TotalWeightKg signed zero must serialize canonically.");
        }

        private static void ExistingNumericValidationRemainsFailClosed()
        {
            Throws<ArgumentOutOfRangeException>(() =>
            {
                var row = Row(1d, 0d);
                row.CuttingLengthM = 0d;
                RebarCsvExporter.ToCsv(new[] { row });
            }, "Positive numeric validation changed while hardening formatting.");

            Throws<ArgumentOutOfRangeException>(() =>
            {
                var row = Row(1d, -1d);
                RebarCsvExporter.ToCsv(new[] { row });
            }, "Non-negative numeric validation changed while hardening formatting.");

            Throws<ArgumentOutOfRangeException>(() =>
            {
                var row = Row(1d, 0d);
                row.NetWeightKg = double.NaN;
                RebarCsvExporter.ToCsv(new[] { row });
            }, "Non-finite numeric validation changed while hardening formatting.");
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

        private static void BitEqual(double expected, string text, string field)
        {
            var actual = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
            Equal(
                BitConverter.DoubleToInt64Bits(expected),
                BitConverter.DoubleToInt64Bits(actual),
                "Rebar CSV " + field + " no longer round-trips bit-exactly.");
        }

        private static void WithCulture(string name, Action action)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var culture = CultureInfo.GetCultureInfo(name);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                action();
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
