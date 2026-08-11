#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CONTEXT = ROOT / "src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs"
errors = []

if not CONTEXT.is_file():
    print("FAIL: missing " + str(CONTEXT.relative_to(ROOT)))
    sys.exit(1)

source = CONTEXT.read_text(encoding="utf-8")


def block(text: str, start_token: str, end_token: str) -> str:
    start = text.find(start_token)
    end = text.find(end_token, start + len(start_token)) if start >= 0 else -1
    if start < 0 or end < 0:
        errors.append("cannot isolate block: " + start_token)
        return ""
    return text[start:end]


save = block(
    source,
    "public static string Save(Document document)",
    "public static ProjectState Reload(Document document)",
)
backing = block(
    source,
    "private static void EnsureBackingStoreUnchanged(",
    "private static void EnsureStableCapture(",
)
sync = block(
    source,
    "private static void SyncDrawingIdentity(ProjectState project, Document document)",
    "private static void ValidateDrawingIdentityReadOnly(",
)
adopt = block(
    source,
    "private static void AdoptDrawingIdentity(ProjectState project, string drawing, string fingerprint, string previousFingerprint)",
    "private static bool SameDrawingName(",
)

required_save = (
    'ExistingProjectMutationContext.Require(document, "Save Project")',
    "var path = GetProjectPath(document);",
    "RecoveryRequiredKey",
    "CaptureRecoveryMetadata(project)",
    "ClearRecoveryMetadata(project);",
    "ProjectFileLock.Acquire(path)",
    'EnsureBackingStoreUnchanged(document, project, true, "QS3D save")',
    "SidecarRevisionStamps.TryGetValue(document, out var baseline)",
    "var pathTransition = !baseline.IsForPath(path);",
    "SyncDrawingIdentity(project, document);",
    "Store.SaveNew(project, path);",
    "Store.SavePreservingValidatedBackup(project, path);",
    "Store.Save(project, path);",
    "SidecarRevisionStamps[document] = ProjectSidecarRevisionStamp.Capture(path);",
    "GetPersistenceStamp(document, project).MarkSaved(project);",
    "RestoreMetadata(project, recoveryMetadata);",
)
for needle in required_save:
    if needle not in save:
        errors.append("Save contract missing: " + needle)

if save.count("SyncDrawingIdentity(project, document);") != 1:
    errors.append("Save must synchronize drawing identity exactly once")

if save:
    require_pos = save.find('ExistingProjectMutationContext.Require(document, "Save Project")')
    path_pos = save.find("var path = GetProjectPath(document);")
    recovery_block_pos = save.find("RecoveryRequiredKey")
    capture_recovery_pos = save.find("CaptureRecoveryMetadata(project)")
    clear_recovery_pos = save.find("ClearRecoveryMetadata(project);")
    lock_pos = save.find("ProjectFileLock.Acquire(path)")
    freshness_pos = save.find('EnsureBackingStoreUnchanged(document, project, true, "QS3D save")')
    baseline_pos = save.find("SidecarRevisionStamps.TryGetValue(document, out var baseline)")
    transition_pos = save.find("var pathTransition = !baseline.IsForPath(path);")
    sync_pos = save.find("SyncDrawingIdentity(project, document);")
    save_new_pos = save.find("Store.SaveNew(project, path);")
    save_recovery_pos = save.find("Store.SavePreservingValidatedBackup(project, path);")
    save_normal_pos = save.find("Store.Save(project, path);")
    revision_pos = save.find("SidecarRevisionStamps[document] = ProjectSidecarRevisionStamp.Capture(path);")
    mark_pos = save.find("GetPersistenceStamp(document, project).MarkSaved(project);")
    restore_pos = save.find("RestoreMetadata(project, recoveryMetadata);")

    ordered = (
        require_pos,
        path_pos,
        recovery_block_pos,
        capture_recovery_pos,
        clear_recovery_pos,
        lock_pos,
        freshness_pos,
        baseline_pos,
        transition_pos,
        sync_pos,
    )
    if any(pos < 0 for pos in ordered) or list(ordered) != sorted(ordered):
        errors.append("Save must complete all fail-fast no-write guards before drawing-identity mutation")

    store_positions = (save_new_pos, save_recovery_pos, save_normal_pos)
    if any(pos < 0 for pos in store_positions) or any(sync_pos >= pos for pos in store_positions):
        errors.append("drawing identity must synchronize before every Store save dispatch")
    if not (max(store_positions) < revision_pos < mark_pos):
        errors.append("sidecar revision capture and MarkSaved must remain after Store dispatches")
    if restore_pos < mark_pos:
        errors.append("recovery metadata restore must remain in the post-save catch path")

for forbidden in (
    "ProjectStateSnapshot.Capture",
    "ProjectStateSnapshot.CreateDetachedCopy",
    ".Restore(project)",
    "RestorePersistenceState",
):
    if forbidden in save:
        errors.append("Save must not introduce broad RAM rollback across possibly-published I/O: " + forbidden)

for needle in (
    "baseline.MatchesCurrent()",
    "var currentPath = GetProjectPath(document);",
    "baseline.IsForPath(currentPath)",
    "ProjectSidecarRevisionStamp.Capture(currentPath)",
    "if (target.HasAnyFile)",
    "refused to overwrite an existing QS3D sidecar at the new DWG path",
):
    if needle not in backing:
        errors.append("backing-store/path-transition guard changed: " + needle)

if "project.Touch();" not in sync or "project.Touch();" not in adopt:
    errors.append("test premise changed: drawing identity synchronizers are expected to remain state mutators")

if errors:
    for error in errors:
        print("FAIL:", error)
    sys.exit(1)

print(
    "PASS: ProjectContextCoordinator.Save keeps recovery/path/freshness/baseline guards ahead of "
    "drawing-identity mutation, synchronizes identity only immediately before authorized Store dispatch, "
    "and preserves same-lock revision stamping without broad post-I/O rollback."
)
