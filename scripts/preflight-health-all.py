#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

command = ROOT / "src/QS3D.BricsCAD.V25/HealthAllCommands.cs"
services = [
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarModeHealthService.cs",
]
for path in [command] + services:
    if not path.is_file(): errors.append("missing unified-health file: " + str(path.relative_to(ROOT)))

owners = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    if re.search(r'\[CommandMethod\("QS3DHEALTHALL"', text, re.IGNORECASE): owners.append(str(path.relative_to(ROOT)))
if len(owners) != 1: errors.append("QS3DHEALTHALL must have exactly one CommandMethod owner; found: " + ", ".join(owners))

if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DHEALTHALL"',
        "new ModelHealthService().Inspect",
        "new GeneratedGeometryStaleHealthService().Inspect",
        "new GeneratedRebarHealthService().InspectAll",
        "new GeneratedTieRebarHealthService().Inspect",
        "new GeneratedBeamStirrupHealthService().Inspect",
        "new GeneratedSlabMeshHealthService().Inspect",
        "new GeneratedWallMeshHealthService().Inspect",
        "new GeneratedRebarOwnershipHealthService().Inspect",
        "new GeneratedRebarModeHealthService().Inspect",
        'PropertyHandles(project, "GeneratedSolidHandle")',
        'PropertyHandles(project, "GeneratedRebarHandles")',
        'PropertyHandles(project, "GeneratedShapeRebarHandles")',
        'PropertyHandles(project, "GeneratedTieRebarHandles")',
        'PropertyHandles(project, "GeneratedBeamStirrupHandles")',
        'PropertyHandles(project, "GeneratedSlabMeshHandles")',
        'PropertyHandles(project, "GeneratedWallMeshHandles")',
        'normalized.Contains("SLAB_MESH")',
        'normalized.Contains("WALL_MESH")',
        "GroupBy(x => x.Severity +",
        "LocateHandles",
        "QS3DZOOMSELECTED",
        "ModelHealthWindow",
    ):
        if needle not in text: errors.append("unified health command missing: " + needle)

ownership = ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs"
if ownership.is_file():
    text = ownership.read_text(encoding="utf-8")
    for needle in ("GeneratedRebarHandles", "GeneratedShapeRebarHandles", "GeneratedTieRebarHandles", "GeneratedBeamStirrupHandles", "GeneratedSlabMeshHandles", "GeneratedWallMeshHandles"):
        if needle not in text: errors.append("cross-family ownership health missing: " + needle)

print("QS3D unified full-health preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: full model/generated/rebar health aggregation covers longitudinal, shape, tie, stirrup, slab mesh, wall mesh, cross-family ownership, mode semantics, dedupe and Locate wiring.")
