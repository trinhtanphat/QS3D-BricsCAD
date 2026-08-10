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
    "foundation_service": ROOT / "src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs",
    "general_service": ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs",
    "ownership_service": ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs",
    "ownership_policy": ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs",
    "ribbon": ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs",
    "geometry_hub": ROOT / "src/QS3D.BricsCAD.V25/UI/GeometryExtensionsWindow.xaml",
    "rebar_hub": ROOT / "src/QS3D.BricsCAD.V25/UI/Rebar3DHubWindow.xaml",
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
        'FoundationMeshSolidBuilder.HandlesKey',
        'GeneratedRebarHealthService().InspectAll',
        'GeneratedTieRebarHealthService().Inspect',
        'GeneratedBeamStirrupHealthService().Inspect',
        'GeneratedSlabMeshHealthService().Inspect',
        'GeneratedWallMeshHealthService().Inspect',
        'GeneratedFoundationMeshHealthService().Inspect',
        'GeneratedRebarOwnershipHealthService().Inspect',
        'code.IndexOf("BEAM_STIRRUP"',
        'code.IndexOf("SLAB_MESH"',
        'code.IndexOf("WALL_MESH"',
        'code.IndexOf("FOUNDATION_MESH"',
    ],
    "ownership_service": [
        'GeneratedHandleOwnershipPolicy.RebarHandleKeys',
        'REBAR_GENERATED_CROSS_KEY_OWNERSHIP_CONFLICT',
    ],
    "ownership_policy": [
        'RebarHandleKeys', 'GeneratedRebarHandles', 'GeneratedShapeRebarHandles', 'GeneratedTieRebarHandles',
        'GeneratedBeamStirrupHandles', 'GeneratedSlabMeshHandles', 'GeneratedWallMeshHandles', 'GeneratedFoundationMeshHandles',
    ],
    "stirrup_service": ['GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)'],
    "tie_service": ['GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)'],
    "slab_service": ['GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)'],
    "wall_service": ['GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)'],
    "ribbon": ['"QS3DREBARHEALTHALL"', '"QS3DSLABREBARHEALTH"', '"QS3DWALLREBARHEALTH"', '"QS3DFOUNDATIONREBARHEALTH"'],
    "geometry_hub": ['Tag="QS3DREBARHEALTHALL"', 'Tag="QS3DSLABREBARHEALTH"', 'Tag="QS3DWALLREBARHEALTH"'],
    "rebar_hub": ['Tag="QS3DREBARHEALTHALL"', 'Tag="QS3DFOUNDATIONREBARHEALTH"'],
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

print("PASS: unified rebar health covers longitudinal, BBS shape, column tie, beam stirrup, slab mesh, wall mesh and foundation mesh with shared policy-driven ownership and UI exposure.")
