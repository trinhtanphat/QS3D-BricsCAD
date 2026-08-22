using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CurtainWallFrameCommands
    {
        [CommandMethod("QS3DCURTAINFRAMES3D", CommandFlags.UsePickSet)]
        public void BuildCurtainFrames3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var result = CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(document, project);
                PaletteCoordinator.RefreshProject();
                var message = result.Frames == 0
                    ? "Curtain Frames 3D: chọn GlassWall semantic LINE. Open/curved POLYLINE hiện giữ generic host và chưa dựng frame overlay."
                    : "Curtain Frames 3D: đã tạo/cập nhật " + result.Frames + " frame solid trên " + result.Elements + " vách kính.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DCURTAINFRAMES3D lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
