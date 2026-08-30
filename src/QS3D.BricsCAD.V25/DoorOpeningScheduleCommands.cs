using System;
using System.IO;
using System.Linq;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class DoorOpeningScheduleCommands
    {
        [CommandMethod("QS3DDOORXLSX", CommandFlags.Modal)]
        public void ExportDoorOpeningSchedule()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var drawingName = string.IsNullOrWhiteSpace(document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất bảng Cửa / Lỗ mở",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-Cua-Lo-Mo.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "Door XLSX: BLOCKED • cần một QS3D project hiện hữu; lệnh export không tạo project mới.");
                    return;
                }
                var snapshot = ProjectStateSnapshot.CreateDetachedCopy(project);
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(snapshot);
                var rows = DoorOpeningScheduleBuilder.Build(snapshot);
                if (rows.Count == 0)
                {
                    Report(document, "Door XLSX: project chưa có Cửa/Lỗ mở semantic để xuất.");
                    return;
                }

                var count = 0;
                var area = new CompensatedExportAreaTotal();
                foreach (var row in rows)
                {
                    count = QuantityReportMath.AddCount(count, row.Count);
                    area.Add(row.OpeningAreaM2, "Door/Opening export area");
                }
                var totalAreaM2 = area.Value("Door/Opening export area");
                var hosts = rows.SelectMany(x => x.HostIds).Distinct(StringComparer.OrdinalIgnoreCase).Count();

                DoorOpeningXlsxExporter.Export(dialog.FileName, rows);

                var status = "Door XLSX: " + rows.Count + " nhóm • " + count + " Cửa/Lỗ • " + totalAreaM2.ToString("0.###") + " m² • " + hosts + " host.";
                FinalizeUi(document, status, dialog.FileName);
            }
            catch (System.Exception)
            {
                Report(document, "QS3DDOORXLSX lỗi: không thể xuất bảng Cửa / Lỗ mở.");
            }
        }

        private sealed class CompensatedExportAreaTotal
        {
            private double _sum;
            private double _compensation;

            internal void Add(double value, string label)
            {
                var incoming = QuantityReportMath.NonNegative(value, label);
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");

                var nextSum = _sum + incoming;
                if (double.IsNaN(nextSum) || double.IsInfinity(nextSum))
                    throw new OverflowException("Door/opening export area total overflow: " + label);

                var correction = Math.Abs(_sum) >= Math.Abs(incoming)
                    ? (_sum - nextSum) + incoming
                    : (incoming - nextSum) + _sum;
                var nextCompensation = _compensation + correction;
                if (double.IsNaN(nextCompensation) || double.IsInfinity(nextCompensation))
                    throw new OverflowException("Door/opening export area compensation overflow: " + label);

                _sum = nextSum == 0d ? 0d : nextSum;
                _compensation = nextCompensation == 0d ? 0d : nextCompensation;
            }

            internal double Value(string label)
            {
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var result = _sum + _compensation;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Door/opening export area total overflow: " + label);
                if (_compensation != 0d && result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation))
                    throw new OverflowException("Door/opening export area total lost a non-zero compensation at floating-point precision: " + label);
                if (_sum != 0d && result == _compensation)
                    throw new OverflowException("Door/opening export area total lost a non-zero accumulated value at floating-point precision: " + label);
                return result == 0d ? 0d : result;
            }

            private static bool IsStrictlyBelowHalfUlp(double current, double compensation)
            {
                if (current <= 0d || compensation == 0d) return false;
                var currentBits = BitConverter.DoubleToInt64Bits(current);
                var adjacentBits = compensation > 0d ? currentBits + 1L : currentBits - 1L;
                var adjacent = BitConverter.Int64BitsToDouble(adjacentBits);
                var spacing = Math.Abs(adjacent - current);
                return Math.Abs(compensation) < spacing / 2d;
            }
        }

        private static void FinalizeUi(Document document, string status, string fileName)
        {
            try
            {
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + "\n" + fileName);
            }
            catch (System.Exception)
            {
                try
                {
                    document.Editor.WriteMessage("\n[QS3D] Cảnh báo UI sau export: không thể cập nhật giao diện sau khi file đã được xuất.");
                }
                catch
                {
                    // Export has already committed; UI reporting is best effort only.
                }
            }
        }

        private static void Report(Document document, string status)
        {
            try { PaletteCoordinator.SetStatus(status); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + status); } catch { }
        }
    }
}
