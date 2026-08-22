#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CAD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad"
SERVICE = CAD / "GeneratedCurtainFrameNativeOwnershipService.cs"
BUILDERS = (
    CAD / "CurtainWallFrameSolidBuilder.cs",
    CAD / "CurtainWallPathFrameSolidBuilder.cs",
)
INVALIDATOR = CAD / "GeneratedDependentGeometryInvalidator.cs"
errors = []

if not SERVICE.is_file():
    errors.append("missing GeneratedCurtainFrameNativeOwnershipService.cs")
else:
    text = SERVICE.read_text(encoding="utf-8")
    for token in (
        'private const string RegAppName = "QS3D_CURTAIN_FRAME";',
        'private const string OwnershipVersion = "1";',
        'private const string HandlesKey = "GeneratedCurtainFrameHandles";',
        "GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(HandlesKey)",
        "entity.GetXDataForApplication(RegAppName)",
    ):
        if token not in text:
            errors.append("curtain native ownership service missing token: " + token)

for path in BUILDERS:
    if not path.is_file():
        errors.append("missing curtain builder: " + path.name)
        continue
    text = path.read_text(encoding="utf-8")
    if "GeneratedCurtainFrameNativeOwnershipService.MarkGenerated(" not in text:
        errors.append(path.name + " must mark every newly generated curtain-frame solid")
    if "GeneratedCurtainFrameNativeOwnershipService.RequireMatchingOwnership(" not in text:
        errors.append(path.name + " must verify native ownership before destructive erase")
    erase = text.find("solid.Erase();")
    require = text.rfind("GeneratedCurtainFrameNativeOwnershipService.RequireMatchingOwnership(", 0, erase if erase >= 0 else len(text))
    if erase >= 0 and require < 0:
        errors.append(path.name + " erases a curtain-frame Solid3d without a preceding native ownership check")

if not INVALIDATOR.is_file():
    errors.append("missing GeneratedDependentGeometryInvalidator.cs")
else:
    text = INVALIDATOR.read_text(encoding="utf-8")
    for token in (
        "EnsureCurtainFrameSetLive(document, project, element, expected);",
        "EraseCurtainFrames(document, transaction, project, element, curtainOwnership);",
        "private static void EnsureCurtainFrameSetLive(",
        "private static void EraseCurtainFrames(",
        "GeneratedCurtainFrameNativeOwnershipService.RequireMatchingOwnership(",
    ):
        if token not in text:
            errors.append("generated invalidator missing curtain native-ownership token: " + token)

    validate_start = text.find("private static void EnsureCurtainFrameSetLive(")
    grid_start = text.find("private static void EnsureGridAnnotationsLive(", validate_start)
    if validate_start < 0 or grid_start <= validate_start:
        errors.append("generated invalidator must keep a dedicated curtain-frame prevalidation path")
    else:
        validate = text[validate_start:grid_start]
        if "GeneratedCurtainFrameNativeOwnershipService.RequireMatchingOwnership(" not in validate:
            errors.append("generated invalidator must verify native ownership during curtain-frame prevalidation")

    erase_start = text.find("private static void EraseCurtainFrames(")
    grid_erase_start = text.find("private static void EraseGridAnnotations(", erase_start)
    if erase_start < 0 or grid_erase_start <= erase_start:
        errors.append("generated invalidator must keep a dedicated curtain-frame erase path")
    else:
        erase = text[erase_start:grid_erase_start]
        require = erase.find("GeneratedCurtainFrameNativeOwnershipService.RequireMatchingOwnership(")
        destructive = erase.find("solid.Erase();")
        if require < 0 or destructive < 0 or require > destructive:
            errors.append("generated invalidator must verify matching native ownership before curtain-frame Solid3d erase")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: LINE/PATH curtain frames use dedicated project/element native ownership markers and dependent invalidation verifies native proof before destructive erase.")
