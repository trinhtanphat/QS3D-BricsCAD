#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs": [
        "public static class GeneratedHandleOwnershipPolicy",
        "RebarHandleKeys",
        "IsRebarOwnerSlot",
        "GeneratedFoundationMeshHandles",
        "PhysicalOpeningCutSolidHandle",
        "CanonicalOwnerSlot",
        "AreSameLogicalOwnerSlots",
        "EnumerateLogicalOwnerHandles",
    ],
    "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs": [
        "GeneratedHandleOwnershipPolicy.RebarHandleKeys",
        "REBAR_GENERATED_CROSS_KEY_OWNERSHIP_CONFLICT",
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs": [
        "CoreOwnershipPolicy.RebarHandleKeys",
        "CoreOwnershipPolicy.IsOwnerSlot",
        "CoreOwnershipPolicy.IsRebarOwnerSlot",
        "CoreOwnershipPolicy.CanonicalOwnerSlot",
        "Refusing destructive erase",
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedTieRebarOwnershipGuard.cs": [
        "CoreOwnershipPolicy.RebarHandleKeys",
        "CoreOwnershipPolicy.IsOwnerSlot",
        "CoreOwnershipPolicy.IsRebarOwnerSlot",
        "CoreOwnershipPolicy.CanonicalOwnerSlot",
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainFrameOwnershipGuard.cs": [
        "CoreOwnershipPolicy.IsOwnerSlot",
        "CoreOwnershipPolicy.CanonicalOwnerSlot",
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs": [
        "CoreOwnershipPolicy.RebarHandleKeys",
        "MetadataPrefixForHandleKey",
        "RemoveByPrefix",
        "GeneratedCurtainFrame",
    ],
    "src/QS3D.BricsCAD.V25/GeneratedHandleOwnershipPolicy.cs": [
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.RebarHandleKeys",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.IsOwnerSlot",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles",
    ],
    "src/QS3D.BricsCAD.V25/HealthAllCommands.cs": [
        "OwnerSlotHandles",
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot",
        'normalized.Contains("GENERATED_HANDLE_OWNERSHIP")',
    ],
    "tests/QS3D.Core.SmokeTests/GeneratedOwnerSlotPolicySmoke.cs": [
        "GeneratedOwnerSlotPolicySmoke",
        "GeneratedFoundationMeshHandles",
        "GeneratedCurtainFrameHandles",
        "PreviewHandle",
        "PhysicalOpeningCutSolidHandle",
        "CanonicalOwnerSlot",
        "EnumerateLogicalOwnerHandles",
        "ModuleInitializer",
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing generated ownership policy file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing owner-slot policy token: " + needle)

for relative in (
    "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs",
):
    path = ROOT / relative
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    if "private static readonly string[] RebarHandleKeys" in text or "private static readonly string[] Keys" in text:
        errors.append(relative + " must not own a private generated-rebar slot list")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: generated owner slots use one Core policy across health, destructive guards, invalidation and Health All locate; logical host/opening aliases canonicalize without hiding different generated families.")
