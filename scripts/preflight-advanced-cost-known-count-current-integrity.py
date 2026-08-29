#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Cost" / "AdvancedCostManagement.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "AdvancedCostKnownCountCurrentIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

caller_loops = (
    ("components", "component", "Rate build-up component collection"),
    ("records", "record", "Historical cost catalog"),
    ("lines", "line", "Tender quote line collection"),
    ("requirements", "requirement", "Tender requirement collection"),
    ("bids", "bid", "Tender bid collection"),
    ("contractItems", "item", "Progress contract item collection"),
    ("claimLines", "line", "Progress claim line collection"),
)

failures = []
for collection, item, label in caller_loops:
    old = f"foreach (var {item} in {collection})"
    if old in source:
        failures.append(f"caller-controlled foreach remains for {label}: {old}")
        continue

    get_enumerator = f"{collection}.GetEnumerator()"
    current = ".Current;"
    label_pos = source.find(f'"{label}"')
    if label_pos < 0:
        failures.append(f"missing collection label: {label}")
        continue

    window_start = max(0, source.rfind("{", 0, label_pos) - 2500)
    window_end = min(len(source), label_pos + 3500)
    window = source[window_start:window_end]
    positions = [
        window.find(get_enumerator),
        window.find("MoveNext()"),
        window.find("AdvancedCostCollectionContract.RequireCanProcessNext("),
        window.find(current),
    ]
    if any(pos < 0 for pos in positions):
        failures.append(f"missing explicit traversal token for {label}: {positions}")
    elif positions != sorted(positions):
        failures.append(f"wrong traversal order for {label}: {positions}")

required_smoke = (
    "CountedCurrentProbe<CostResourceComponent>",
    "CountedCurrentProbe<HistoricalCostRecord>",
    "MoveNextCalls",
    "CurrentReads",
    "Equal(2, source.MoveNextCalls",
    "Equal(1, source.CurrentReads",
)
for token in required_smoke:
    if token not in smoke:
        failures.append(f"smoke missing token: {token}")

if failures:
    for failure in failures:
        print(f"FAIL advanced-cost known-Count Current integrity: {failure}", file=sys.stderr)
    raise SystemExit(1)

print("PASS advanced-cost known-Count Current integrity")
