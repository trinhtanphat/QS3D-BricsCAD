using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

internal static class MaterialUsagePrimaryUnitIntegritySmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        SupportedUnitsSelectTheirMetrics();
        EmptyUnitKeepsLegacyZero();
        UnsupportedUnitFailsClosed();
        ExportRejectsUnsupportedUnitBeforePublication();
    }

    private static void SupportedUnitsSelectTheirMetrics()
    {
        var row = new MaterialUsageRow
        {
            LengthM = 1.25d,
            AreaM2 = 2.5d,
            VolumeM3 = 3.75d,
            MassKg = 4.5d
        };

        row.UnitHint = "m";
        Require(row.PrimaryQuantity == 1.25d, "Material Usage length unit must select LengthM.");

        row.UnitHint = "m²";
        Require(row.PrimaryQuantity == 2.5d, "Material Usage area unit must select AreaM2.");

        row.UnitHint = "m^3";
        Require(row.PrimaryQuantity == 3.75d, "Material Usage volume unit must select VolumeM3.");

        row.UnitHint = "kg";
        Require(row.PrimaryQuantity == 4.5d, "Material Usage mass unit must select MassKg.");
    }

    private static void EmptyUnitKeepsLegacyZero()
    {
        var row = new MaterialUsageRow
        {
            UnitHint = "",
            LengthM = 7d,
            AreaM2 = 8d,
            VolumeM3 = 9d,
            MassKg = 10d
        };

        Require(row.PrimaryQuantity == 0d, "Material Usage rows without a unit hint must keep the legacy zero primary quantity.");
    }

    private static void UnsupportedUnitFailsClosed()
    {
        var row = new MaterialUsageRow
        {
            UnitHint = "ea",
            ElementCount = 3,
            AreaM2 = 12d
        };

        ExpectInvalidOperation(
            () => _ = row.PrimaryQuantity,
            "A non-empty unsupported Material Usage unit must not fabricate a zero primary quantity.");
    }

    private static void ExportRejectsUnsupportedUnitBeforePublication()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "qs3d-material-usage-primary-unit-" + Guid.NewGuid().ToString("N") + ".xlsx");
        var row = new MaterialUsageRow
        {
            Floor = "L1",
            MaterialName = "Custom counted material",
            UnitHint = "pcs",
            Component = "Material",
            Category = "CustomQuantity",
            FamilyName = "Custom",
            ElementCount = 4,
            AreaM2 = 20d
        };

        try
        {
            ExpectInvalidOperation(
                () => MaterialUsageXlsxExporter.Export(path, new[] { row }),
                "Material Usage XLSX export must fail closed before publishing an unsupported primary unit.");
            Require(!File.Exists(path), "Unsupported Material Usage unit must not leave a published workbook behind.");
        }
        finally
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup only; the assertion above owns publication correctness.
            }
        }
    }

    private static void ExpectInvalidOperation(Action action, string message)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
