#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedSlabMeshHealthSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedSlabMeshHealthSmokeRegistration.cs"
errors = []

for path in (SERVICE, SMOKE, REG):
    if not path.is_file():
        errors.append("missing slab mesh health source contract file: " + str(path.relative_to(ROOT)))

if SERVICE.is_file():
    text = SERVICE.read_text(encoding="utf-8")
    for token in (
        'private const string FootprintModeKey = "GeneratedSlabMeshFootprintMode";',
        'private const string RectangleFootprintMode = "RectangleLocalXY";',
        'private const string PolygonFootprintMode = "PolygonGlobalXY";',
        '"SLAB_MESH_FOOTPRINT_MODE_INVALID"',
        'if (element == null) continue;',
        'public HashSet<string> Conflicts',
        'if (Conflicts.Contains(handle)) return true;',
        'GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)',
    ):
        if token not in text:
            errors.append("GeneratedSlabMeshHealthService.cs missing fail-closed token: " + token)

    if 'if (!element.Properties.TryGetValue(FootprintModeKey' in text:
        errors.append("Slab footprint mode must remain optional for legacy rectangle metadata; missing key must not fail closed.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "AcceptsLegacyMissingFootprintMode();",
        "AcceptsRectangleFootprintMode();",
        "AcceptsPolygonFootprintMode();",
        "RejectsInvalidFootprintMode();",
        "DetectsLaterOwnershipConflict();",
        "IgnoresNullSemanticEntry();",
        '"SLAB_MESH_FOOTPRINT_MODE_INVALID"',
        '"SLAB_MESH_GENERATED_OWNERSHIP_CONFLICT"',
    ):
        if token not in text:
            errors.append("GeneratedSlabMeshHealthSmoke.cs missing regression token: " + token)

if REG.is_file() and "GeneratedSlabMeshHealthSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Slab mesh health smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Slab mesh Core health keeps legacy footprint compatibility while rejecting invalid modes, ownership ambiguity, and null-diagnostic crashes.")
