using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class FoundationMeshCommands
    {
        [CommandMethod("QS3DFOUNDATIONREBAR3D", CommandFlags.UsePickSet)]
        public void BuildFoundationMesh3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var result = FoundationMeshSolidBuilder.BuildSelected(document, project);
                PaletteCoordinator.RefreshProject();
                var message = result.Bars == 0
                    ? "Foundation Rebar 3D: chọn Foundation semantic có closed rectangle POLYLINE + RebarFoundationXNotation/RebarFoundationYNotation."
                    : "Foundation Rebar 3D: đã tạo/cập nhật " + result.Bars + " thanh cho " + result.Elements + " móng.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DFOUNDATIONREBAR3D lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
