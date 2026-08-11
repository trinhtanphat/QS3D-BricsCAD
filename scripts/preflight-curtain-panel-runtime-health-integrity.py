#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainPanelRuntimeHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing curtain-panel runtime health source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '"CURTAIN_PANEL_NATIVE_HANDLE_INVALID"',
        '"CURTAIN_PANEL_NATIVE_HANDLE_UNRESOLVED"',
        '"CURTAIN_PANEL_NATIVE_ENTITY_MISSING"',
        '"CURTAIN_PANEL_NATIVE_ENTITY_TYPE_MISMATCH"',
        '"CURTAIN_PANEL_NATIVE_OWNERSHIP_MISMATCH"',
        "CadHandleService.NormalizeHexHandle(token)",
        "CadHandleService.Resolve(document, new[] { canonical })",
        "OpenMode.ForRead",
    )
    for token in required:
        if token not in text:
            errors.append("missing curtain-panel fail-visible/read-only token: " + token)

    forbidden_silent = (
        "if (canonical == null) continue;",
        "if (ids.Count != 1) continue;",
        "if (!(entity is Solid3d solid) || solid.IsErased) continue;",
    )
    for token in forbidden_silent:
        if token in text:
            errors.append("curtain-panel health silently skips corrupt metadata: " + token)

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
            errors.append("curtain-panel health must remain read-only; forbidden token: " + token)

print("QS3D curtain-panel runtime-health integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: curtain-panel runtime health keeps corrupt generated references fail-visible and read-only.")
