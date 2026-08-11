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
        "SolidOwnershipKind.Rebar",
        "SolidOwnershipKind.CurtainFrame",
        "GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(",
        "GeneratedCurtainFrameNativeOwnershipService.RequireMatchingOwnership(",
        "RequireNativeOwnership(solid, project, element, propertyKey, ownershipKind",
    ):
        if token not in text:
            errors.append("generated dependent invalidator missing native ownership token: " + token)
    erase = text.find("solid.Erase();")
    require = text.rfind("RequireNativeOwnership(solid, project, element, propertyKey, ownershipKind", 0, erase if erase >= 0 else len(text))
    if erase >= 0 and require < 0:
        errors.append("generated dependent invalidator erases Solid3d sets without native ownership proof")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: LINE/PATH curtain frames use dedicated project/element native ownership markers and generated-dependent invalidation preserves rebar/curtain native proof before destructive erase.")
