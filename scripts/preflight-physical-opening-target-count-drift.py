#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/PhysicalOpeningCutTargetKnownCountSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "using (var enumerator = source.GetEnumerator())",
    'RequireKnownCountStable(source, knownCount, "before MoveNext")',
    "var hasNext = enumerator.MoveNext();",
    'RequireKnownCountStable(source, knownCount, "after MoveNext")',
    "if (knownCount.HasValue && observedCount >= knownCount.Value)",
    "var raw = enumerator.Current;",
    'RequireKnownCountStable(source, knownCount, "after Current")',
    'RequireKnownCountStable(source, knownCount, "after traversal")',
    "RequireObservedCount(knownCount, observedCount);",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"FAIL physical-opening Count-drift preflight: missing source contract: {token}")

ordered = [source.index(token) for token in required_source[:8]]
if ordered != sorted(ordered):
    raise SystemExit("FAIL physical-opening Count-drift preflight: traversal/Count rebound ordering regressed")

normalize_start = source.index("public static IReadOnlyList<string> Normalize")
normalize_end = source.index("private static int? GetKnownCount", normalize_start)
normalize = source[normalize_start:normalize_end]
if "foreach (var raw in source)" in normalize:
    raise SystemExit("FAIL physical-opening Count-drift preflight: legacy foreach traversal returned")

required_smoke = [
    "TransientCountDriftFailsAtCallerBoundaries();",
    "moveNextDrift.CurrentReads != 0",
    "currentDrift.CurrentReads != 1",
    "KnownOverYieldStopsBeforeUnexpectedCurrent();",
    "source.MoveNextCalls != 2 || source.CurrentReads != 1",
    "PureStreamingStillStopsAtBoundary();",
    "source.MoveNextCalls != MaxOpeningIds + 1 || source.CurrentReads != MaxOpeningIds",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"FAIL physical-opening Count-drift preflight: missing deterministic smoke evidence: {token}")

print("PASS physical-opening target Count drift integrity")
