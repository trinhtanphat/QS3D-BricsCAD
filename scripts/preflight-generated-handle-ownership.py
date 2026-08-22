#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipHealthService.cs": [
        "GeneratedHandleOwnershipHealthService",
        "SafeGeneratedHandleOwnershipHealthService().Inspect(project)",
    ],
    "src/QS3D.Core/Diagnostics/SafeGeneratedHandleOwnershipHealthService.cs": [
        "SourceHandles",
        "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)",
        "GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(value)",
        "GeneratedHandleOwnershipPolicy.AreSameLogicalOwnerSlots(x.Slot, slot)",
        "GENERATED_HANDLE_OWNERSHIP_CONFLICT",
        "GroupBy(x => x.Value, StringComparer.OrdinalIgnoreCase)",
    ],
    "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs": [
        "public static class GeneratedHandleOwnershipPolicy",
        "EnumerateOwnerHandles",
        "CollectOwnerHandles",
        "TryFindOwner",
        "NormalizeHandleIdentity",
        "AreSameLogicalOwnerSlots",
        'StartsWith("Generated"',
        'EndsWith("Handle"',
        'EndsWith("Handles"',
        'PhysicalOpeningCutSolidHandle',
    ],
    "tests/QS3D.Core.SmokeTests/GeneratedHandleOwnershipHealthSmoke.cs": [
        "SourceAndGeneratedCollisionIsReported",
        "RebarAndCurtainCrossTypeCollisionIsReported",
        "DuplicateWithinSameSlotIsNotCrossOwnerConflict",
        "NonOwnerHandleMetadataIsIgnored",
        "GeneratedTieRebarHandles",
        "GeneratedCurtainFrameHandles",
        "PreviewHandle",
    ],
    "tests/QS3D.Core.SmokeTests/GeneratedHandleOwnershipHealthRegistration.cs": [
        "ModuleInitializer",
        "GeneratedHandleOwnershipHealthSmoke.Run();",
    ],
    "src/QS3D.BricsCAD.V25/GeneratedHandleOwnershipHealthCommands.cs": [
        'CommandMethod("QS3DHANDLEHEALTH"',
        "GeneratedHandleOwnershipHealthService().Inspect(project)",
        "ModelHealthWindow",
        "QS3DZOOMSELECTED",
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing generated-handle ownership file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing generated-handle ownership token: " + needle)

legacy_duplicate = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipHealthService.Safe.cs"
if legacy_duplicate.exists():
    errors.append("duplicate GeneratedHandleOwnershipHealthService.Safe.cs must not exist; SDK compile glob would declare the public service twice")

commands = []
adapter = ROOT / "src/QS3D.BricsCAD.V25"
if adapter.is_dir():
    for path in adapter.rglob("*.cs"):
        commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
if commands.count("QS3DHANDLEHEALTH") != 1:
    errors.append("QS3DHANDLEHEALTH must be declared exactly once")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: generated ownership health, selection and future generated families share normalized CAD identity and one Core owner-slot enumeration contract.")
