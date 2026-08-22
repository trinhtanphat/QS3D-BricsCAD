using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SlabMeshCommands
    {
        [CommandMethod("QS3DSLABREBAR3D", CommandFlags.UsePickSet)]
        public void BuildSlabMesh3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var result = SlabMeshSolidBuilder.BuildSelected(document, project);
                PaletteCoordinator.RefreshProject();
                var message = result.Bars == 0
                    ? "Slab Mesh 3D: chọn Slab semantic có closed rectangular POLYLINE + RebarSlabXNotation/RebarSlabYNotation."
                    : "Slab Mesh 3D: đã tạo/cập nhật " + result.Bars + " thanh trên " + result.Elements + " sàn.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception ex)
            {
                var message = "QS3DSLABREBAR3D lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
