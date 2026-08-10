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

                // Resolve rule/dependency failures before any host/frame builder commits native CAD.
                // Native host and detail builders intentionally remain separate transaction families,
                // so semantic blockers must never be discovered only after those transactions succeed.
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);

                var hostSolids = WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.GlassWall);
                hostSolids += PolylineWallSolidBuilder.BuildSelected(document, project, ElementCategory.GlassWall);
                var lineFrames = CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(document, project);
                var pathFrames = CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines(document, project);
                var frameElements = checked(lineFrames.Elements + pathFrames.Elements);
                var frameSolids = checked(lineFrames.Frames + pathFrames.Frames);
                var stamped = frameElements > 0 ? CurtainWallFrameLiveStateService.StampSelected(document, project) : 0;
                if (hostSolids == 0 && frameSolids == 0)
                {
                    Report(document, "Curtain 3D: chọn GlassWall semantic LINE hoặc open/bulged POLYLINE WCS-XY.");
                    return;
                }

                FinalizeUi(document, hostSolids, frameSolids, stamped, regenerated);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DCURTAIN3D lỗi: " + ex.Message);
            }
        }

        private static void FinalizeUi(Document document, int hostSolids, int frameSolids, int stamped, int regenerated)
        {
            var status = "Curtain 3D: " + hostSolids + " host solid • " + frameSolids + " frame solid • live fingerprint " + stamped + " • regenerate " + regenerated + ".";
            try
            {
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
                document.SendStringToExecute("QS3DVIEW3D ", true, false, false);
            }
            catch (Exception ex)
            {
                TryWriteMessage(document, "\nQS3D " + status + " UI sync warning: " + ex.Message);
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
