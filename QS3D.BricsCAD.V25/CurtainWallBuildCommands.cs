using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
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
            ProjectState project = null;
            ProjectStateSnapshot rollback = null;
            var nativeCommitted = false;
            try
            {
                var selected = EntitySnapshotReader.ReadCurrentSelection(document);
                if (selected.Count == 0)
                {
                    Report(document, "Curtain 3D: chọn GlassWall semantic LINE hoặc open/bulged POLYLINE WCS-XY.");
                    return;
                }

                project = ExistingProjectMutationContext.Require(document, "Curtain 3D");
                rollback = ProjectStateSnapshot.Capture(project);

                // Resolve rule/dependency failures before native mutation. The command snapshot restores
                // this semantic phase as well when any later host/frame phase fails before outer commit.
                regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);

                var hostSolids = 0;
                var frameElements = 0;
                var frameSolids = 0;
                // Builder transactions remain canonical/nested. The outer transaction is the command-level
                // native commit boundary, so aborting it rolls back every earlier host/frame phase together.
                using (var commandTransaction = document.Database.TransactionManager.StartTransaction())
                {
                    phase = "LINE host replacement";
                    lineHostSolids = WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.GlassWall);

                    phase = "open-POLYLINE host replacement";
                    pathHostSolids = PolylineWallSolidBuilder.BuildSelected(document, project, ElementCategory.GlassWall);

                    phase = "LINE frame replacement";
                    lineFrames = CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(document, project);

                    phase = "open/bulged path frame replacement";
                    pathFrames = CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines(document, project);

                    hostSolids = checked(lineHostSolids + pathHostSolids);
                    frameElements = checked(lineFrames.Elements + pathFrames.Elements);
                    frameSolids = checked(lineFrames.Frames + pathFrames.Frames);
                    commandTransaction.Commit();
                    nativeCommitted = true;
                }

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
<<<<<<< HEAD
                ReportPhaseFailure(document, phase, regenerated, lineHostSolids, pathHostSolids, lineFrames, pathFrames, ex);
            }
        }

        private static void ReportPhaseFailure(
            Document document,
            string phase,
            int regenerated,
            int lineHostSolids,
            int pathHostSolids,
            CurtainFrameBuildResult lineFrames,
            CurtainFrameBuildResult pathFrames,
            Exception error)
        {
            var committedHosts = checked(lineHostSolids + pathHostSolids);
            var committedFrames = checked((lineFrames?.Frames ?? 0) + (pathFrames?.Frames ?? 0));
            if (regenerated == 0 && committedHosts == 0 && committedFrames == 0)
=======
                if (!nativeCommitted && rollback != null && project != null)
                {
                    try { rollback.Restore(project); }
                    catch (Exception restoreError)
                    {
                        Report(document, "QS3DCURTAIN3D lỗi tại " + phase + " và semantic rollback thất bại: " +
                            ex.Message + " • rollback: " + restoreError.Message);
                        return;
                    }
                    TryRegen(document);
                }
                ReportAtomicFailure(document, phase, nativeCommitted, ex);
            }
        }

        private static void ReportAtomicFailure(Document document, string phase, bool nativeCommitted, Exception error)
        {
            if (!nativeCommitted)
>>>>>>> origin/main
            {
                Report(document, "QS3DCURTAIN3D lỗi tại " + phase + ": " + error.Message +
                    ". ATOMIC ROLLBACK đã hoàn tác toàn bộ host/frame CAD và semantic state; không có phase Curtain 3D nào được commit.");
                return;
            }

<<<<<<< HEAD
            var status = "Curtain 3D PARTIAL COMMIT: semantic regenerate=" + regenerated +
                " • host LINE=" + lineHostSolids +
                " • host path=" + pathHostSolids +
                " • frame LINE=" + (lineFrames?.Frames ?? 0) +
                " • frame path=" + (pathFrames?.Frames ?? 0) +
                " • lỗi tại " + phase + ": " + error.Message +
                ". Các phase trước đã commit bằng transaction riêng và không bị giả vờ rollback. Chạy QS3DCURTAINFRAMEHEALTH/QS3DHEALTHALL, sửa lỗi rồi rebuild host hoặc chạy QS3DCURTAINFRAMES3D theo kết quả health.";
            Report(document, status);
=======
            Report(document, "QS3DCURTAIN3D post-commit warning tại " + phase + ": " + error.Message +
                ". Native host/frame transaction đã commit; chạy QS3DCURTAINFRAMEHEALTH/QS3DHEALTHALL trước khi phát hành.");
>>>>>>> origin/main
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

        private static void TryRegen(Document document)
        {
            try { document.Editor.Regen(); }
            catch { }
        }
    }
}
