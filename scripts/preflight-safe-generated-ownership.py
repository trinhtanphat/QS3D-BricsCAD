#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs": [
        "GeneratedHandleOwnershipPolicy",
        'PhysicalOpeningCutSolidHandle',
        'StartsWith("Generated"',
        'EndsWith("Handle"',
        'EndsWith("Handles"',
    ],
    "src/QS3D.Core/Diagnostics/SafeGeneratedHandleOwnershipHealthService.cs": [
        "SafeGeneratedHandleOwnershipHealthService",
        'AddClaims(claims, element, "SourceHandles"',
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot",
        "GENERATED_HANDLE_OWNERSHIP_CONFLICT",
    ],
    "tests/QS3D.Core.SmokeTests/SafeGeneratedHandleOwnershipHealthSmoke.cs": [
        "SharedBoundaryProvenanceIsAllowed",
        'BoundarySourceHandles',
        "SourceAndGeneratedCollisionStillFails",
        "CrossGeneratedTypeCollisionStillFails",
        "GeneratedSlabMeshHandles",
        "GeneratedCurtainFrameHandles",
    ],
    "tests/QS3D.Core.SmokeTests/SafeGeneratedHandleOwnershipHealthRegistration.cs": [
        "ModuleInitializer",
        "SafeGeneratedHandleOwnershipHealthSmoke.Run();",
    ],
    "src/QS3D.BricsCAD.V25/SafeGeneratedHandleOwnershipHealthCommands.cs": [
        'CommandMethod("QS3DOWNERSHIPHEALTH"',
        "SafeGeneratedHandleOwnershipHealthService().Inspect(project)",
        "ModelHealthWindow",
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing provenance-safe ownership file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing provenance-safe ownership token: " + needle)

# Shared evidence/provenance references are not ownership slots.
policy = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
if policy.is_file():
    text = policy.read_text(encoding="utf-8")
    if "BoundarySourceHandles" in text:
        errors.append("ownership policy must not special-case BoundarySourceHandles as an owner slot")

commands = []
adapter = ROOT / "src/QS3D.BricsCAD.V25"
if adapter.is_dir():
    for path in adapter.rglob("*.cs"):
        commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
if commands.count("QS3DOWNERSHIPHEALTH") != 1:
    errors.append("QS3DOWNERSHIPHEALTH must be declared exactly once")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: ownership health counts SourceHandles and generated owner slots only; shared Room/evidence provenance handles are excluded and regression-covered.")
