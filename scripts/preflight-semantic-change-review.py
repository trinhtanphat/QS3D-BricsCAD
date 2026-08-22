#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Revisions/SemanticChangeReview.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticChangeReviewSmoke.cs"
REVISION = ROOT / "src/QS3D.Core/Revisions/RevisionService.cs"
QUANTITY = ROOT / "src/QS3D.Core/Revisions/QuantityRevisionReport.cs"
PORTABILITY = ROOT / "src/QS3D.Core/Export/ProjectInterchangeElementPropertyPolicy.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing semantic change review file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
smoke = read(SMOKE)
revision = read(REVISION)
quantity = read(QUANTITY)
portability = read(PORTABILITY)

for token in (
    "public sealed class SemanticChangeReviewBuilder",
    "new RevisionService().Compare(before, after)",
    "SemanticChangeFieldKind.Identity",
    "SemanticChangeFieldKind.Property",
    "SemanticChangeFieldKind.Quantity",
    'private const string SourceHandlesField = "SourceHandles"',
    'private const string PropertyFieldPrefix = "Property:"',
    "IsPortableReviewField(field.Field)",
    "ProjectInterchangeElementPropertyPolicy.IsPortable(propertyKey)",
    "omittedSourceReferences++",
    "OmittedSourceReferenceChangeCount",
    "OrderBy(x => ChangeRank(x.Change))",
    "ThenBy(x => x.Category, StringComparer.OrdinalIgnoreCase)",
    "ThenBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)",
    "non-canonical padded element id",
    "duplicate element id",
):
    if token not in source:
        errors.append("semantic change review source missing contract token: " + token)

for forbidden in (
    "Bricscad.",
    "Teigha.",
    "Autodesk.",
    "QuantityRevisionRow",
    "QuantityRevisionSummary",
    "QuantityRevisionReport().Build",
):
    if forbidden in source:
        errors.append("semantic change review must remain Core presentation-only and not duplicate quantity/native authority: " + forbidden)

for token in (
    "GeneratedHandleOwnershipPolicy.IsOwnerSlot(normalized)",
    'normalized.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)',
    'normalized.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)',
    'normalized.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)',
    'normalized.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) < 0',
):
    if token not in portability:
        errors.append("shared interchange/review property portability policy drifted: " + token)

for token in (
    "GroupsStableSemanticChangesWithoutHandleAuthority",
    "ReviewOrderingIsDeterministic",
    "MalformedSnapshotsFailClosed",
    'a.Properties["GeneratedSolidHandle"]',
    'a.Properties["BoundarySourceHandles"]',
    'a.Properties["QS3D.GeneratedSolid.StaleSnapshot"]',
    'a.Properties["PhysicalOpeningCutHostHandle"]',
    "Equal(5, item.OmittedSourceReferenceChangeCount)",
    'x.Field.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) < 0',
    'x.Field.IndexOf("Generated", StringComparison.OrdinalIgnoreCase) < 0',
    'x.Field.IndexOf("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase) < 0',
    "SemanticChangeFieldKind.Identity",
    "SemanticChangeFieldKind.Property",
    "SemanticChangeFieldKind.Quantity",
):
    if token not in smoke:
        errors.append("semantic change review smoke missing regression token: " + token)

if "public IReadOnlyList<RevisionDelta> Compare" not in revision:
    errors.append("RevisionService.Compare authority is missing")
if "public sealed class QuantityRevisionReport" not in quantity:
    errors.append("existing QuantityRevisionReport authority is missing")

print("QS3D semantic change review preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: semantic change review groups RevisionService deltas by stable semantic ID and reuses the Interchange property portability policy so SourceHandles and drawing-local generated/handle-bearing properties stay out of portable review content.")
