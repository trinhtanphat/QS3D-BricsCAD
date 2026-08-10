using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class StirrupRebarGeometryCommands
    {
        [CommandMethod("QS3DREBAR3DSTIRRUP", CommandFlags.UsePickSet)]
        public void BuildStirrupRebar3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var result = StirrupRebarSolidBuilder.BuildSelected(document, project);
                PaletteCoordinator.RefreshProject();
                var message = result.Ties == 0
                    ? "Stirrup 3D: chọn Beam LINE hoặc Column rectangle POLYLINE có StirrupNotation và tham số stirrup explicit."
                    : "Stirrup 3D: đã tạo/cập nhật " + result.Ties + " đai cho " + result.Elements + " element.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception ex)
            {
                var message = "QS3DREBAR3DSTIRRUP lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
