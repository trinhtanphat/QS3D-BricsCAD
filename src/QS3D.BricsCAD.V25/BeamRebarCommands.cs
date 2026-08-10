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
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var count = BeamRebarSolidBuilder.BuildSelected(document, project);
                PaletteCoordinator.RefreshProject();
                var message = count == 0
                    ? "Cốt thép 3D Dầm: chọn LINE đã capture thành Beam và khai báo RebarNotation; top/bottom có thể đặt bằng RebarBeamTopCount/RebarBeamBottomCount."
                    : "Cốt thép 3D Dầm: đã tạo/cập nhật " + count + " thanh dọc.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DBEAMREBAR3D lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
