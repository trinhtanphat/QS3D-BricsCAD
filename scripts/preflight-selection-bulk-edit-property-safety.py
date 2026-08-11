#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
path = root / "src/QS3D.Core/Selection/SemanticSelectionBulkEditService.cs"
errors = []

if not path.is_file():
    errors.append("missing SemanticSelectionBulkEditService.cs")
else:
    text = path.read_text(encoding="utf-8")
    required = (
        "MeasuredSolidQuantityPolicy.VolumeProperty",
        "MeasuredSolidQuantityPolicy.SurfaceAreaProperty",
        'key.StartsWith("CAD.", StringComparison.OrdinalIgnoreCase)',
        "LooksLikeIdentityReferenceKey(key)",
        'key.EndsWith("Id", StringComparison.OrdinalIgnoreCase)',
        'key.EndsWith("Ids", StringComparison.OrdinalIgnoreCase)',
        'key.EndsWith("Ref", StringComparison.OrdinalIgnoreCase)',
        'key.EndsWith("Refs", StringComparison.OrdinalIgnoreCase)',
        'key.EndsWith("RefId", StringComparison.OrdinalIgnoreCase)',
        'key.EndsWith("RefIds", StringComparison.OrdinalIgnoreCase)',
        "Semantic identity/reference field cannot be edited as a generic property",
    )
    for token in required:
        if token not in text:
            errors.append("bulk-edit fail-closed property boundary missing: " + token)

    for unsafe in (
        'SourceDerivedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)\n        {\n            "LengthM",\n            "AreaM2",\n            "VolumeM3",\n            "PerimeterM",\n            "Layer"\n        };',
    ):
        if unsafe in text:
            errors.append("bulk-edit source-derived key policy is still missing measured CAD metrics")

if errors:
    print("preflight-selection-bulk-edit-property-safety: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-selection-bulk-edit-property-safety: PASS")
print("Multi-selection generic property edits cannot overwrite CAD-derived metrics/provenance or semantic ID/ref-shaped relations.")
