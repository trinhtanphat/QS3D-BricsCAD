#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

command = ROOT / "src/QS3D.BricsCAD.V25/HealthAllCommands.cs"
generic = ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs"
stale = ROOT / "src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs"
tie = ROOT / "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs"
stirrup = ROOT / "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs"

for path in (command, generic, stale, tie, stirrup):
    if not path.is_file(): errors.append("missing unified-health file: " + str(path.relative_to(ROOT)))

owners = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    if re.search(r'\[CommandMethod\("QS3DHEALTHALL"', text, re.IGNORECASE):
        owners.append(str(path.relative_to(ROOT)))
if len(owners) != 1:
    errors.append("QS3DHEALTHALL must have exactly one CommandMethod owner; found: " + ", ".join(owners))

if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DHEALTHALL"',
        "new ModelHealthService().Inspect",
        "new GeneratedGeometryStaleHealthService().Inspect",
        "new GeneratedRebarHealthService().InspectAll",
        "new GeneratedTieRebarHealthService().Inspect",
        "new GeneratedBeamStirrupHealthService().Inspect",
        'PropertyHandles(project, "GeneratedSolidHandle")',
        'PropertyHandles(project, "GeneratedRebarHandles")',
        'PropertyHandles(project, "GeneratedShapeRebarHandles")',
        'PropertyHandles(project, "GeneratedTieRebarHandles")',
        'PropertyHandles(project, "GeneratedBeamStirrupHandles")',
        "GroupBy(x => x.Severity +",
        "LocateHandles",
        "QS3DZOOMSELECTED",
        "ModelHealthWindow",
    ):
        if needle not in text: errors.append("unified health command missing: " + needle)

if generic.is_file():
    text = generic.read_text(encoding="utf-8")
    for needle in ("GeneratedTieRebarHandles", "GeneratedBeamStirrupHandles", "GeneratedSolidHandle", "SourceHandles"):
        if needle not in text: errors.append("generic rebar health cross-set ownership missing: " + needle)

if stale.is_file():
    text = stale.read_text(encoding="utf-8")
    for needle in ("TIE_REBAR_GENERATED_STALE", "BEAM_STIRRUP_GENERATED_STALE"):
        if needle not in text: errors.append("stale health coverage missing: " + needle)

print("QS3D unified full-health preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: full model/generated/rebar health aggregation, cross-set ownership, stale coverage, dedupe and locate wiring are present.")
