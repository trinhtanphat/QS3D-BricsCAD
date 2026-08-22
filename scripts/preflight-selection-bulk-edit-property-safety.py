#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
selection_path = root / "src/QS3D.Core/Selection/SemanticSelectionBulkEditService.cs"
policy_path = root / "src/QS3D.Core/Services/SemanticPropertyEditPolicy.cs"
errors = []

if not selection_path.is_file():
    errors.append("missing SemanticSelectionBulkEditService.cs")
else:
    selection = selection_path.read_text(encoding="utf-8")
    if selection.count("SemanticPropertyEditPolicy.RequireEditablePropertyKey(propertyName)") < 2:
        errors.append("multi-selection set/multiply paths must delegate property canonicalization and fail-closed safety to SemanticPropertyEditPolicy")

if not policy_path.is_file():
    errors.append("missing SemanticPropertyEditPolicy.cs")
else:
    policy = policy_path.read_text(encoding="utf-8")
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
        if token not in policy:
            errors.append("shared bulk-edit fail-closed property boundary missing: " + token)

    unsafe = 'SourceDerivedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)\n        {\n            "LengthM",\n            "AreaM2",\n            "VolumeM3",\n            "PerimeterM",\n            "Layer"\n        };'
    if unsafe in policy:
        errors.append("shared source-derived key policy is still missing measured CAD metrics")

if errors:
    print("preflight-selection-bulk-edit-property-safety: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-selection-bulk-edit-property-safety: PASS")
print("Multi-selection generic property edits delegate to the shared policy and cannot overwrite CAD-derived metrics/provenance or semantic ID/ref-shaped relations.")
