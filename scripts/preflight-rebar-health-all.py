#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = {
    "all": ROOT / "src/QS3D.BricsCAD.V25/RebarHealthAllCommands.cs",
    "stirrup_service": ROOT / "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs",
    "tie_service": ROOT / "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs",
    "slab_service": ROOT / "src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs",
    "wall_service": ROOT / "src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs",
    "general_service": ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs",
    "ownership_service": ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs",
    "ribbon": ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs",
    "geometry_hub": ROOT / "src/QS3D.BricsCAD.V25/UI/GeometryExtensionsWindow.xaml",
}

for path in required.values():
    if not path.is_file(): errors.append("missing unified rebar-health file: " + str(path.relative_to(ROOT)))

checks = {
    "all": [
        'CommandMethod("QS3DREBARHEALTHALL"',
        'Collect(project, "GeneratedRebarHandles")',
        'Collect(project, "GeneratedShapeRebarHandles")',
        'Collect(project, "GeneratedTieRebarHandles")',
        'Collect(project, "GeneratedBeamStirrupHandles")',
        'Collect(project, "GeneratedSlabMeshHandles")',
        'Collect(project, "GeneratedWallMeshHandles")',
        'GeneratedRebarHealthService().InspectAll',
        'GeneratedTieRebarHealthService().Inspect',
        'GeneratedBeamStirrupHealthService().Inspect',
        'GeneratedSlabMeshHealthService().Inspect',
        'GeneratedWallMeshHealthService().Inspect',
        'GeneratedRebarOwnershipHealthService().Inspect',
        'code.IndexOf("BEAM_STIRRUP"',
        'code.IndexOf("SLAB_MESH"',
        'code.IndexOf("WALL_MESH"',
    ],
    "ownership_service": [
        '"GeneratedRebarHandles"', '"GeneratedShapeRebarHandles"', '"GeneratedTieRebarHandles"',
        '"GeneratedBeamStirrupHandles"', '"GeneratedSlabMeshHandles"', '"GeneratedWallMeshHandles"',
    ],
    "ribbon": ['"QS3DREBARHEALTHALL"', '"QS3DSLABREBARHEALTH"', '"QS3DWALLREBARHEALTH"'],
    "geometry_hub": ['Tag="QS3DREBARHEALTHALL"', 'Tag="QS3DSLABREBARHEALTH"', 'Tag="QS3DWALLREBARHEALTH"'],
}

for key, needles in checks.items():
    path = required[key]
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(str(path.relative_to(ROOT)) + " missing unified-health token: " + needle)

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: unified rebar health covers longitudinal, BBS shape, column tie, beam stirrup, slab mesh and wall mesh generated families with cross-family ownership and UI exposure.")
