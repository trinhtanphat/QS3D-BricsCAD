#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "BcfIssueExchange.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "BcfTransientKnownCountSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

start = source.index("        internal static List<T> MaterializeBounded<T>(")
end = source.index("        private static int? ValidateKnownCounts<T>(", start)
region = source[start:end]

marker = "RequireStableKnownCounts"
if region.count(marker) < 2:
    raise SystemExit("BCF MaterializeBounded must rebind admitted Count before and after MoveNext.")

pre = region.index(marker)
move = region.index("enumerator.MoveNext()", pre)
post = region.index(marker, move)
current = region.index("enumerator.Current", post)
if not (pre < move < post < current):
    raise SystemExit("BCF Count rebound ordering must be rebound -> MoveNext -> rebound -> Current.")

required_smoke = (
    "TransientGrowthFailsBeforeCurrent",
    "TransientShrinkFailsBeforeCurrent",
    "TransientNegativeFailsBeforeCurrent",
    "TransientConflictFailsBeforeCurrent",
    "Equal(0, source.CurrentReads)",
    "StableCountedAndStreamingRemainAccepted",
)
for token in required_smoke:
    if token not in smoke:
        raise SystemExit("BCF transient Count smoke missing required assertion: " + token)

print("PASS BCF transient known-Count stability source guard")
