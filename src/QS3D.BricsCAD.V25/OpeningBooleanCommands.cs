using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class OpeningBooleanCommands
    {
        [CommandMethod("QS3DCUTOPENINGS", CommandFlags.Modal)]
        public void CutOpenings()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var count = OpeningBooleanService.CutLinkedOpenings(document, project);
                PaletteCoordinator.RefreshProject();
                var message = count == 0
                    ? "Physical opening: không có linked opening mới cần khoét hoặc host chưa có generated LINE solid tương thích."
                    : "Physical opening: đã khoét " + count + " Cửa/Lỗ Mở vào generated host solid.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception ex)
            {
                var message = "QS3DCUTOPENINGS lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
