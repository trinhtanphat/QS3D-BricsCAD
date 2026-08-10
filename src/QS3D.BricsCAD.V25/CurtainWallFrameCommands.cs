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
                var line = CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(document, project);
                var path = CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines(document, project);
                var elements = checked(line.Elements + path.Elements);
                var frames = checked(line.Frames + path.Frames);
                var stamped = elements > 0 ? CurtainWallFrameLiveStateService.StampSelected(document, project) : 0;
                PaletteCoordinator.RefreshProject();
                var message = frames == 0
                    ? "Curtain Frames 3D: chọn GlassWall semantic LINE hoặc open/bulged POLYLINE WCS-XY."
                    : "Curtain Frames 3D: đã tạo/cập nhật " + frames + " frame solid trên " + elements + " vách kính • live fingerprint " + stamped + ".";
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
