using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RebarMatCommands
    {
        [CommandMethod("QS3DREBARMAT3D", CommandFlags.UsePickSet)]
        public void BuildRebarMat3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var result = RebarMatSolidBuilder.BuildSelected(document, project);
                PaletteCoordinator.RefreshProject();
                var message = result.Bars == 0
                    ? "Rebar Mat 3D: chọn Slab/Foundation rectangle đã capture. Dùng RebarMatXNotation/RebarMatYNotation dạng D12@200 và RebarMatFaces=Bottom|Top|Both."
                    : "Rebar Mat 3D: đã tạo/cập nhật " + result.Bars + " thanh cho " + result.Elements + " cấu kiện.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception ex)
            {
                var message = "QS3DREBARMAT3D lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
