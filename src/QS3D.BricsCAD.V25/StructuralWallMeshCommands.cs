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
                var selectedIds = CadSelectionGuard.AcquireCurrentSelection(document);
                if (selectedIds.Length == 0)
                {
                    Report(document, "Wall Mesh 3D: chọn StructuralWall semantic LINE có RebarWallHorizontalNotation/RebarWallVerticalNotation.");
                    return;
                }

                var project = ExistingProjectMutationContext.Require(document, "Wall Mesh 3D");
                var result = StructuralWallMeshSolidBuilder.BuildSelected(document, project);
                var message = result.Bars == 0
                    ? "Wall Mesh 3D: chọn StructuralWall semantic LINE có RebarWallHorizontalNotation/RebarWallVerticalNotation."
                    : "Wall Mesh 3D: đã tạo/cập nhật " + result.Bars + " thanh trên " + result.Elements + " vách.";
                FinalizeUi(document, message);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DWALLREBAR3D lỗi: " + ex.Message);
            }
        }

        private static void FinalizeUi(Document document, string message)
        {
            try
            {
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
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
