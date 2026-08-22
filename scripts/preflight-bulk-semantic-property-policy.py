#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
POLICY = ROOT / "src/QS3D.Core/Services/SemanticPropertyEditPolicy.cs"
BULK = ROOT / "src/QS3D.Core/Services/BulkEditService.cs"
SELECTION = ROOT / "src/QS3D.Core/Selection/SemanticSelectionBulkEditService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BulkEditAtomicitySmoke.cs"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing semantic bulk property policy file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


policy = read(POLICY)
bulk = read(BULK)
selection = read(SELECTION)
smoke = read(SMOKE)

for token in (
    "ReservedIdentityKeys",
    '"FamilyId"',
    '"FloorId"',
    '"ZoneId"',
    'key.EndsWith("Id", StringComparison.OrdinalIgnoreCase)',
    'key.EndsWith("RefId", StringComparison.OrdinalIgnoreCase)',
    'key.IndexOf("Handle", StringComparison.OrdinalIgnoreCase)',
    'key.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)',
    "Semantic identity/reference field cannot be edited as a generic property",
):
    if token not in policy:
        errors.append("semantic property policy missing token: " + token)

call = "SemanticPropertyEditPolicy.RequireEditablePropertyKey(propertyName)"
if bulk.count(call) < 2:
    errors.append("BulkEditService must apply the shared policy to SetProperty and MultiplyNumericProperty.")
if selection.count(call) < 2:
    errors.append("SemanticSelectionBulkEditService must apply the shared policy to SetProperty and MultiplyNumericProperty.")

for token in (
    "GenericSemanticReferencesFailClosed",
    "ProjectFloorService.BottomLevelIdKey",
    '"HostRefId"',
    "Generic bulk property edit bypassed the Level relation service.",
    "Rejected generic semantic-reference edits touched project persistence state.",
):
    if token not in smoke:
        errors.append("bulk semantic-reference smoke missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: generic bulk property edits share one fail-closed policy for semantic identity, relation, source-derived and native/generated ownership fields.")
