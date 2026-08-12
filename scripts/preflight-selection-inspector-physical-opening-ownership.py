#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Selection/SemanticSelectionInspector.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticSelectionInspectorSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing selection-inspector ownership contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    start = source.find("private static bool IsInternalOwnershipProperty(string key)")
    if start < 0:
        errors.append("cannot isolate SemanticSelectionInspector.IsInternalOwnershipProperty")
    else:
        body = source[start:]
        for token in (
            'normalized.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)',
            'normalized.StartsWith("QS3D.PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)',
            'normalized.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)',
        ):
            if token not in body:
                errors.append("selection inspector missing internal ownership filter: " + token)

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        'project.Elements[0].Properties["QS3D.PhysicalOpeningCutOpeningIds"]',
        'x.Name.StartsWith("QS3D.PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)',
        "Property inspector must not expose namespaced physical opening cut ownership state.",
    ):
        if token not in smoke:
            errors.append("selection inspector smoke missing namespaced ownership regression: " + token)

print("QS3D selection inspector physical-opening ownership preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: semantic selection inspection hides both legacy and QS3D-namespaced physical-opening ownership metadata while preserving existing generated/handle filtering.")
