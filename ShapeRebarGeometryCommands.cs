using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Audit;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ShapeRebarGeometryCommands
    {
        [CommandMethod("QS3DREBAR3DSHAPE", CommandFlags.UsePickSet)]
        public void BuildShapeRebar3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var result = ShapeRebarSolidBuilder.BuildSelected(document, project);
                if (result.Bars > 0) AuditTrail.ForProject(project).Record("geometry.rebar3d.shape", string.Empty, result.Bars + " bars • " + result.Elements + " elements");
                PaletteCoordinator.RefreshProject();
                var message = result.Bars == 0 ? "Shape Rebar 3D: chọn cấu kiện semantic có BBS/RebarNotation hợp lệ." : "Shape Rebar 3D: đã tạo/cập nhật " + result.Bars + " thanh cho " + result.Elements + " cấu kiện.";
                PaletteCoordinator.SetStatus(message); document.Editor.WriteMessage("\nQS3D " + message);
                if (result.Bars > 0) document.SendStringToExecute("QS3DVIEW3D ", true, false, false);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DREBAR3DSHAPE lỗi: " + ex.Message; PaletteCoordinator.SetStatus(message); document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
