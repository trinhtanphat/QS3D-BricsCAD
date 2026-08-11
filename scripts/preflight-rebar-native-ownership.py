#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CAD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad"
SERVICE = CAD / "GeneratedRebarNativeOwnershipService.cs"
INVALIDATOR = CAD / "GeneratedDependentGeometryInvalidator.cs"
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

if not INVALIDATOR.is_file():
    errors.append("missing GeneratedDependentGeometryInvalidator.cs")
else:
    text = INVALIDATOR.read_text(encoding="utf-8")
    for token in (
        "EnsureRebarSetLive(document, project, element, key, expected);",
        "EraseRebarSet(document, transaction, project, element, key, rebarOwnership);",
        "private static void EnsureRebarSetLive(",
        "GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(",
    ):
        if token not in text:
            errors.append("generated invalidator missing rebar native-ownership token: " + token)

    prevalidate_start = text.find("private static void EnsureRebarSetLive(")
    generic_validate_start = text.find("private static void EnsureSolidSetLive(", prevalidate_start)
    if prevalidate_start < 0 or generic_validate_start <= prevalidate_start:
        errors.append("generated invalidator must keep a dedicated rebar prevalidation path")
    else:
        prevalidate = text[prevalidate_start:generic_validate_start]
        if "RequireMatchingOwnership(" not in prevalidate:
            errors.append("generated invalidator must verify native ownership during rebar prevalidation")

    erase_start = text.find("private static void EraseRebarSet(")
    curtain_start = text.find("private static void EraseCurtainFrames(", erase_start)
    if erase_start < 0 or curtain_start <= erase_start:
        errors.append("generated invalidator must keep a dedicated rebar erase path")
    else:
        erase = text[erase_start:curtain_start]
        require = erase.find("RequireMatchingOwnership(")
        destructive = erase.find("solid.Erase();")
        if require < 0 or destructive < 0 or require > destructive:
            errors.append("generated invalidator must verify matching native ownership before rebar Solid3d erase")
        if "EraseSolidSet(document, transaction, element, propertyKey, expected);" in erase:
            errors.append("generated invalidator rebar path must not bypass native ownership through generic EraseSolidSet")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: generated rebar builders and dependent invalidation use project/element/owner-slot native ownership markers and fail closed before destructive erase.")
