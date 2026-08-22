using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class StructuralWallMeshCommands
    {
        [CommandMethod("QS3DWALLREBAR3D", CommandFlags.UsePickSet)]
        public void BuildStructuralWallMesh3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var result = StructuralWallMeshSolidBuilder.BuildSelected(document, project);
                PaletteCoordinator.RefreshProject();
                var message = result.Bars == 0
                    ? "Wall Mesh 3D: chọn StructuralWall semantic LINE có RebarWallHorizontalNotation/RebarWallVerticalNotation."
                    : "Wall Mesh 3D: đã tạo/cập nhật " + result.Bars + " thanh trên " + result.Elements + " vách.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception ex)
            {
                var message = "QS3DWALLREBAR3D lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
