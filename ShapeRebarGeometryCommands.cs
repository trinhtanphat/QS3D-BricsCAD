using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
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
                var message = result.Bars == 0
                    ? "Shape Rebar 3D: chọn cấu kiện semantic có BBS/RebarNotation hợp lệ."
                    : "Shape Rebar 3D: đã tạo/cập nhật " + result.Bars + " thanh cho " + result.Elements + " cấu kiện.";
                FinalizeUi(document, result, message);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DREBAR3DSHAPE lỗi: " + ex.Message);
            }
        }

        private static void FinalizeUi(Document document, ShapeRebarBuildResult result, string message)
        {
            try
            {
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                if (result.Bars > 0) document.SendStringToExecute("QS3DVIEW3D ", true, false, false);
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
