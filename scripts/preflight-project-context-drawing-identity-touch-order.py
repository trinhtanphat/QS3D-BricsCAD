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
        touch = "project.Touch();"
        assign_path = "project.DrawingPath = drawing;"
        for token in (noop, touch, assign_path):
            if token not in sync:
                errors.append("SyncDrawingIdentity missing contract token: " + token)
        positions = [sync.find(noop), sync.find(touch), sync.find(assign_path)]
        if all(position >= 0 for position in positions) and positions != sorted(positions):
            errors.append("SyncDrawingIdentity must no-op first, then Touch, then assign DrawingPath")
        if "project.DrawingPath = drawing;\n            project.Touch();" in sync:
            errors.append("SyncDrawingIdentity regressed to mutating DrawingPath before Touch")

    if adopt:
        snapshot = "var elements = project.Elements.ToList();"
        null_guard = "if (elements.Any(x => x == null))"
        null_error = 'throw new InvalidOperationException("Project contains a null element entry.");'
        touch = "project.Touch();"
        assign_path = "project.DrawingPath = drawing;"
        assign_fingerprint = "project.DrawingFingerprint = fingerprint;"
        foreach_snapshot = "foreach (var element in elements)"
        required = (snapshot, null_guard, null_error, touch, assign_path, assign_fingerprint, foreach_snapshot)
        for token in required:
            if token not in adopt:
                errors.append("AdoptDrawingIdentity missing contract token: " + token)
        positions = [
            adopt.find(snapshot),
            adopt.find(null_guard),
            adopt.find(null_error),
            adopt.find(touch),
            adopt.find(assign_path),
            adopt.find(assign_fingerprint),
            adopt.find(foreach_snapshot),
        ]
        if all(position >= 0 for position in positions) and positions != sorted(positions):
            errors.append("AdoptDrawingIdentity must snapshot/validate elements and Touch before identity mutation")
        if "foreach (var element in project.Elements)" in adopt:
            errors.append("AdoptDrawingIdentity must mutate the prevalidated element snapshot")

if not PROJECT_STATE.is_file():
    errors.append("missing ProjectState source")
else:
    project_state = PROJECT_STATE.read_text(encoding="utf-8")
    if "var nextChangeVersion = checked(ChangeVersion + 1L);" not in project_state:
        errors.append("ProjectState.Touch no longer exposes the checked version-advance contract pinned by this gate")

print("QS3D project-context drawing-identity touch-order preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: drawing identity synchronization validates first and advances project persistence state before mutation.")
