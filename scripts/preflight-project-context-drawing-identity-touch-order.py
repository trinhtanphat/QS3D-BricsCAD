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
    sync = method_block(text, "private static void SyncDrawingIdentity(ProjectState project, Document document)", "private static void ValidateDrawingIdentityReadOnly(ProjectState project, Document document)", "SyncDrawingIdentity")
    adopt = method_block(text, "private static void AdoptDrawingIdentity(ProjectState project, string drawing, string fingerprint, string previousFingerprint)", "private static bool SameDrawingName(string? left, string? right)", "AdoptDrawingIdentity")
    if sync:
        noop = "if (SameDrawingName(storedPath, drawing)) return;"
        assign_path = "project.DrawingPath = drawing;"
        for token in (noop, assign_path):
            if token not in sync: errors.append("SyncDrawingIdentity missing contract token: " + token)
        positions = [sync.find(noop), sync.find(assign_path)]
        if all(p >= 0 for p in positions) and positions != sorted(positions): errors.append("SyncDrawingIdentity must no-op before assigning DrawingPath")
        if "project.Touch();" in sync: errors.append("SyncDrawingIdentity must not add an adapter-owned revision before the persisted DrawingPath scalar")
    if adopt:
        required = (
            "var elements = project.Elements.ToList();",
            "if (elements.Any(x => x == null))",
            'throw new InvalidOperationException("Project contains a null element entry.");',
            "var pathChanged = !string.Equals(project.DrawingPath, drawing, StringComparison.Ordinal);",
            "var fingerprintChanged = !string.Equals(project.DrawingFingerprint, fingerprint, StringComparison.Ordinal);",
            "var scalarChanges = (pathChanged ? 1L : 0L) + (fingerprintChanged ? 1L : 0L);",
            "_ = checked(project.ChangeVersion + scalarChanges);",
            "project.DrawingPath = drawing;",
            "project.DrawingFingerprint = fingerprint;",
            "foreach (var element in elements)",
        )
        for token in required:
            if token not in adopt: errors.append("AdoptDrawingIdentity missing contract token: " + token)
        positions = [adopt.find(token) for token in required]
        if all(p >= 0 for p in positions) and positions != sorted(positions): errors.append("AdoptDrawingIdentity must validate the element snapshot and scalar revision capacity before identity mutation")
        if "project.Touch();" in adopt: errors.append("AdoptDrawingIdentity must not add an adapter-owned revision before persisted scalar assignments")
        if "foreach (var element in project.Elements)" in adopt: errors.append("AdoptDrawingIdentity must mutate the prevalidated element snapshot")

if not PROJECT_STATE.is_file():
    errors.append("missing ProjectState source")
else:
    project_state = PROJECT_STATE.read_text(encoding="utf-8")
    required_state = (
        "SetPersistedScalar(ref _drawingPath, PersistedTextXml.Verify(rawValue, nameof(value), \"Drawing path\"));",
        "set => SetCanonicalOptionalIdentity(ref _drawingFingerprint, value, \"Drawing fingerprint\");",
        "private void SetCanonicalOptionalIdentity(ref string field, string? value, string label)",
        "SetPersistedScalar(ref field, PersistedTextXml.Verify(normalizedValue, nameof(value), label));",
        "private void SetPersistedScalar(ref string field, string value)",
        "var nextChangeVersion = checked(ChangeVersion + 1L);",
    )
    for token in required_state:
        if token not in project_state: errors.append("ProjectState missing scalar-owned revision contract token: " + token)
    if "Drawing path cannot contain control characters." not in project_state:
        errors.append("DrawingPath must retain its persisted XML/control-character validation before scalar mutation")

print("QS3D project-context drawing-identity scalar-revision preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: drawing identity synchronization validates first, preflights exact scalar revision capacity, preserves canonical/persistable identity normalization, and relies on the shared persisted-scalar helper as the only project revision owner.")
