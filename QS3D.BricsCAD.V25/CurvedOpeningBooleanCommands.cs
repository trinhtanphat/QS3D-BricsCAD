using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CurvedOpeningBooleanCommands
    {
        [CommandMethod("QS3DCUTOPENINGSCURVED", CommandFlags.Modal)]
        public void CutCurvedOpenings()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var count = CurvedOpeningBooleanService.CutLinkedOpenings(document, project);
                PaletteCoordinator.RefreshProject();
                var message = count == 0
                    ? "Curved Opening Cut: chưa có linked Opening/Door trên generated host open POLYLINE có bulge cần khoét."
                    : "Curved Opening Cut: đã khoét " + count + " Opening/Door trên host cong.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DCUTOPENINGSCURVED lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
