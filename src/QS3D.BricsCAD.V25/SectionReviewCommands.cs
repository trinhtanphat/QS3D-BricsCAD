using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SectionReviewCommands
    {
        private const string BimDetailCommand = "_BIMSECTION _Detail ";
        private const string SectionPlaneCommand = "_SECTIONPLANE ";
        private const string ClipDisplayCommand = "_CLIPDISPLAY ";

        [CommandMethod("QS3DSECTIONBOX", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void SectionBox()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DSECTIONBOX", () =>
            {
                var highlighted = ModelReviewService.HighlightSelection(document, false);
                var message = highlighted > 0
                    ? "Section Box: giữ highlight " + highlighted + " đối tượng tham chiếu; chọn 2 góc đáy và chiều cao cho BIM Detail section."
                    : "Section Box: chọn 2 góc đáy và chiều cao cho BIM Detail section.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message + " Command này dùng native BIMSECTION/Detail và cần BricsCAD BIM hỗ trợ lệnh BIMSECTION.");
                document.SendStringToExecute(BimDetailCommand, true, false, true);
            });
        }

        [CommandMethod("QS3DSECTIONPLANE", CommandFlags.Modal)]
        public void SectionPlane()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DSECTIONPLANE", () =>
            {
                PaletteCoordinator.SetStatus("Section Plane: đang chuyển sang native SECTIONPLANE để đặt mặt cắt tương tác.");
                document.Editor.WriteMessage("\nQS3D Section Plane: dùng native SECTIONPLANE. Chọn phương thức/điểm theo command bar của BricsCAD.");
                document.SendStringToExecute(SectionPlaneCommand, true, false, true);
            });
        }

        [CommandMethod("QS3DCLIPDISPLAY", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ClipDisplay()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DCLIPDISPLAY", () =>
            {
                PaletteCoordinator.SetStatus("Clip Display: chọn BIM/Section entity cần bật/tắt vùng cắt.");
                document.Editor.WriteMessage("\nQS3D Clip Display: dùng native CLIPDISPLAY trên section entity được chọn/prompt.");
                document.SendStringToExecute(ClipDisplayCommand, true, false, true);
            });
        }

        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;

        private static void Guard(Document document, string operation, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                var message = operation + " lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
