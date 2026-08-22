#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/SafeGeneratedHandleOwnershipHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing SafeGeneratedHandleOwnershipHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "GeneratedHandleOwnershipIndex.Build(project)",
        "catch (InvalidOperationException)",
        '"GENERATED_HANDLE_OWNERSHIP_INVALID_PROJECT"',
        "HealthSeverity.Error",
        '"Generated handle ownership cannot be inspected safely because the semantic project is invalid."',
        '"GENERATED_HANDLE_OWNERSHIP_CONFLICT"',
        "GeneratedHandleOwnershipPolicy.AreSameLogicalOwnerSlots",
    )
    for token in required:
        if token not in text:
            errors.append("missing safe generated ownership redaction token: " + token)

    forbidden = (
        "catch (InvalidOperationException ex)",
        "+ ex.Message",
        "ex.Message +",
        "OpenMode.ForWrite",
        ".UpgradeOpen(",
        "project.Touch(",
        ".Save(",
        ".Erase(",
    )
    for token in forbidden:
        if token in text:
            errors.append("safe ownership health regressed redaction/read-only contract: " + token)

print("QS3D safe generated ownership health redaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: malformed-project ownership errors remain fail-visible without raw canonical validation detail.")
