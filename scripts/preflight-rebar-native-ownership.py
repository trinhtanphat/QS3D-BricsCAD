#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CAD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad"
SERVICE = CAD / "GeneratedRebarNativeOwnershipService.cs"
BUILDERS = {
    "BeamRebarSolidBuilder.cs": "GeneratedRebarHandles",
    "BeamStirrupSolidBuilder.cs": "GeneratedBeamStirrupHandles",
    "ColumnRebarSolidBuilder.cs": "GeneratedRebarHandles",
    "ColumnTieSolidBuilder.cs": "GeneratedTieRebarHandles",
    "ShapeRebarSolidBuilder.cs": "GeneratedShapeRebarHandles",
    "SlabMeshSolidBuilder.cs": "GeneratedSlabMeshHandles",
    "StructuralWallMeshSolidBuilder.cs": "GeneratedWallMeshHandles",
    "FoundationMeshSolidBuilder.cs": "GeneratedFoundationMeshHandles",
}
errors = []

if not SERVICE.is_file():
    errors.append("missing GeneratedRebarNativeOwnershipService.cs")
else:
    text = SERVICE.read_text(encoding="utf-8")
    for token in (
        'private const string RegAppName = "QS3D_REBAR";',
        'private const string OwnershipVersion = "1";',
        "GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(propertyKey.Trim())",
        "entity.GetXDataForApplication(RegAppName)",
        "entity == null || entity.IsErased || !entity.IsNewObject",
        "MarkGenerated(document, transaction, entity, project, element, propertyKey);",
    ):
        if token not in text:
            errors.append("native ownership service missing token: " + token)

for name, owner_slot in BUILDERS.items():
    path = CAD / name
    if not path.is_file():
        errors.append("missing rebar builder: " + name)
        continue
    text = path.read_text(encoding="utf-8")
    if "RequireMatchingOwnership(" not in text:
        errors.append(name + " must verify native ownership before destructive erase")
    if "solid.Erase();" in text:
        erase = text.find("solid.Erase();")
        require = text.rfind("RequireMatchingOwnership(", 0, erase)
        if require < 0:
            errors.append(name + " erases generated Solid3d without a preceding native ownership check")
    if owner_slot not in text:
        errors.append(name + " missing canonical owner slot " + owner_slot)
    if "MarkGenerated(" not in text and "MarkFreshGeneratedHandles(" not in text:
        errors.append(name + " does not mark newly generated rebar ownership")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: generated rebar, stirrup, tie, shape, slab, wall and foundation solids use project/element/owner-slot native ownership markers and fail closed before destructive erase.")
