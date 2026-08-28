#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Documentation/SemanticSheetIndexBuilder.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticSheetIndexKnownCountStabilitySmoke.cs"


def fail(message: str) -> None:
    print("FAIL semantic sheet index known-count stability: " + message)
    sys.exit(1)


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

start = source.find("private static List<SemanticSheetPlan> MaterializeBounded")
end = source.find("private static int? RequireKnownCountsWithinLimit", start)
if start < 0 or end < 0:
    fail("MaterializeBounded source boundary is missing")
method = source[start:end]

required_source = [
    "var knownCount = RequireKnownCountsWithinLimit(sheets);",
    "while (enumerator.MoveNext())",
    "if (knownCount.HasValue && result.Count >= knownCount.Value)",
    "if (result.Count >= MaxSheets)",
    "var sheet = enumerator.Current;",
    "if (knownCount.HasValue && result.Count != knownCount.Value)",
    "var postTraversalKnownCount = RequireKnownCountsWithinLimit(sheets);",
    "known count changed during traversal",
]
for token in required_source:
    if token not in method:
        fail("required source invariant is missing: " + token)

known_guard = method.index("if (knownCount.HasValue && result.Count >= knownCount.Value)")
stream_guard = method.index("if (result.Count >= MaxSheets)")
current_read = method.index("var sheet = enumerator.Current;")
if not (known_guard < current_read and stream_guard < current_read):
    fail("known-count and streaming ceiling guards must execute before Current")

post_rebind = method.index("var postTraversalKnownCount = RequireKnownCountsWithinLimit(sheets);")
if post_rebind < current_read:
    fail("post-traversal Count evidence must be rebound only after traversal")

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
    "Equal(1, source.CurrentReads",
    "[ModuleInitializer]",
]
for token in required_smoke:
    if token not in smoke:
        fail("required deterministic smoke evidence is missing: " + token)

print("PASS semantic sheet index known-count stability source guard")
