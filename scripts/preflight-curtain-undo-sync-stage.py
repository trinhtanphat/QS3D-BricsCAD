#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COORD = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurtainWallUndoCoordinator.cs"
source = COORD.read_text(encoding="utf-8")
errors = []

codes = (
    "PREPARE_TRANSITION",
    "TARGET_OWNER_RESTORE",
    "TARGET_PERSISTENCE_RESTORE",
    "TARGET_MATCH",
    "RECOVERY",
)
for code in codes:
    if f'"{code}"' not in source:
        errors.append("missing sanitized Curtain Undo sync stage: " + code)

if "CURTAIN_UNDO_SYNC_" not in source:
    errors.append("Curtain Undo warning must expose only the sanitized stage prefix")

sync_start = source.find("private static void TrySynchronizeAtStableBoundary")
sync_end = source.find("private static void SynchronizeKnownRevision", sync_start)
if sync_start < 0 or sync_end < 0:
    errors.append("Curtain Undo stable-boundary sync block is missing")
else:
    sync = source[sync_start:sync_end]
    if "error.Message" in sync:
        errors.append("Curtain Undo stable-boundary warning must not expose exception text")
    if "SyncStageException" not in sync:
        errors.append("Curtain Undo stable-boundary warning must classify SyncStageException")

for forbidden in (
    '"QS3D Curtain Undo sync warning: " + error.Message',
    '"QS3D Curtain Undo sync warning: " + restoreError.Message',
    '"QS3D Curtain Undo sync warning: " + rollbackError.Message',
):
    if forbidden in source:
        errors.append("Curtain Undo warning leaks internal exception detail")

if "throw new SyncStageException(" not in source:
    errors.append("Curtain Undo synchronization must wrap failures with a sanitized stage")

if errors:
    for error in errors:
        print("FAIL: " + error)
    raise SystemExit(1)

print("PASS: Curtain Undo sync stage diagnostics are sanitized and allowlisted")
