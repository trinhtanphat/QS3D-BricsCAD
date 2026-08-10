using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CurtainWallBuildCommands
    {
        [CommandMethod("QS3DCURTAIN3D", CommandFlags.UsePickSet)]
        public void BuildCurtain3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var hostSolids = WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.GlassWall);
                hostSolids += PolylineWallSolidBuilder.BuildSelected(document, project, ElementCategory.GlassWall);
                var frames = CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(document, project);
                if (hostSolids == 0 && frames.Frames == 0)
                {
                    var message = "Curtain 3D: chọn GlassWall semantic LINE hoặc open POLYLINE. Frame overlay hiện chỉ hỗ trợ LINE nằm ngang.";
                    PaletteCoordinator.SetStatus(message);
                    document.Editor.WriteMessage("\nQS3D " + message);
                    return;
                }
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                PaletteCoordinator.RefreshProject();
                var status = "Curtain 3D: " + hostSolids + " host solid • " + frames.Frames + " frame solid • regenerate " + regenerated + ".";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
                document.SendStringToExecute("QS3DVIEW3D ", true, false, false);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DCURTAIN3D lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
