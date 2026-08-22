using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SourceReconcileCommands
    {
        [CommandMethod("QS3DSYNCSOURCE", CommandFlags.UsePickSet)]
        public void SyncSource()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            SourceReconcileResult result;
            try
            {
                result = SourceReconcileService.ReconcileSelection(document);
            }
            catch (Exception ex)
            {
                ReportOperationFailure(document, "QS3DSYNCSOURCE lỗi: " + ex.Message);
                return;
            }

            FinalizeUi(document, result);
        }

        private static void FinalizeUi(Document document, SourceReconcileResult result)
        {
            var status = result.Elements == 0
                ? "Sync Source: chọn source CAD đang được QS3D theo dõi rồi chạy lại QS3DSYNCSOURCE."
                : "Sync Source: đã reconcile " + result.Elements + " semantic source • regenerate " + result.Regenerated + ". Generated host/rebar/curtain phụ thuộc đã được invalidate/remove an toàn; chạy QS3DBUILD3D hoặc workflow 3D tương ứng khi muốn dựng lại.";
            try
            {
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();
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
    }
}
