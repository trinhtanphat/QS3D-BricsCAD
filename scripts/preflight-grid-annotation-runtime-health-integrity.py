#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedGridAnnotationRuntimeHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing grid-annotation runtime health source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '"GRID_ANNOTATION_CAD_HANDLE_INVALID"',
        '"GRID_ANNOTATION_CAD_MISSING"',
        '"GRID_ANNOTATION_CAD_TYPE_MISMATCH"',
        '"GRID_ANNOTATION_CAD_OWNERSHIP_MISMATCH"',
        '"GRID_ANNOTATION_CAD_TEXT_STALE"',
        "long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)",
        "OpenMode.ForRead",
    )
    for token in required:
        if token not in text:
            errors.append("missing grid-annotation fail-visible/read-only token: " + token)

    if "if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) return;" in text:
        errors.append("grid-annotation health silently returns on malformed persisted handle")

    forbidden_mutation = (
        "OpenMode.ForWrite",
        ".UpgradeOpen(",
        "ProjectMutationContext",
        "project.Touch(",
        ".Save(",
        ".Erase(",
        "StampOwnership(",
        "SetXData(",
    )
    for token in forbidden_mutation:
        if token in text:
            errors.append("grid-annotation health must remain read-only; forbidden token: " + token)

print("QS3D grid-annotation runtime-health integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: grid-annotation runtime health surfaces malformed handles while preserving read-only inspection.")
