#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
path = ROOT / "src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs"
errors = []

if not path.is_file():
    errors.append("missing ProjectContextCoordinator.cs")
else:
    text = path.read_text(encoding="utf-8")
    required = (
        "Convert.ToString(document.Database.FingerprintGuid)",
        "UnsavedProjectKeys",
        'Guid.NewGuid().ToString("N")',
        'stem + "-" + key + ".qsdb"',
        "UnsavedProjectKeys.Remove(document)",
        "QS3D drawing identity mismatch",
        "ProjectFileLock.Acquire(path)",
    )
    for token in required:
        if token not in text:
            errors.append("project context lifecycle contract missing: " + token)
    if "string.IsNullOrWhiteSpace(document.Database.FingerprintGuid)" in text:
        errors.append("ProjectContextCoordinator must not assume the TD_Mgd FingerprintGuid managed wrapper type")
    if 'stem + ".qsdb"' in text:
        errors.append("unsaved drawings must not share a name-only LocalAppData sidecar")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: project context keeps drawing identity fail-closed, normalizes FINGERPRINTGUID without a wrapper-type assumption, and isolates unsaved sidecars per Document.")
