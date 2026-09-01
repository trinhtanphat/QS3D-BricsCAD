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
            catch (Exception)
            {
                ReportOperationFailure(document, "QS3DSYNCSOURCE lỗi: không thể reconcile source CAD đã chọn.");
                return;
            }

            FinalizeUi(document, result);
        }

        private static void FinalizeUi(Document document, SourceReconcileResult result)
        {
            var status = result.Elements == 0
                ? "Sync Source: chọn source CAD đang được QS3D theo dõi rồi chạy lại QS3DSYNCSOURCE."
                : "Sync Source: đã reconcile " + result.Elements + " semantic source • regenerate " + result.Regenerated + ". Generated host/rebar/curtain phụ thuộc đã được invalidate/remove an toàn; chạy QS3DBUILD3D hoặc workflow 3D tương ứng khi muốn dựng lại.";
            var uiSyncFailed = false;
            try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }
            try { document.Editor.Regen(); } catch { uiSyncFailed = true; }
            try { PaletteCoordinator.SetStatus(status); } catch { uiSyncFailed = true; }
            try { document.Editor.WriteMessage("\nQS3D " + status); } catch { uiSyncFailed = true; }
            if (uiSyncFailed)
                TryWriteMessage(document, "\nQS3D Sync Source UI sync warning: reconcile đã commit; một phần UI không thể đồng bộ.");
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
