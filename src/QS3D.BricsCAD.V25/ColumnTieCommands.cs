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
            var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try
            {
                // Capture PICKFIRST once before binding the canonical project. The same
                // snapshot is passed into native generation, so admission and mutation
                // cannot observe two different implied-selection sets.
                var selectedIds = CadSelectionGuard.ReadImpliedSelection(document);
                if (selectedIds.Length == 0)
                {
                    Report(document, nativeDatabaseIdentity, SelectionGuidance);
                    return;
                }

                var project = ExistingProjectMutationContext.Require(document, "Column Tie 3D");
                RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);
                var count = ColumnTieSolidBuilder.BuildSelected(document, project, selectedIds);
                var message = count == 0
                    ? SelectionGuidance
                    : "Tie 3D: đã tạo/cập nhật " + count + " đai cột.";
                FinalizeUi(document, nativeDatabaseIdentity, message);
            }
            catch (Exception)
            {
                Report(document, nativeDatabaseIdentity, OperationFailure);
            }
        }

        private static void FinalizeUi(Document document, IntPtr nativeDatabaseIdentity, string message)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try
            {
                RefreshModelTree(document, nativeDatabaseIdentity);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                document.Editor.Regen();
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                TrySetPaletteStatus(document, nativeDatabaseIdentity, message);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception ex)
            {
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                TryWriteMessage(document, nativeDatabaseIdentity, "\nQS3D " + message + " " + UiSyncWarning + " (" + ex.GetType().Name + ").");
            }
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            try
            {
                return document.Database.UnmanagedObject;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (nativeDatabaseIdentity == IntPtr.Zero ||
                !ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                return false;

            try
            {
                return document.Database.UnmanagedObject == nativeDatabaseIdentity;
            }
            catch
            {
                return false;
            }
        }

        private static void RequireActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                throw new InvalidOperationException("Column Tie 3D document generation changed before geometry mutation.");
        }

        private static void RefreshModelTree(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            PaletteCoordinator.RefreshProject();
        }

        private static void TrySetPaletteStatus(Document document, IntPtr nativeDatabaseIdentity, string message)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try { PaletteCoordinator.SetStatus(message); }
            catch { }
        }

        private static void Report(Document document, IntPtr nativeDatabaseIdentity, string message)
        {
            TrySetPaletteStatus(document, nativeDatabaseIdentity, message);
            TryWriteMessage(document, nativeDatabaseIdentity, "\nQS3D " + message);
        }

        private static void TryWriteMessage(Document document, IntPtr nativeDatabaseIdentity, string message)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try { document.Editor.WriteMessage(message); }
            catch { }
        }
    }
}
