#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
service = ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarModeHealthService.cs"
command = ROOT / "src/QS3D.BricsCAD.V25/RebarModeHealthCommands.cs"
for path in (service, command):
    if not path.is_file(): errors.append("missing rebar-mode health file: " + str(path.relative_to(ROOT)))

if service.is_file():
    text = service.read_text(encoding="utf-8")
    for needle in (
        "GeneratedRebarHandles", "GeneratedRebarMode", "COLUMNVERTICALBARS", "BEAMLONGITUDINALBARS",
        "GeneratedSlabMeshHandles", "GeneratedSlabMeshMode", "SlabMeshXY",
        "GeneratedWallMeshHandles", "GeneratedWallMeshMode", "StructuralWallMesh", "GENERATED_REBAR_MODE_MISSING",
        "GENERATED_REBAR_MODE_UNKNOWN", "GENERATED_REBAR_MODE_CATEGORY_MISMATCH",
        "GeneratedSlabMeshXActualSpacingM", "GeneratedSlabMeshYActualSpacingM",
        "GeneratedWallMeshHorizontalActualSpacingM", "GeneratedWallMeshVerticalActualSpacingM",
    ):
        if needle not in text: errors.append("generated rebar-mode health missing: " + needle)

if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in ('CommandMethod("QS3DREBARMODEHEALTH"', "GeneratedRebarModeHealthService().Inspect", "ModelHealthWindow", "GeneratedRebarHandles"):
        if needle not in text: errors.append("rebar-mode health command missing: " + needle)

owners = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    if re.search(r'\[CommandMethod\("QS3DREBARMODEHEALTH"', path.read_text(encoding="utf-8"), re.IGNORECASE): owners.append(str(path.relative_to(ROOT)))
if len(owners) != 1: errors.append("QS3DREBARMODEHEALTH must have exactly one owner; found: " + ", ".join(owners))

print("QS3D generated rebar-mode health preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: GeneratedRebar mode/category/metadata validation and review command are present.")
