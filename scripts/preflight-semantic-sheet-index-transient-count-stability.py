#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Documentation/SemanticSheetIndexBuilder.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticSheetIndexTransientCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/semantic-sheet-index-transient-count-stability.md"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

start = source.index("private static List<SemanticSheetPlan> MaterializeBounded")
end = source.index("private static void RequireStableKnownCount", start)
method = source[start:end]
pre = method.index("RequireStableKnownCount(sheets, knownCount);")
move = method.index("if (!enumerator.MoveNext())", pre)
post = method.index("RequireStableKnownCount(sheets, knownCount);", pre + 1)
current = method.index("var sheet = enumerator.Current;", post)
if not pre < move < post < current:
    raise SystemExit("Semantic sheet transient Count checks must straddle MoveNext and precede Current")
if "while (enumerator.MoveNext())" in method:
    raise SystemExit("Semantic sheet caller-controlled traversal must retain pre-MoveNext Count stability checks")

stable_start = source.index("private static void RequireStableKnownCount")
stable_end = source.index("private static int? RequireKnownCountsWithinLimit", stable_start)
stable = source[stable_start:stable_end]
for token in [
    "var currentKnownCount = RequireKnownCountsWithinLimit(sheets);",
    "if (knownCount != currentKnownCount)",
    "known count changed during traversal",
]:
    if token not in stable:
        raise SystemExit("Semantic sheet Count stability helper missing token: " + token)

for token in [
    "[ModuleInitializer]",
    "RejectGrowthAfterMoveNextBeforeCurrent",
    "RejectNegativeAfterMoveNextBeforeCurrent",
    "RejectShrinkBeforeNextMoveNext",
    "Equal(0, source.CurrentReads",
    "Equal(1, source.MoveNextCalls",
    "Equal(1, source.CurrentReads",
]:
    if token not in smoke:
        raise SystemExit("Semantic sheet transient Count smoke missing token: " + token)

if not RUNBOOK.exists():
    raise SystemExit("Semantic sheet transient Count runbook is missing")

print("PASS semantic sheet index transient Count stability source guard")
