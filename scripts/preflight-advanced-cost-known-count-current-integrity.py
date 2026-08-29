#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Cost" / "AdvancedCostManagement.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "AdvancedCostKnownCountCurrentIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

caller_loops = (
    ("components", "component", "componentEnumerator", "Rate build-up component collection"),
    ("records", "record", "recordEnumerator", "Historical cost catalog"),
    ("lines", "line", "lineEnumerator", "Tender quote line collection"),
    ("requirements", "requirement", "requirementEnumerator", "Tender requirement collection"),
    ("bids", "bid", "bidEnumerator", "Tender bid collection"),
    ("contractItems", "item", "contractEnumerator", "Progress contract item collection"),
    ("claimLines", "line", "claimEnumerator", "Progress claim line collection"),
)

failures = []
for collection, item, enumerator, label in caller_loops:
    old = f"foreach (var {item} in {collection})"
    if old in source:
        failures.append(f"caller-controlled foreach remains for {label}: {old}")
        continue

    start_token = f"using (var {enumerator} = {collection}.GetEnumerator())"
    start = source.find(start_token)
    if start < 0:
        failures.append(f"missing explicit enumerator for {label}: {start_token}")
        continue

    window = source[start:start + 1800]
    positions = [
        window.find(f"{enumerator}.MoveNext()"),
        window.find("AdvancedCostCollectionContract.RequireCanProcessNext("),
        window.find(f"var {item} = {enumerator}.Current;"),
    ]
    if any(pos < 0 for pos in positions):
        failures.append(f"missing explicit traversal token for {label}: {positions}")
    elif positions != sorted(positions):
        failures.append(f"wrong MoveNext/guard/Current order for {label}: {positions}")

    if f'"{label}"' not in window:
        failures.append(f"explicit traversal window lost collection label: {label}")

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
