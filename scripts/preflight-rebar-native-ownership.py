#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
CAD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad"
SERVICE = CAD / "GeneratedRebarNativeOwnershipService.cs"
OWNERSHIP_GUARD = CAD / "GeneratedRebarOwnershipGuard.cs"
TIE_OWNERSHIP_GUARD = CAD / "GeneratedTieRebarOwnershipGuard.cs"
INVALIDATOR = CAD / "GeneratedDependentGeometryInvalidator.cs"
BUILDERS = {
    "BeamRebarSolidBuilder.cs": (
        "GeneratedRebarHandles",
        "GeneratedRebarOwnershipGuard.Build(project)",
        "ownership.EnsureOwned(",
    ),
    "BeamStirrupSolidBuilder.cs": (
        "GeneratedBeamStirrupHandles",
        "GeneratedRebarOwnershipGuard.Build(project)",
        "ownership.EnsureOwned(",
    ),
    "ColumnRebarSolidBuilder.cs": (
        "GeneratedRebarHandles",
        "GeneratedRebarOwnershipGuard.Build(project)",
        "ownership.EnsureOwned(",
    ),
    "ColumnTieSolidBuilder.cs": (
        "GeneratedTieRebarHandles",
        "GeneratedTieRebarOwnershipGuard.Build(project)",
        "ownership.EnsureTieOwned(",
    ),
    "ShapeRebarSolidBuilder.cs": (
        "GeneratedShapeRebarHandles",
        "GeneratedRebarOwnershipGuard.Build(project)",
        "ownership.EnsureOwned(",
    ),
    "SlabMeshSolidBuilder.cs": (
        "GeneratedSlabMeshHandles",
        "GeneratedRebarOwnershipGuard.Build(project)",
        "ownership.EnsureOwned(",
    ),
    "StructuralWallMeshSolidBuilder.cs": (
        "GeneratedWallMeshHandles",
        "GeneratedRebarOwnershipGuard.Build(project)",
        "ownership.EnsureOwned(",
    ),
    "FoundationMeshSolidBuilder.cs": (
        "GeneratedFoundationMeshHandles",
        "GeneratedRebarOwnershipGuard.Build(project)",
        "ownership.EnsureOwned(",
    ),
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


def check_exact_set_guard(path, label, ensure_call, validate_signature, resolve_token, validated_token, refusal_text):
    if not path.is_file():
        errors.append("missing " + path.name)
        return
    text = path.read_text(encoding="utf-8")
    for token in (
        "CadHandleService.NormalizeHexHandle(handle)",
        ensure_call,
        "ReferenceEquals(_document, Application.DocumentManager.MdiActiveDocument)",
        resolve_token,
        "StartOpenCloseTransaction()",
        "OpenMode.ForRead",
        "GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(",
        refusal_text,
        validated_token,
    ):
        if token not in text:
            errors.append(label + " exact-set ownership guard missing token: " + token)

    validate_start = text.find(validate_signature)
    validate_end = text.find("public static OwnershipIndex Build(", validate_start)
    if validate_start < 0 or validate_end <= validate_start:
        errors.append(label + " ownership guard must keep a dedicated complete-live-set validator")
        return

    validate = text[validate_start:validate_end]
    resolve = validate.find(resolve_token)
    native = validate.find("GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(")
    mark_validated = validate.find(validated_token)
    if resolve < 0 or native < 0 or mark_validated < 0 or not (resolve < native < mark_validated):
        errors.append(label + " ownership guard must resolve the complete set and verify native ownership before caching validation")
    if "ids.Count != expectedHandles.Count" not in validate:
        errors.append(label + " ownership guard must reject incomplete live-handle sets")


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

check_exact_set_guard(
    OWNERSHIP_GUARD,
    "shared rebar",
    "EnsureCompleteLiveSet(element, propertyKey, expectedOwner);",
    "private void EnsureCompleteLiveSet(",
    "CadHandleService.Resolve(_document, expectedHandles)",
    "_validatedLiveSets.Add(expectedOwner);",
    "Refusing destructive replacement before any rebar is erased.",
)

if OWNERSHIP_GUARD.is_file():
    text = OWNERSHIP_GUARD.read_text(encoding="utf-8")
    protected = private_static_method(text, "private static void AddProtected(string? handle, string token, Dictionary<string, string> owners)")
    for token in (
        "var canonical = CanonicalHandle(handle, token);",
        "if (!owners.ContainsKey(canonical)) owners[canonical] = token;",
    ):
        if token not in protected:
            errors.append("shared rebar protected-handle collection missing token: " + token)
    if "throw new InvalidOperationException" in protected:
        errors.append("shared rebar protected-handle collection must coalesce repeated protected references instead of treating them as destructive owners")

    owned = private_static_method(text, "private static void Add(ProjectElement element, string propertyKey, Dictionary<string, string> owners)")
    for token in (
        "owners.TryGetValue(canonical, out var existing)",
        "Generated rebar handle ownership conflict:",
        "owners[canonical] = token;",
    ):
        if token not in owned:
            errors.append("shared rebar owned-handle collection must retain protected/rebar and rebar/rebar conflict token: " + token)

check_exact_set_guard(
    TIE_OWNERSHIP_GUARD,
    "column tie",
    "EnsureCompleteLiveSet(element, expectedOwner);",
    "private void EnsureCompleteLiveSet(",
    "CadHandleService.Resolve(_document, expectedHandles)",
    "_validatedLiveSets.Add(expectedOwner);",
    "Refusing destructive replacement before any tie is erased.",
)

for name, contract in BUILDERS.items():
    owner_slot, guard_build, ensure_call = contract
    path = CAD / name
    if not path.is_file():
        errors.append("missing rebar builder: " + name)
        continue
    text = path.read_text(encoding="utf-8")
    if guard_build not in text:
        errors.append(name + " must build the expected strict ownership guard: " + guard_build)
    if "RequireMatchingOwnership(" not in text:
        errors.append(name + " must verify native ownership before destructive erase")
    if "solid.Erase();" in text:
        erase = text.find("solid.Erase();")
        require = text.rfind("RequireMatchingOwnership(", 0, erase)
        ensure_owned = text.rfind(ensure_call, 0, erase)
        if require < 0:
            errors.append(name + " erases generated Solid3d without a preceding native ownership check")
        if ensure_owned < 0:
            errors.append(name + " must route destructive replacement through its exact-set guard before erase: " + ensure_call)
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

print("PASS: all eight generated rebar replacement families are bound to their exact live-set ownership guard; project/element/owner-slot native ownership is required before destructive replacement, and dependent invalidation keeps its dedicated strict path.")
