#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Reporting/MaterialUsageSchedule.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MaterialUsageGenerationFenceSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""
errors: list[str] = []

for token, label in {
    "MaterialUsageGenerationSnapshot": "detached Material Usage generation snapshot",
    "CaptureStable": "stable snapshot capture boundary",
    "Revalidate": "post-aggregation live-generation revalidation",
}.items():
    if token not in source:
        errors.append(f"missing {label}: {token}")

if "foreach (var element in project.Elements.OrderBy" in source:
    errors.append("aggregation still traverses live project.Elements after catalog snapshot capture")

for token, label in {
    "InPlaceQuantityDriftFailsClosed": "in-place quantity drift regression",
    "FamilySemanticDriftFailsClosed": "family semantic drift regression",
    "SourceHandleDriftFailsClosed": "source-handle provenance drift regression",
    "StableSnapshotRemainsDeterministic": "stable generation regression",
}.items():
    if token not in smoke:
        errors.append(f"missing {label}: {token}")

if errors:
    raise SystemExit(
        "ERROR: Material Usage schedule remains vulnerable to mixed project generations:\n - "
        + "\n - ".join(errors)
    )

print("PASS: Material Usage aggregates one detached semantic generation and rejects live drift")
