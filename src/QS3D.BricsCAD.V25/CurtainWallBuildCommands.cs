using System;
using System.Collections.Generic;
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
            var linePanels = new CurtainPanelBuildResult();
            var pathPanels = new CurtainPanelBuildResult();
            ProjectState? project = null;
            ProjectStateSnapshot? rollback = null;
            CurtainWallBuildSelection? validatedSelection = null;
            CurtainWallUndoCoordinator.PendingTransition? undoTransition = null;
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
                phase = "canonical source prevalidation";
                validatedSelection = CurtainWallBuildSelectionGuard.Validate(document, project);
                rollback = ProjectStateSnapshot.Capture(project);

                // Capture only the selected GlassWall generated-owner surface. This is intentionally
                // narrower than the command rollback snapshot so a later native Undo cannot erase
                // unrelated semantic edits made elsewhere in the project.
                var undoBefore = CurtainWallUndoCoordinator.OwnerStateSnapshot.CaptureSelectedOwners(
                    document,
                    project,
                    validatedSelection.AllSourceIds);
                if (undoBefore.Count > 0)
                    undoTransition = CurtainWallUndoCoordinator.BeginTransition(document, project, undoBefore);

                // Resolve rule/dependency failures before native mutation. The command snapshot restores
                // this semantic phase as well when any later host/frame phase fails before outer commit.
                regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                CurtainWallBuildFailureInjection.ThrowIfArmed(CurtainWallBuildFailureInjection.SemanticRegeneration);

                var hostSolids = 0;
                var frameElements = 0;
                var frameSolids = 0;
                var panelElements = 0;
                var panelSolids = 0;
                // Builder transactions remain canonical/nested. The outer transaction is the command-level
                // native commit boundary, so aborting it rolls back every earlier host/frame/panel phase together.
                using (var commandTransaction = document.Database.TransactionManager.StartTransaction())
                {
                    phase = "LINE host replacement";
                    if (validatedSelection.LineSourceIds.Count > 0)
                    {
                        ApplySelection(document, validatedSelection.LineSourceIds);
                        lineHostSolids = WallSolidBuilder.BuildSelectedLineWalls(
                            document,
                            project,
                            ElementCategory.GlassWall,
                            allowPostCommitUi: false);
                    }
                    CurtainWallBuildFailureInjection.ThrowIfArmed(CurtainWallBuildFailureInjection.LineHost);

                    phase = "open-POLYLINE host replacement";
                    if (validatedSelection.PathSourceIds.Count > 0)
                    {
                        ApplySelection(document, validatedSelection.PathSourceIds);
                        pathHostSolids = PolylineWallSolidBuilder.BuildSelected(
                            document,
                            project,
                            ElementCategory.GlassWall,
                            allowPostCommitUi: false);
                    }
                    CurtainWallBuildFailureInjection.ThrowIfArmed(CurtainWallBuildFailureInjection.PathHost);

                    phase = "LINE frame replacement";
                    if (validatedSelection.LineSourceIds.Count > 0)
                    {
                        ApplySelection(document, validatedSelection.LineSourceIds);
                        lineFrames = CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(
                            document,
                            project,
                            allowInteractiveSelection: false);
                    }
                    CurtainWallBuildFailureInjection.ThrowIfArmed(CurtainWallBuildFailureInjection.LineFrame);

                    phase = "open/bulged path frame replacement";
                    if (validatedSelection.PathSourceIds.Count > 0)
                    {
                        ApplySelection(document, validatedSelection.PathSourceIds);
                        pathFrames = CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines(
                            document,
                            project,
                            allowInteractiveSelection: false);
                    }
                    CurtainWallBuildFailureInjection.ThrowIfArmed(CurtainWallBuildFailureInjection.PathFrame);

                    phase = "LINE panel replacement";
                    if (validatedSelection.LineSourceIds.Count > 0)
                    {
                        ApplySelection(document, validatedSelection.LineSourceIds);
                        linePanels = CurtainWallPanelSolidBuilder.BuildSelectedLineWalls(document, project);
                    }
                    CurtainWallBuildFailureInjection.ThrowIfArmed(CurtainWallBuildFailureInjection.LinePanel);

                    phase = "open/bulged path panel replacement";
                    if (validatedSelection.PathSourceIds.Count > 0)
                    {
                        ApplySelection(document, validatedSelection.PathSourceIds);
                        pathPanels = CurtainWallPathPanelSolidBuilder.BuildSelectedOpenPolylines(document, project);
                    }
                    CurtainWallBuildFailureInjection.ThrowIfArmed(CurtainWallBuildFailureInjection.PathPanel);

                    ApplySelection(document, validatedSelection.AllSourceIds);

                    hostSolids = checked(lineHostSolids + pathHostSolids);
                    frameElements = checked(lineFrames.Elements + pathFrames.Elements);
                    frameSolids = checked(lineFrames.Frames + pathFrames.Frames);
                    panelElements = checked(linePanels.Elements + pathPanels.Elements);
                    panelSolids = checked(linePanels.Panels + pathPanels.Panels);

                    if (undoTransition != null)
                    {
                        phase = "native Undo registration";
                        var undoAfter = CurtainWallUndoCoordinator.OwnerStateSnapshot.Capture(project, undoBefore.OwnerIds);
                        // Stage the revision marker in this same outer transaction. Native Undo therefore
                        // moves CAD geometry and the marker together; semantic state follows on CommandEnded.
                        undoTransition.StageAfter(project, commandTransaction, undoAfter);
                    }

                    commandTransaction.Commit();
                    nativeCommitted = true;
                    undoTransition?.ConfirmCommitted();
                }

                phase = "live fingerprint stamp";
                CurtainWallPostCommitFailureInjection.ThrowIfArmed(CurtainWallPostCommitFailureInjection.LiveFingerprint);
                var stampWarning = string.Empty;
                var stamped = frameElements > 0 ? CurtainWallFrameLiveStateService.TryStampSelected(document, project, out stampWarning) : 0;
                var panelStampWarning = string.Empty;
                var panelsStamped = panelElements > 0 ? CurtainWallPanelLiveStateService.TryStampSelected(document, project, out panelStampWarning) : 0;
                if (!string.IsNullOrWhiteSpace(panelStampWarning))
                    stampWarning = string.IsNullOrWhiteSpace(stampWarning) ? panelStampWarning : stampWarning + " | " + panelStampWarning;
                if (undoTransition != null)
                {
                    phase = "semantic Undo post-commit state";
                    var committedAfter = CurtainWallUndoCoordinator.OwnerStateSnapshot.Capture(project, undoBefore.OwnerIds);
                    undoTransition.RefreshCommittedAfter(project, committedAfter);
                }
                if (hostSolids == 0 && frameSolids == 0 && panelSolids == 0)
                {
                    Report(document, "Curtain 3D: chọn GlassWall semantic LINE hoặc open/bulged POLYLINE WCS-XY.");
                    return;
                }

                FinalizeUi(document, hostSolids, frameSolids, panelSolids, checked(stamped + panelsStamped), regenerated, stampWarning);
            }
            catch (Exception ex)
            {
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
            finally
            {
                undoTransition?.Dispose();
                TryRestoreSelection(document, validatedSelection);
            }
        }

        private static void ApplySelection(Document document, IReadOnlyList<Teigha.DatabaseServices.ObjectId> sourceIds)
        {
            var ids = new Teigha.DatabaseServices.ObjectId[sourceIds.Count];
            for (var index = 0; index < sourceIds.Count; index++) ids[index] = sourceIds[index];
            document.Editor.SetImpliedSelection(ids);
        }

        private static void TryRestoreSelection(Document document, CurtainWallBuildSelection? selection)
        {
            if (selection == null) return;
            try { ApplySelection(document, selection.AllSourceIds); }
            catch { }
        }

        private static void ReportAtomicFailure(Document document, string phase, bool nativeCommitted, Exception error)
        {
            if (!nativeCommitted)
            {
                Report(document, "QS3DCURTAIN3D lỗi tại " + phase + ": " + error.Message +
                    ". ATOMIC ROLLBACK đã hoàn tác toàn bộ host/frame/panel CAD và semantic state; không có phase Curtain 3D nào được commit.");
                return;
            }

            Report(document, "QS3DCURTAIN3D post-commit warning tại " + phase + ": " + error.Message +
                ". Native host/frame/panel transaction đã commit; chạy QS3DCURTAINFRAMEHEALTH/QS3DHEALTHALL trước khi phát hành.");
        }

        private static void FinalizeUi(Document document, int hostSolids, int frameSolids, int panelSolids, int stamped, int regenerated, string stampWarning)
        {
            var status = "Curtain 3D: " + hostSolids + " host solid • " + frameSolids + " frame solid • live fingerprint " + stamped + " • regenerate " + regenerated;
            status += " • " + panelSolids + " panel solid";
            if (!string.IsNullOrWhiteSpace(stampWarning)) status += " • fingerprint pending";
            status += ".";
            try
            {
                CurtainWallPostCommitFailureInjection.ThrowIfArmed(CurtainWallPostCommitFailureInjection.UiRefresh);
                PaletteCoordinator.RefreshProject();
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
                if (!string.IsNullOrWhiteSpace(stampWarning))
                    document.Editor.WriteMessage("\nQS3D warning: " + stampWarning);
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
