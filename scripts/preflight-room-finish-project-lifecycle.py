#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "SemanticCaptureService.cs"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"

errors = []


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing token: " + token)


def slice_method(source, start_token, end_token):
    start = source.find(start_token)
    end = source.find(end_token, start)
    if start < 0 or end < 0:
        errors.append("cannot isolate method: " + start_token)
        return ""
    return source[start:end]


source = SOURCE.read_text(encoding="utf-8") if SOURCE.is_file() else ""
inbox = INBOX.read_text(encoding="utf-8") if INBOX.is_file() else ""
if not source:
    errors.append("missing SemanticCaptureService.cs")
if not inbox:
    errors.append("missing docs/LOCAL-AGENT-INBOX.md")

capture = slice_method(source, "public static int Capture(", "public static bool CaptureSnapshot(")
capture_snapshot = slice_method(source, "public static bool CaptureSnapshot(", "private static bool CaptureSnapshotCore(")
finishes = slice_method(source, "public static int GenerateRoomFinishes(", "public static int SyncExistingRoomFinishes(")

# New semantic capture is an intentional authoring/bootstrap path; do not accidentally harden it away.
require(capture, "ProjectContextCoordinator.GetOrCreate(document)", "Capture authoring bootstrap")
require(capture_snapshot, "ProjectContextCoordinator.GetOrCreate(document)", "CaptureSnapshot authoring bootstrap")

require(finishes, "EntitySnapshotReader.ReadCurrentSelection(document)", "Room Finish selection acquisition")
require(finishes, "if (snapshots.Count == 0) return 0;", "Room Finish cancel boundary")
require(finishes, 'ExistingProjectMutationContext.Require(document, "Room finish generation")', "Room Finish existing-project boundary")
require(finishes, "ProjectStateSnapshot.Capture(project)", "Room Finish rollback snapshot")
require(finishes, "RoomFinishSynchronizationService.Synchronize", "Room Finish semantic synchronization")
require(finishes, "Regenerate(project, finish);", "Room Finish element-scoped regeneration")
if "ProjectContextCoordinator.GetOrCreate(document)" in finishes:
    errors.append("GenerateRoomFinishes must not create/cache a replacement project")
if "RegenerateDirty(" in finishes or "RegenerateProject(" in finishes:
    errors.append("GenerateRoomFinishes must not consume unrelated project dirty state through full-project regeneration")

selection = finishes.find("EntitySnapshotReader.ReadCurrentSelection(document)")
empty_return = finishes.find("if (snapshots.Count == 0) return 0;")
bind = finishes.find('ExistingProjectMutationContext.Require(document, "Room finish generation")')
snapshot = finishes.find("ProjectStateSnapshot.Capture(project)")
sync = finishes.find("RoomFinishSynchronizationService.Synchronize")
scoped_regen = finishes.find("Regenerate(project, finish);")
if min(selection, empty_return, bind, snapshot, sync, scoped_regen) < 0 or not selection < empty_return < bind < snapshot < sync < scoped_regen:
    errors.append("Room Finish must finish selection and return on cancel/empty before canonical binding, snapshot before mutation, and regenerate only the synchronized finish")

for token, label in [
    ("LOCAL-001 — exact V25 build/load baseline", "canonical local baseline item"),
    ("QS3DFINISH", "Room Finish local lifecycle scenario"),
    ("cancel/empty selection must return before canonical project bind/cache", "Room Finish cancel-before-bind local evidence"),
    ("no replacement project", "local no-replacement evidence"),
]:
    require(inbox, token, label)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DFINISH remains selection-scoped, existing-project-only, rollback-protected, and regenerates only synchronized Room Finish elements without consuming unrelated project dirty state; LOCAL-001 owns native V25 proof.")
