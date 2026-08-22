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
                var message = result.Bars == 0
                    ? "Foundation Rebar 3D: chọn Foundation semantic có closed straight plan-view POLYLINE + RebarFoundationXNotation/RebarFoundationYNotation. Rectangle giữ local X/Y; polygon dùng drawing X/Y."
                    : "Foundation Rebar 3D: đã tạo/cập nhật " + result.Bars + " thanh cho " + result.Elements + " móng.";
                FinalizeUi(document, message);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DFOUNDATIONREBAR3D lỗi: " + ex.Message);
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
