#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Documentation/SemanticSheetIndexBuilder.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticSheetIndexKnownCountStabilitySmoke.cs"
TRANSIENT_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticSheetIndexTransientCountStabilitySmoke.cs"


def fail(message: str) -> None:
    print("FAIL semantic sheet index known-count stability: " + message)
    sys.exit(1)


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
transient_smoke = TRANSIENT_SMOKE.read_text(encoding="utf-8")

start = source.find("private static List<SemanticSheetPlan> MaterializeBounded")
end = source.find("private static void RequireStableKnownCount", start)
if start < 0 or end < 0:
    fail("MaterializeBounded source boundary is missing")
method = source[start:end]

required_source = [
    "var knownCount = RequireKnownCountsWithinLimit(sheets);",
    "while (true)",
    "RequireStableKnownCount(sheets, knownCount);",
    "if (!enumerator.MoveNext())",
    "if (knownCount.HasValue && result.Count >= knownCount.Value)",
    "if (result.Count >= MaxSheets)",
    "var sheet = enumerator.Current;",
    "if (knownCount.HasValue && result.Count != knownCount.Value)",
]
for token in required_source:
    if token not in method:
        fail("required source invariant is missing: " + token)

pre = method.index("RequireStableKnownCount(sheets, knownCount);")
move = method.index("if (!enumerator.MoveNext())", pre)
post = method.index("RequireStableKnownCount(sheets, knownCount);", pre + 1)
known_guard = method.index("if (knownCount.HasValue && result.Count >= knownCount.Value)", post)
stream_guard = method.index("if (result.Count >= MaxSheets)", known_guard)
current_read = method.index("var sheet = enumerator.Current;", stream_guard)
if not (pre < move < post < known_guard < stream_guard < current_read):
    fail("Count stability and capacity guards must straddle MoveNext and execute before Current")

if "while (enumerator.MoveNext())" in method:
    fail("caller-controlled semantic sheet traversal must not regress to while(MoveNext())")

required_smoke = [
    "KnownCountOverrunRejectsBeforeSecondCurrent",
    "KnownCountUnderYieldStillFailsClosed",
    "PostTraversalCountDriftFailsClosed",
    "ConflictingCountSurfacesFailBeforeTraversal",
    "NegativeCountFailsBeforeTraversal",
    "StreamingCeilingRejectsBeforeOverflowCurrent",
    "NullSheetStillFailsClosed",
    "HonestCountedInputRemainsSortedAndAccepted",
    "DuplicateNumbersRemainRejected",
    "Equal(5, source.CountReads",
    "Equal(7, source.CountReads",
    "[ModuleInitializer]",
]
for token in required_smoke:
    if token not in smoke:
        fail("required deterministic smoke evidence is missing: " + token)

for token in [
    "RejectGrowthAfterMoveNextBeforeCurrent",
    "RejectNegativeAfterMoveNextBeforeCurrent",
    "RejectShrinkBeforeNextMoveNext",
    "Equal(0, source.CurrentReads",
    "Equal(1, source.MoveNextCalls",
]:
    if token not in transient_smoke:
        fail("required transient smoke evidence is missing: " + token)

print("PASS semantic sheet index known-count stability source guard")
