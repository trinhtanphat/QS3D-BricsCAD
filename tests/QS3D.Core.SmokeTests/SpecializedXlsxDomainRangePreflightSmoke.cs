using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

internal static class SpecializedXlsxDomainRangePreflightSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        MaterialUsageRejectsNegativeCountAndMetric();
        DoorOpeningRejectsNegativeCountAndNonPositiveWidth();
        CurtainRejectsNegativeCountAndInvertedRange();
    }

    private static void MaterialUsageRejectsNegativeCountAndMetric()
    {
        var negativeCount = new MaterialUsageRow
        {
            UnitHint = "m",
            ElementCount = -1,
            LengthM = 1d
        };
        RejectsBeforePublication(
            path => MaterialUsageXlsxExporter.Export(path, new[] { negativeCount }),
            "Material Usage negative element count");

        var negativeMetric = new MaterialUsageRow
        {
            UnitHint = "m²",
            ElementCount = 1,
            ElementIds = { "E-1" },
            AreaM2 = -1d
        };
        RejectsBeforePublication(
            path => MaterialUsageXlsxExporter.Export(path, new[] { negativeMetric }),
            "Material Usage negative area");
    }

    private static void DoorOpeningRejectsNegativeCountAndNonPositiveWidth()
    {
        var negativeCount = ValidDoorRow();
        negativeCount.Count = -1;
        RejectsBeforePublication(
            path => DoorOpeningXlsxExporter.Export(path, new[] { negativeCount }),
            "Door/opening negative count");

        var zeroWidth = ValidDoorRow();
        zeroWidth.WidthM = 0d;
        RejectsBeforePublication(
            path => DoorOpeningXlsxExporter.Export(path, new[] { zeroWidth }),
            "Door/opening non-positive width");
    }

    private static void CurtainRejectsNegativeCountAndInvertedRange()
    {
        var negativeCount = ValidCurtainRow();
        negativeCount.PanelCount = -1;
        RejectsBeforePublication(
            path => CurtainWallXlsxExporter.Export(path, new[] { negativeCount }),
            "Curtain negative panel count");

        var invertedRange = ValidCurtainRow();
        invertedRange.MinimumClearPanelWidthM = 2d;
        invertedRange.MaximumClearPanelWidthM = 1d;
        RejectsBeforePublication(
            path => CurtainWallXlsxExporter.Export(path, new[] { invertedRange }),
            "Curtain inverted clear-panel width range");
    }

    private static DoorOpeningScheduleRow ValidDoorRow()
    {
        return new DoorOpeningScheduleRow
        {
            WidthM = 0.9d,
            HeightM = 2.1d,
            SillHeightM = 0d,
            ThicknessM = 0.1d,
            Count = 1,
            OpeningAreaM2 = 1.89d,
            HostCount = 1
        };
    }

    private static CurtainWallScheduleRow ValidCurtainRow()
    {
        return new CurtainWallScheduleRow
        {
            WallCount = 1,
            TotalWallLengthM = 3d,
            GrossWallAreaM2 = 7d,
            OpeningAreaM2 = 1d,
            NetGlassAreaM2 = 5d,
            FrameFaceAreaM2 = 1d,
            FrameLengthM = 8d,
            PanelCount = 4,
            VerticalFrameCount = 3,
            HorizontalFrameCount = 2,
            MinimumClearPanelWidthM = 0.5d,
            MaximumClearPanelWidthM = 1d,
            MinimumClearPanelHeightM = 0.5d,
            MaximumClearPanelHeightM = 2d,
            ElementIds = { "CW-DOMAIN-RANGE-1" },
            SourceHandles = { "CW-DOMAIN-RANGE-HANDLE-1" }
        };
    }

    private static void RejectsBeforePublication(Action<string> export, string label)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "qs3d-specialized-xlsx-range-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "result.xlsx");

        try
        {
            try
            {
                export(path);
            }
            catch (ArgumentOutOfRangeException)
            {
                Require(!Directory.Exists(directory), label + " must fail before creating its destination directory.");
                Require(!File.Exists(path), label + " must fail before publishing a workbook.");
                return;
            }

            throw new InvalidOperationException(label + " must fail closed at the XLSX publication boundary.");
        }
        finally
        {
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
            catch
            {
                // Best-effort cleanup only; assertions above own publication correctness.
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
