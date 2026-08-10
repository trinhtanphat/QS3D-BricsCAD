using System;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GridCommands
    {
        [CommandMethod("QS3DGRID", CommandFlags.UsePickSet)]
        public void CaptureGrid()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            int count;
            try
            {
                var snapshots = Cad.EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0)
                {
                    TryWriteMessage(document, "\nQS3D Grid: chọn LINE/ARC trục tham chiếu rồi chạy lại QS3DGRID.");
                    return;
                }

                var invalid = snapshots
                    .Where(x => !IsSupportedGridSource(x.EntityType) ||
                                !x.LengthDrawingUnits.HasValue ||
                                double.IsNaN(x.LengthDrawingUnits.Value) ||
                                double.IsInfinity(x.LengthDrawingUnits.Value) ||
                                !(x.LengthDrawingUnits.Value > 0d))
                    .ToArray();
                if (invalid.Length > 0)
                {
                    var kinds = string.Join(", ", invalid.Select(x => x.EntityType).Distinct(StringComparer.OrdinalIgnoreCase));
                    TryWriteMessage(document, "\nQS3D Grid: chỉ nhận LINE/ARC có chiều dài hữu hạn dương. Selection không hợp lệ: " + kinds + ".");
                    return;
                }

                count = SemanticCaptureService.Capture(document, ElementCategory.Grid);
            }
            catch (Exception ex)
            {
                ReportOperationFailure(document, "QS3DGRID lỗi: " + ex.Message);
                return;
            }

            FinalizeUi(document, count);
        }

        private static void FinalizeUi(Document document, int count)
        {
            var status = "Grid/Trục: đã capture " + count + " semantic reference(s). Grid hiện là reference/takeoff semantic, không sinh native 3D.";
            try
            {
                PaletteCoordinator.RefreshProject();
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception ex)
            {
                TryWriteMessage(document, "\nQS3D " + status + " UI sync warning: " + ex.Message);
            }
        }

        private static void ReportOperationFailure(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            TryWriteMessage(document, "\n" + message);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }

        private static bool IsSupportedGridSource(string entityType) =>
            string.Equals(entityType, "Line", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entityType, "Arc", StringComparison.OrdinalIgnoreCase);
    }
}
