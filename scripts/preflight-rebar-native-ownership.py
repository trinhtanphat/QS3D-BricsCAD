#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
CAD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad"
SERVICE = CAD / "GeneratedRebarNativeOwnershipService.cs"
OWNERSHIP_GUARD = CAD / "GeneratedRebarOwnershipGuard.cs"
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


def private_static_method(text, signature):
    start = text.find(signature)
    if start < 0:
        return ""
    tail_start = start + len(signature)
    match = re.search(r"\n\s*private static ", text[tail_start:])
    end = tail_start + match.start() if match else len(text)
    return text[start:end]


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

if not OWNERSHIP_GUARD.is_file():
    errors.append("missing GeneratedRebarOwnershipGuard.cs")
else:
    text = OWNERSHIP_GUARD.read_text(encoding="utf-8")
    for token in (
        "CadHandleService.NormalizeHexHandle(handle)",
        "EnsureCompleteLiveSet(element, propertyKey, expectedOwner);",
        "ReferenceEquals(_document, Application.DocumentManager.MdiActiveDocument)",
        "CadHandleService.Resolve(_document, expectedHandles)",
        "ids.Count != expectedHandles.Count",
        "StartOpenCloseTransaction()",
        "OpenMode.ForRead",
        "GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(",
        "Refusing destructive replacement before any rebar is erased.",
        "_validatedLiveSets.Add(expectedOwner);",
    ):
        if token not in text:
            errors.append("rebar exact-set ownership guard missing token: " + token)

    validate_start = text.find("private void EnsureCompleteLiveSet(")
    validate_end = text.find("public static OwnershipIndex Build(", validate_start)
    if validate_start < 0 or validate_end <= validate_start:
        errors.append("rebar ownership guard must keep a dedicated complete-live-set validator")
    else:
        validate = text[validate_start:validate_end]
        resolve = validate.find("CadHandleService.Resolve(_document, expectedHandles)")
        native = validate.find("GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(")
        mark_validated = validate.find("_validatedLiveSets.Add(expectedOwner);")
        if resolve < 0 or native < 0 or mark_validated < 0 or not (resolve < native < mark_validated):
            errors.append("rebar ownership guard must resolve the complete set and verify native ownership before caching validation")

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
        ensure_owned = text.rfind("ownership.", 0, erase)
        if require < 0:
            errors.append(name + " erases generated Solid3d without a preceding native ownership check")
        if ensure_owned < 0 or ensure_owned > erase:
            errors.append(name + " must route destructive replacement through the strict ownership/exact-set guard before erase")
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

    prevalidate = private_static_method(text, "private static void EnsureRebarSetLive(")
    if not prevalidate:
        errors.append("generated invalidator must keep a dedicated rebar prevalidation path")
    elif "GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(" not in prevalidate:
        errors.append("generated invalidator must verify native ownership during rebar prevalidation")

    erase = private_static_method(text, "private static void EraseRebarSet(")
    if not erase:
        errors.append("generated invalidator must keep a dedicated rebar erase path")
    else:
        require = erase.find("GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(")
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

print("PASS: all eight generated rebar replacement families route destructive erase through canonical exact-live-set prevalidation plus project/element/owner-slot native ownership; dependent invalidation keeps its dedicated strict path.")
