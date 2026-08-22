using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ColumnTieCommands
    {
        [CommandMethod("QS3DREBARTIES3D", CommandFlags.UsePickSet)]
        public void BuildColumnTies()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var count = ColumnTieSolidBuilder.BuildSelected(document, project);
                PaletteCoordinator.RefreshProject();
                var message = count == 0
                    ? "Tie 3D: chọn Column semantic có closed rectangle POLYLINE; khai báo RebarTieDiameterMm/RebarTieSpacingMm nếu cần override."
                    : "Tie 3D: đã tạo/cập nhật " + count + " đai cột.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DREBARTIES3D lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
