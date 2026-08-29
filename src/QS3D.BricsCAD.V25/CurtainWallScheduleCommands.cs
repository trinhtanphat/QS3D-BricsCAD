using System;
using System.IO;
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
    public sealed class CurtainWallScheduleCommands
    {
        [CommandMethod("QS3DCURTAINXLSX", CommandFlags.Modal)]
        public void ExportCurtainSchedule()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var drawingName = string.IsNullOrWhiteSpace(document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất bảng Vách Kính",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-Vach-Kinh.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "Curtain XLSX: BLOCKED • cần một QS3D project hiện hữu; lệnh export không tạo project mới.");
                    return;
                }
                var snapshot = ProjectStateSnapshot.CreateDetachedCopy(project);
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(snapshot);
                var rows = CurtainWallScheduleBuilder.Build(snapshot);
                if (rows.Count == 0)
                {
                    Report(document, "Curtain XLSX: chưa có Vách Kính semantic để xuất.");
                    return;
                }

                var panels = 0;
                var glass = new CompensatedStatusTotal();
                var frame = new CompensatedStatusTotal();
                foreach (var row in rows)
                {
                    panels = QuantityReportMath.AddCount(panels, row.PanelCount);
                    glass.Add(row.NetGlassAreaM2, "Curtain export net glass area");
                    frame.Add(row.FrameLengthM, "Curtain export frame length");
                }
                var glassTotal = glass.Value("Curtain export net glass area");
                var frameTotal = frame.Value("Curtain export frame length");

                CurtainWallXlsxExporter.Export(dialog.FileName, rows);

                var status = "Curtain XLSX: " + rows.Count + " nhóm • " + panels + " panel • " + glassTotal.ToString("0.###") + " m² kính net • " + frameTotal.ToString("0.###") + " m khung.";
                FinalizeUi(document, status, dialog.FileName);
            }
            catch (System.Exception)
            {
                Report(document, "QS3DCURTAINXLSX lỗi: không thể xuất bảng Vách Kính.");
            }
        }

        private sealed class CompensatedStatusTotal
        {
            private double _sum;
            private double _compensation;

            internal void Add(double value, string label)
            {
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var incoming = QuantityReportMath.NonNegative(value, label);

                var result = _sum + incoming;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Curtain export status total overflowed: " + label + ".");

                var correction = Math.Abs(_sum) >= Math.Abs(incoming)
                    ? (_sum - result) + incoming
                    : (incoming - result) + _sum;
                var nextCompensation = _compensation + correction;
                if (double.IsNaN(nextCompensation) || double.IsInfinity(nextCompensation))
                    throw new OverflowException("Curtain export status compensation overflowed: " + label + ".");

                _sum = result == 0d ? 0d : result;
                _compensation = nextCompensation == 0d ? 0d : nextCompensation;
            }

            internal double Value(string label)
            {
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var result = _sum + _compensation;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Curtain export status total overflowed: " + label + ".");
                if (_compensation != 0d && result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation))
                    throw new OverflowException("Curtain export status total lost a non-zero compensation at floating-point precision: " + label + ".");
                if (_sum != 0d && result == _compensation)
                    throw new OverflowException("Curtain export status total lost a non-zero accumulated value at floating-point precision: " + label + ".");
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
