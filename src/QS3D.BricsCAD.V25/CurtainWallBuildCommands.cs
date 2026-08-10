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

            var phase = "semantic regeneration";
            var regenerated = 0;
            var lineHostSolids = 0;
            var pathHostSolids = 0;
            var lineFrames = new CurtainFrameBuildResult();
            var pathFrames = new CurtainFrameBuildResult();
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);

                // Resolve rule/dependency failures before any host/frame builder commits native CAD.
                // Native host and detail builders intentionally remain separate transaction families,
                // so semantic blockers must never be discovered only after those transactions succeed.
                regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);

                phase = "LINE host replacement";
                lineHostSolids = WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.GlassWall);

                phase = "open-POLYLINE host replacement";
                pathHostSolids = PolylineWallSolidBuilder.BuildSelected(document, project, ElementCategory.GlassWall);

                phase = "LINE frame replacement";
                lineFrames = CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(document, project);

                phase = "open/bulged path frame replacement";
                pathFrames = CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines(document, project);

                var hostSolids = checked(lineHostSolids + pathHostSolids);
                var frameElements = checked(lineFrames.Elements + pathFrames.Elements);
                var frameSolids = checked(lineFrames.Frames + pathFrames.Frames);
                phase = "live fingerprint stamp";
                var stampWarning = string.Empty;
                var stamped = frameElements > 0 ? CurtainWallFrameLiveStateService.TryStampSelected(document, project, out stampWarning) : 0;
                if (hostSolids == 0 && frameSolids == 0)
                {
                    Report(document, "Curtain 3D: chọn GlassWall semantic LINE hoặc open/bulged POLYLINE WCS-XY.");
                    return;
                }

                FinalizeUi(document, hostSolids, frameSolids, stamped, regenerated, stampWarning);
            }
            catch (Exception ex)
            {
                ReportPhaseFailure(document, phase, lineHostSolids, pathHostSolids, lineFrames, pathFrames, ex);
            }
        }

        private static void ReportPhaseFailure(
            Document document,
            string phase,
            int lineHostSolids,
            int pathHostSolids,
            CurtainFrameBuildResult lineFrames,
            CurtainFrameBuildResult pathFrames,
            Exception error)
        {
            var committedHosts = checked(lineHostSolids + pathHostSolids);
            var committedFrames = checked((lineFrames?.Frames ?? 0) + (pathFrames?.Frames ?? 0));
            if (committedHosts == 0 && committedFrames == 0)
            {
                Report(document, "QS3DCURTAIN3D lỗi tại " + phase + ": " + error.Message);
                return;
            }

            var status = "Curtain 3D PARTIAL COMMIT: host LINE=" + lineHostSolids +
                " • host path=" + pathHostSolids +
                " • frame LINE=" + (lineFrames?.Frames ?? 0) +
                " • frame path=" + (pathFrames?.Frames ?? 0) +
                " • lỗi tại " + phase + ": " + error.Message +
                ". Các phase trước đã commit bằng transaction riêng và không bị giả vờ rollback. Chạy QS3DCURTAINFRAMEHEALTH/QS3DHEALTHALL, sửa lỗi rồi rebuild host hoặc chạy QS3DCURTAINFRAMES3D theo kết quả health.";
            Report(document, status);
        }

        private static void FinalizeUi(Document document, int hostSolids, int frameSolids, int stamped, int regenerated, string stampWarning)
        {
            var status = "Curtain 3D: " + hostSolids + " host solid • " + frameSolids + " frame solid • live fingerprint " + stamped + " • regenerate " + regenerated;
            if (!string.IsNullOrWhiteSpace(stampWarning)) status += " • fingerprint pending";
            status += ".";
            try
            {
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
                if (!string.IsNullOrWhiteSpace(stampWarning))
                    document.Editor.WriteMessage("\nQS3D warning: " + stampWarning);
                document.SendStringToExecute("QS3DVIEW3D ", true, false, false);
            }
            catch (Exception ex)
            {
                TryWriteMessage(document, "\nQS3D " + status + " UI sync warning: " + ex.Message);
                if (!string.IsNullOrWhiteSpace(stampWarning)) TryWriteMessage(document, "\nQS3D warning: " + stampWarning);
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
