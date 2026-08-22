using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RebarGeometryCommands
    {
        [CommandMethod("QS3DREBAR3D", CommandFlags.UsePickSet)]
        public void BuildRebar3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var count = ColumnRebarSolidBuilder.BuildSelected(document, project);
                PaletteCoordinator.RefreshProject();
                var message = count == 0
                    ? "Rebar 3D: chọn Column semantic có closed rectangle POLYLINE + RebarNotation."
                    : "Rebar 3D: đã tạo/cập nhật " + count + " thanh đứng cho cột được chọn.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception ex)
            {
                var message = "QS3DREBAR3D lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
