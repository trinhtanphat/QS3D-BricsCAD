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

            try
            {
                var snapshots = Cad.EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3D Grid: chọn LINE/ARC trục tham chiếu rồi chạy lại QS3DGRID.");
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
                    document.Editor.WriteMessage("\nQS3D Grid: chỉ nhận LINE/ARC có chiều dài hữu hạn dương. Selection không hợp lệ: " + kinds + ".");
                    return;
                }

                var count = SemanticCaptureService.Capture(document, ElementCategory.Grid);
                PaletteCoordinator.RefreshProject();
                var status = "Grid/Trục: đã capture " + count + " semantic reference(s). Grid hiện là reference/takeoff semantic, không sinh native 3D.";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DGRID lỗi: " + ex.Message;
                try { PaletteCoordinator.SetStatus(message); } catch { }
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static bool IsSupportedGridSource(string entityType) =>
            string.Equals(entityType, "Line", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entityType, "Arc", StringComparison.OrdinalIgnoreCase);
    }
}
