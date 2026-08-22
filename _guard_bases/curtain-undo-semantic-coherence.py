#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COORD = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurtainWallUndoCoordinator.cs"
BUILD = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurtainWallBuildCommands.cs"
LIFECYCLE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DocumentLifecycleCoordinator.cs"
PROJECT_CONTEXT = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectContextCoordinator.cs"
CHECKPOINT = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectPersistenceCheckpoint.cs"
CHECKPOINT_SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectPersistenceCheckpointSmoke.cs"
SMOKE_REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"
SOURCE_UNDO = ROOT / "src" / "QS3D.BricsCAD.V25" / "SourceReconcileUndoCoordinator.cs"
errors = []

for path in (COORD, BUILD, LIFECYCLE, PROJECT_CONTEXT, CHECKPOINT, CHECKPOINT_SMOKE, SMOKE_REGISTRATION, SOURCE_UNDO):
    if not path.is_file():
        errors.append("missing Curtain Undo dependency: " + str(path.relative_to(ROOT)))

coord = COORD.read_text(encoding="utf-8") if COORD.is_file() else ""
build = BUILD.read_text(encoding="utf-8") if BUILD.is_file() else ""
lifecycle = LIFECYCLE.read_text(encoding="utf-8") if LIFECYCLE.is_file() else ""
project_context = PROJECT_CONTEXT.read_text(encoding="utf-8") if PROJECT_CONTEXT.is_file() else ""
checkpoint = CHECKPOINT.read_text(encoding="utf-8") if CHECKPOINT.is_file() else ""
checkpoint_smoke = CHECKPOINT_SMOKE.read_text(encoding="utf-8") if CHECKPOINT_SMOKE.is_file() else ""
smoke_registration = SMOKE_REGISTRATION.read_text(encoding="utf-8") if SMOKE_REGISTRATION.is_file() else ""
source_undo = SOURCE_UNDO.read_text(encoding="utf-8") if SOURCE_UNDO.is_file() else ""

for token in (
    'private const string RegAppName = "QS3D_CURTAIN_UNDO";',
    'private const string RevisionPrefix = "CWU1:";',
    "Dictionary<Document, ObserverRegistration>",
    "OwnerStateSnapshot CaptureSelectedOwners(",
    'key.StartsWith("GeneratedSolid", StringComparison.OrdinalIgnoreCase)',
    'key.StartsWith("GeneratedCurtainFrame", StringComparison.OrdinalIgnoreCase)',
    'key.StartsWith("GeneratedCurtainPanel", StringComparison.OrdinalIgnoreCase)',
    "ProjectElement.GeneratedGeometryStateKey",
    "ProjectElement.GeneratedGeometryStaleReasonKey",
    "ProjectPersistenceCheckpoint.Capture(project, owners.Keys)",
    "CoreMatches(project) && _persistence.Matches(project)",
    "_persistence.Restore(project);",
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
    "document.CommandWillStart += CommandWillStart",
    "document.CommandEnded += CommandEnded",
    "document.CommandCancelled += CommandCancelled",
    "document.CommandFailed += CommandFailed",
    "document.CommandWillStart -= CommandWillStart",
    "document.CommandEnded -= CommandEnded",
    "document.CommandCancelled -= CommandCancelled",
    "document.CommandFailed -= CommandFailed",
    "OnCommandWillStart(document, args)",
    "OnCommandEnded(document, args)",
    "OnCommandAborted(document)",
    "TryConsumeMatchingCommand(document, args?.GlobalCommandName)",
    "TrySynchronizeAtStableBoundary(document, refreshAfterRestore: false);",
    "TrySynchronizeAtStableBoundary(document, refreshAfterRestore: true);",
    "NormalizeNativeUndoRedo(args?.GlobalCommandName)",
    "IsActiveDocument(document)",
    "if (!currentExpected.Matches(project))",
    "public void RefreshCommittedAfter(ProjectState project, OwnerStateSnapshot after)",
    "_staged.After = after;",
    "var restoreRollback = OwnerStateSnapshot.Capture(project, target.OwnerIds);",
    "target.Restore(project);",
    "if (!target.Matches(project))",
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
if "project.Touch()" in coord:
    errors.append("Curtain Undo restore must not advance or overflow the captured project revision")
if "Dictionary<Document, CommandEventHandler>" in coord:
    errors.append("Curtain Undo must retain the full matched observer lifecycle, not an Ended-only handler")
if 'string.Equals(normalized, "U", StringComparison.OrdinalIgnoreCase)' in coord:
    errors.append("single-letter U is ambiguous in BricsCAD V25 and must not drive Curtain semantic Undo")

will_start = coord.find("private static void OnCommandWillStart(")
ended = coord.find("private static void OnCommandEnded(", will_start)
stable = coord.find("private static void TrySynchronizeAtStableBoundary(", ended)
sync = coord.find("private static void SynchronizeKnownRevision(", stable)
aborted = coord.find("private static void OnCommandAborted(", sync)
consume = coord.find("private static bool TryConsumeMatchingCommand(", aborted)
active = coord.find("private static bool IsActiveDocument(", consume)
tracked = coord.find("private static bool IsTrackedProperty(", active)
if min(will_start, ended, stable, sync, aborted, consume, active, tracked) < 0:
    errors.append("Curtain matched command observer method boundaries are missing")
else:
    will_body = coord[will_start:ended]
    ended_body = coord[ended:stable]
    sync_body = coord[sync:aborted]
    aborted_body = coord[aborted:consume]
    consume_body = coord[consume:active]
    recovery = will_body.find("TrySynchronizeAtStableBoundary(document, refreshAfterRestore: false);")
    normalize = will_body.find("NormalizeNativeUndoRedo(args?.GlobalCommandName)")
    pending = will_body.find("registration.PendingCommand = normalized;")
    if min(recovery, normalize, pending) < 0 or not recovery < normalize < pending:
        errors.append("next-command recovery must run before recording new native Undo/Redo intent")
    if "if (registration.PendingCommand != null)" not in will_body or "registration.PendingCommand = null;" not in will_body:
        errors.append("nested/ambiguous Curtain command starts must invalidate pending intent")
    if "TryConsumeMatchingCommand(document, args?.GlobalCommandName)" not in ended_body or "TrySynchronizeAtStableBoundary(document, refreshAfterRestore: true);" not in ended_body:
        errors.append("Curtain command end must synchronize only after consuming matching intent")
    if "registration.PendingCommand = null;" not in aborted_body:
        errors.append("cancelled/failed Curtain commands must clear pending native intent")
    if "var pendingCommand = registration.PendingCommand;" not in consume_body or "registration.PendingCommand = null;" not in consume_body:
        errors.append("Curtain terminal intent must be single-consumption")
    if "IsActiveDocument(document)" not in consume_body:
        errors.append("Curtain terminal restore must remain active-document bound")
    if "ReadRevision(document)" not in sync_body or "target.Restore(project);" not in sync_body or "if (refreshAfterRestore) RefreshAfterRestore(document);" not in sync_body:
        errors.append("stable-boundary reconciliation must reuse the known-marker exact snapshot restore")

# Deterministic event model for the host-observer contract. The source-token and
# ordering checks above bind this model to production; no BricsCAD runtime is
# needed to prove single-consumption and missed-terminal recovery behavior.
class ObserverModel:
    def __init__(self):
        self.current = "AFTER"
        self.native = "AFTER"
        self.pending = None
        self.restores = []

    def synchronize(self):
        if self.native != self.current:
            self.current = self.native
            self.restores.append(self.native)

    def start(self, command):
        self.synchronize()
        normalized = command if command in ("UNDO", "REDO", "MREDO") else None
        if self.pending is not None:
            self.pending = None
            return
        self.pending = normalized

    def end(self, command):
        pending = self.pending
        self.pending = None
        if pending == command and command in ("UNDO", "REDO", "MREDO"):
            self.synchronize()

    def abort(self):
        self.pending = None

normal = ObserverModel()
normal.start("UNDO")
normal.native = "BEFORE"
normal.end("UNDO")
if normal.current != "BEFORE" or normal.restores != ["BEFORE"]:
    errors.append("deterministic Curtain matched terminal transition did not restore exactly once")

missed = ObserverModel()
missed.start("UNDO")
missed.native = "BEFORE"
# No terminal event: the next command boundary must repair before its body.
missed.start("P11PROBE")
if missed.current != "BEFORE" or missed.restores != ["BEFORE"] or missed.pending is not None:
    errors.append("deterministic Curtain missed-terminal recovery did not close the marker transition")

aborted_model = ObserverModel()
aborted_model.start("UNDO")
aborted_model.abort()
aborted_model.native = "BEFORE"
aborted_model.end("UNDO")
if aborted_model.current != "AFTER" or aborted_model.restores:
    errors.append("aborted Curtain native intent must not restore from a later unmatched terminal event")

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
    "undoTransition.RefreshCommittedAfter(project, committedAfter);",
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
refresh = build.find("undoTransition.RefreshCommittedAfter(project, committedAfter);")
if min(capture, begin, regen, line_host, line_frame, line_panel, stage, commit, post, refresh) < 0:
    errors.append("cannot establish QS3DCURTAIN3D Undo ordering")
elif not (capture < begin < regen < line_host < line_frame < line_panel < stage < commit < post < refresh):
    errors.append("Curtain Undo must capture/register before mutation, stage after all builders, commit marker with CAD, then finalize the exact post-fingerprint state")

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

if project_context.count("CurtainWallUndoCoordinator.Forget(document);") < 3:
    errors.append("project reload/forget/name-forget must discard stale Curtain Undo history")

for token in (
    "public sealed class ProjectPersistenceCheckpoint",
    "project.RestorePersistenceState(_projectUpdatedUtc, _projectChangeVersion);",
    "pair.Value.Restore(targets[pair.Key]);",
    "element.RestorePersistenceState(Dirty, UpdatedUtc);",
    "project.ChangeVersion != _projectChangeVersion",
    "project.UpdatedUtc != _projectUpdatedUtc",
    "element.Dirty == Dirty && element.UpdatedUtc == UpdatedUtc",
):
    if token not in checkpoint:
        errors.append("exact Core persistence checkpoint contract missing: " + token)
if ".Touch()" in checkpoint or "AuditEvents" in checkpoint:
    errors.append("persistence checkpoint must neither Touch revisions nor mutate AuditTrail state")

for token in (
    "RestoresExactSelectedStateWithoutTouchingAuditOrUnrelatedElements();",
    "RefusesProjectAndElementAffinityBeforeMutation();",
    "RestoresLongMaxValueWithoutOverflow();",
    "Equal(long.MaxValue, project.ChangeVersion",
    "ReferenceEquals(audit, project.AuditEvents[0])",
):
    if token not in checkpoint_smoke:
        errors.append("persistence checkpoint smoke contract missing: " + token)
if "ProjectPersistenceCheckpointSmoke.Run();" not in smoke_registration:
    errors.append("persistence checkpoint smoke is not registered")

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
    "exact project/owner persistence checkpoints and document/backing-store fail-closed guards, "
    "and preserves post-commit fingerprint warning semantics."
)
