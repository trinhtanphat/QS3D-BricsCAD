#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COORD = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurtainWallUndoCoordinator.cs"
BUILD = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurtainWallBuildCommands.cs"
LIFECYCLE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DocumentLifecycleCoordinator.cs"
SOURCE_UNDO = ROOT / "src" / "QS3D.BricsCAD.V25" / "SourceReconcileUndoCoordinator.cs"
errors = []

for path in (COORD, BUILD, LIFECYCLE, SOURCE_UNDO):
    if not path.is_file():
        errors.append("missing Curtain Undo dependency: " + str(path.relative_to(ROOT)))

coord = COORD.read_text(encoding="utf-8") if COORD.is_file() else ""
build = BUILD.read_text(encoding="utf-8") if BUILD.is_file() else ""
lifecycle = LIFECYCLE.read_text(encoding="utf-8") if LIFECYCLE.is_file() else ""
source_undo = SOURCE_UNDO.read_text(encoding="utf-8") if SOURCE_UNDO.is_file() else ""

for token in (
    'private const string RegAppName = "QS3D_CURTAIN_UNDO";',
    'private const string RevisionPrefix = "CWU1:";',
    "OwnerStateSnapshot CaptureSelectedOwners(",
    'key.StartsWith("GeneratedSolid", StringComparison.OrdinalIgnoreCase)',
    'key.StartsWith("GeneratedCurtainFrame", StringComparison.OrdinalIgnoreCase)',
    'key.StartsWith("GeneratedCurtainPanel", StringComparison.OrdinalIgnoreCase)',
    "ProjectElement.GeneratedGeometryStateKey",
    "ProjectElement.GeneratedGeometryStaleReasonKey",
    '!string.Equals(key, FrameLiveFingerprintKey, StringComparison.OrdinalIgnoreCase)',
    '!string.Equals(key, PanelLiveFingerprintKey, StringComparison.OrdinalIgnoreCase)',
    "ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, \"Curtain Undo registration\")",
    "ProjectContextCoordinator.RequireBackingStoreUnchanged(_document, project, \"Curtain Undo staging\")",
    "ProjectContextCoordinator.TryGetCached(document, out var project)",
    "ReferenceEquals(project, history.Project)",
    "history.Desynchronized = true;",
    'string.Equals(normalized, "UNDO", StringComparison.OrdinalIgnoreCase)',
    'string.Equals(normalized, "REDO", StringComparison.OrdinalIgnoreCase)',
    'string.Equals(normalized, "MREDO", StringComparison.OrdinalIgnoreCase)',
    "var currentFull = OwnerStateSnapshot.Capture(project, currentExpected.OwnerIds);",
    "if (undo) transition.After = currentFull;",
    "else transition.Before = currentFull;",
    "var restoreRollback = OwnerStateSnapshot.Capture(project, target.OwnerIds);",
    "target.Restore(project);",
    "restoreRollback.Restore(project);",
    "modelSpace.XData = marker;",
):
    if token not in coord:
        errors.append("Curtain Undo coordinator contract missing: " + token)

if 'private const string RegAppName = "QS3D_SRC_SYNC_UNDO";' not in source_undo:
    errors.append("Source Reconcile Undo marker precedent unexpectedly changed")
if 'QS3D_SRC_SYNC_UNDO' in coord:
    errors.append("Curtain Undo must own a distinct native revision marker namespace")
if "ProjectStateSnapshot" in coord:
    errors.append("Curtain Undo must not restore whole-project snapshots during native Undo/Redo")
if "SourceReconcileUndoCoordinator" in coord:
    errors.append("Curtain Undo must remain independent from Source Reconcile behavior")

for token in (
    "CurtainWallUndoCoordinator.OwnerStateSnapshot.CaptureSelectedOwners(",
    "CurtainWallUndoCoordinator.BeginTransition(document, project, undoBefore)",
    "CurtainWallUndoCoordinator.OwnerStateSnapshot.Capture(project, undoBefore.OwnerIds)",
    "undoTransition.StageAfter(project, commandTransaction, undoAfter);",
    "commandTransaction.Commit();",
    "nativeCommitted = true;",
    "undoTransition?.ConfirmCommitted();",
    "CurtainWallPostCommitFailureInjection.ThrowIfArmed(CurtainWallPostCommitFailureInjection.LiveFingerprint);",
    "CurtainWallFrameLiveStateService.TryStampSelected",
    "CurtainWallPanelLiveStateService.TryStampSelected",
    "if (!nativeCommitted && rollback != null && project != null)",
    "rollback.Restore(project);",
):
    if token not in build:
        errors.append("QS3DCURTAIN3D Undo integration contract missing: " + token)

capture = build.find("CurtainWallUndoCoordinator.OwnerStateSnapshot.CaptureSelectedOwners(")
begin = build.find("CurtainWallUndoCoordinator.BeginTransition(document, project, undoBefore)")
regen = build.find("RegenerateDirty(project)")
line_host = build.find("WallSolidBuilder.BuildSelectedLineWalls")
line_frame = build.find("CurtainWallFrameSolidBuilder.BuildSelectedLineWalls")
line_panel = build.find("CurtainWallPanelSolidBuilder.BuildSelectedLineWalls")
stage = build.find("undoTransition.StageAfter(project, commandTransaction, undoAfter);")
commit = build.find("commandTransaction.Commit();")
post = build.find("CurtainWallPostCommitFailureInjection.ThrowIfArmed(CurtainWallPostCommitFailureInjection.LiveFingerprint);")
if min(capture, begin, regen, line_host, line_frame, line_panel, stage, commit, post) < 0:
    errors.append("cannot establish QS3DCURTAIN3D Undo ordering")
elif not (capture < begin < regen < line_host < line_frame < line_panel < stage < commit < post):
    errors.append("Curtain Undo must capture/register before mutation, stage after all builders, commit marker with CAD, and keep fingerprint post-commit")

for token in (
    "CurtainWallUndoCoordinator.Attach(docs.MdiActiveDocument);",
    "CurtainWallUndoCoordinator.Stop();",
    "CurtainWallUndoCoordinator.Attach(e.Document);",
    "CurtainWallUndoCoordinator.Detach(document);",
    "CurtainWallUndoCoordinator.Attach(active);",
):
    if token not in lifecycle:
        errors.append("document lifecycle missing Curtain Undo affinity hook: " + token)

if lifecycle.count("CurtainWallUndoCoordinator.Attach(e.Document);") < 2:
    errors.append("Curtain Undo must attach on both DocumentCreated and DocumentActivated")
if "ProjectContextCoordinator.Forget(document);" not in lifecycle:
    errors.append("document destruction must still forget canonical project state")

for forbidden in (
    "CurtainWallFrameSolidBuilder.cs",
    "CurtainWallPanelSolidBuilder.cs",
    "GeneratedCurtainFrameNativeOwnershipService",
    "GeneratedCurtainPanelNativeOwnershipService",
):
    if forbidden in coord:
        errors.append("Curtain Undo coordinator must not own geometry/ownership builder implementation: " + forbidden)

if errors:
    print("Curtain semantic/native Undo coherence preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: QS3DCURTAIN3D stages a dedicated native revision marker in its outer transaction, "
    "tracks only selected GlassWall generated-owner metadata, synchronizes native Undo/Redo with "
    "document/backing-store fail-closed guards, and preserves post-commit fingerprint warning semantics."
)
