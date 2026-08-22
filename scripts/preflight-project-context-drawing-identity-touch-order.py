#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COORDINATOR = ROOT / "src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs"
PROJECT_STATE = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
errors = []


def method_block(text: str, start_token: str, end_token: str, label: str) -> str:
    start = text.find(start_token)
    end = text.find(end_token, start + 1) if start >= 0 else -1
    if start < 0 or end < 0:
        errors.append("cannot isolate " + label)
        return ""
    return text[start:end]


if not COORDINATOR.is_file():
    errors.append("missing ProjectContextCoordinator source")
else:
    text = COORDINATOR.read_text(encoding="utf-8")
    sync = method_block(
        text,
        "private static void SyncDrawingIdentity(ProjectState project, Document document)",
        "private static void ValidateDrawingIdentityReadOnly(ProjectState project, Document document)",
        "SyncDrawingIdentity",
    )
    adopt = method_block(
        text,
        "private static void AdoptDrawingIdentity(ProjectState project, string drawing, string fingerprint, string previousFingerprint)",
        "private static bool SameDrawingName(string? left, string? right)",
        "AdoptDrawingIdentity",
    )

    if sync:
        noop = "if (SameDrawingName(storedPath, drawing)) return;"
        assign_path = "project.DrawingPath = drawing;"
        for token in (noop, assign_path):
            if token not in sync:
                errors.append("SyncDrawingIdentity missing contract token: " + token)
        positions = [sync.find(noop), sync.find(assign_path)]
        if all(position >= 0 for position in positions) and positions != sorted(positions):
            errors.append("SyncDrawingIdentity must no-op before assigning DrawingPath")
        if "project.Touch();" in sync:
            errors.append("SyncDrawingIdentity must not add an adapter-owned revision before the persisted DrawingPath scalar")

    if adopt:
        snapshot = "var elements = project.Elements.ToList();"
        null_guard = "if (elements.Any(x => x == null))"
        null_error = 'throw new InvalidOperationException("Project contains a null element entry.");'
        path_changed = "var pathChanged = !string.Equals(project.DrawingPath, drawing, StringComparison.Ordinal);"
        fingerprint_changed = "var fingerprintChanged = !string.Equals(project.DrawingFingerprint, fingerprint, StringComparison.Ordinal);"
        scalar_changes = "var scalarChanges = (pathChanged ? 1L : 0L) + (fingerprintChanged ? 1L : 0L);"
        capacity = "_ = checked(project.ChangeVersion + scalarChanges);"
        assign_path = "project.DrawingPath = drawing;"
        assign_fingerprint = "project.DrawingFingerprint = fingerprint;"
        foreach_snapshot = "foreach (var element in elements)"
        required = (
            snapshot,
            null_guard,
            null_error,
            path_changed,
            fingerprint_changed,
            scalar_changes,
            capacity,
            assign_path,
            assign_fingerprint,
            foreach_snapshot,
        )
        for token in required:
            if token not in adopt:
                errors.append("AdoptDrawingIdentity missing contract token: " + token)
        positions = [adopt.find(token) for token in required]
        if all(position >= 0 for position in positions) and positions != sorted(positions):
            errors.append("AdoptDrawingIdentity must validate the element snapshot and scalar revision capacity before identity mutation")
        if "project.Touch();" in adopt:
            errors.append("AdoptDrawingIdentity must not add an adapter-owned revision before persisted scalar assignments")
        if "foreach (var element in project.Elements)" in adopt:
            errors.append("AdoptDrawingIdentity must mutate the prevalidated element snapshot")

if not PROJECT_STATE.is_file():
    errors.append("missing ProjectState source")
else:
    project_state = PROJECT_STATE.read_text(encoding="utf-8")
    required_state = (
        "set => SetPersistedScalar(ref _drawingPath, value);",
        "set => SetPersistedScalar(ref _drawingFingerprint, value);",
        "var nextChangeVersion = checked(ChangeVersion + 1L);",
    )
    for token in required_state:
        if token not in project_state:
            errors.append("ProjectState missing scalar-owned revision contract token: " + token)

print("QS3D project-context drawing-identity scalar-revision preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: drawing identity synchronization validates first, preflights exact scalar revision capacity, and relies on persisted drawing scalars as the only project revision owners.")
