using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class BeamRebarCommands
    {
        [CommandMethod("QS3DBEAMREBAR3D", CommandFlags.UsePickSet)]
        public void BuildBeamRebar3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var selectedIds = CadSelectionGuard.AcquireCurrentSelection(document);
                if (selectedIds.Length == 0)
                {
                    Report(document, "Cốt thép 3D Dầm: chọn LINE đã capture thành Beam và khai báo RebarNotation; top/bottom có thể đặt bằng RebarBeamTopCount/RebarBeamBottomCount.");
                    return;
                }

                var project = ExistingProjectMutationContext.Require(document, "Beam Rebar 3D");
                var count = BeamRebarSolidBuilder.BuildSelected(document, project);
                var message = count == 0
                    ? "Cốt thép 3D Dầm: chọn LINE đã capture thành Beam và khai báo RebarNotation; top/bottom có thể đặt bằng RebarBeamTopCount/RebarBeamBottomCount."
                    : "Cốt thép 3D Dầm: đã tạo/cập nhật " + count + " thanh dọc.";
                FinalizeUi(document, message);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DBEAMREBAR3D lỗi: " + ex.Message);
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
            catch (Exception ex)
            {
                TryWriteMessage(document, "\nQS3D " + message + " UI sync warning: " + ex.Message);
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
