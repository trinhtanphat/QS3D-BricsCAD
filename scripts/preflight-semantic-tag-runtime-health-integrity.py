#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticTagRuntimeHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing semantic-tag runtime health source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '"SEMANTIC_TAG_HANDLE_INVALID"',
        '"SEMANTIC_TAG_MISSING"',
        '"SEMANTIC_TAG_TYPE_MISMATCH"',
        '"SEMANTIC_TAG_OWNERSHIP_MISMATCH"',
        "long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)",
        "OpenMode.ForRead",
        "entity is MText mtext",
        "entity is MLeader mleader",
    )
    for token in required:
        if token not in text:
            errors.append("missing semantic-tag fail-visible/read-only token: " + token)

    if "if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) continue;" in text:
        errors.append("semantic-tag health silently skips malformed persisted handle")

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
            errors.append("semantic-tag health must remain read-only; forbidden token: " + token)

print("QS3D semantic-tag runtime-health integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: semantic-tag runtime health surfaces malformed handles and MText/MLeader type/ownership drift while preserving read-only inspection.")
