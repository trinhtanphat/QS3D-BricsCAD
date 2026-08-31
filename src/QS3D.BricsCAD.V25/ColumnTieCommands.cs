using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ColumnTieCommands
    {
        private const string SelectionGuidance = "Tie 3D: chọn Column semantic có closed rectangle POLYLINE; khai báo RebarTieDiameterMm/RebarTieSpacingMm nếu cần override.";
        private const string OperationFailure = "QS3DREBARTIES3D lỗi: không thể tạo/cập nhật đai cột. Kiểm tra selection, project semantic và dữ liệu rebar rồi thử lại.";
        private const string UiSyncWarning = "UI sync warning: đã cập nhật đai cột nhưng đồng bộ giao diện chưa hoàn tất. Dữ liệu CAD/project đã được giữ nguyên; hãy refresh giao diện.";

        [CommandMethod("QS3DREBARTIES3D", CommandFlags.UsePickSet)]
        public void BuildColumnTies()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                // Capture PICKFIRST once before binding the canonical project. The same
                // snapshot is passed into native generation, so admission and mutation
                // cannot observe two different implied-selection sets.
                var selectedIds = CadSelectionGuard.ReadImpliedSelection(document);
                if (selectedIds.Length == 0)
                {
                    Report(document, SelectionGuidance);
                    return;
                }

                var project = ExistingProjectMutationContext.Require(document, "Column Tie 3D");
                var count = ColumnTieSolidBuilder.BuildSelected(document, project, selectedIds);
                var message = count == 0
                    ? SelectionGuidance
                    : "Tie 3D: đã tạo/cập nhật " + count + " đai cột.";
                FinalizeUi(document, message);
            }
            catch (Exception)
            {
                Report(document, OperationFailure);
            }
        }

        private static void FinalizeUi(Document document, string message)
        {
            try
            {
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception)
            {
                TryWriteMessage(document, "\nQS3D " + message + " " + UiSyncWarning);
            }
        }

        private static void Report(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); }
            catch { }
            TryWriteMessage(document, "\nQS3D " + message);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); }
            catch { }
        }
    }
}
