#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RATE_BOOK = ROOT / "src/QS3D.Core/Cost/RateBook.cs"
PROJECTION = ROOT / "src/QS3D.Core/Cost/FrozenEstimateProjection.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CostEvidenceKnownCountNoOverreadSmoke.cs"


def fail(message: str) -> None:
    print("FAIL cost evidence known-count no-overread: " + message)
    sys.exit(1)


def require_order(text: str, tokens, label: str) -> None:
    cursor = -1
    for token in tokens:
        index = text.find(token, cursor + 1)
        if index < 0:
            fail(label + " missing token: " + token)
        if index <= cursor:
            fail(label + " ordering violated at: " + token)
        cursor = index

rate = RATE_BOOK.read_text(encoding="utf-8")
projection = PROJECTION.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

require_order(rate, [
    "while (true)",
    "RequireStableKnownCount(items, knownCount);",
    "if (!enumerator.MoveNext())",
    "RequireStableKnownCount(items, knownCount);",
    "if (hasKnownCount && index >= knownCount)",
    "if (index >= MaxItems)",
    "var item = enumerator.Current;",
], "RateBook traversal")
require_order(rate, [
    "if (hasKnownCount && index != knownCount)",
    "RequireStableKnownCount(items, knownCount);",
    "foreach (var pair in _byScope)",
], "RateBook Count rebind")
if "while (enumerator.MoveNext())" in rate:
    fail("RateBook traversal regressed to the stale while(MoveNext) shape")

require_order(projection, [
    "while (enumerator.MoveNext())",
    "if (hasKnownCount && index >= knownCount)",
    "if (index >= MaxLines)",
    "var line = enumerator.Current;",
], "Frozen projection traversal")
require_order(projection, [
    "if (hasKnownCount && rows.Count != knownCount)",
    "RequireStableKnownCount(lines, knownCount);",
], "Frozen projection Count rebind")

for token in [
    "RateBookOverrunRejectsBeforeSecondCurrent",
    "RateBookStreamingCeilingRejectsBeforeOverflowCurrent",
    "FrozenProjectionOverrunRejectsBeforeSecondCurrent",
    "FrozenProjectionStreamingCeilingRejectsBeforeOverflowCurrent",
    "UnderYieldRejectsOnBothSurfaces",
    "CountDriftRejectsOnBothSurfaces",
    "ConflictingAndNegativeCountsRejectBeforeTraversal",
    "NullAndDuplicateEvidenceRemainRejected",
    "HonestCountedEvidenceRemainsAccepted",
    "SequencedCountCollection",
    "DualCountCollection",
    "Equal(1, source.CurrentReads",
    "Equal(10000, source.CurrentReads",
    "[ModuleInitializer]",
]:
    if token not in smoke:
        fail("required deterministic smoke evidence is missing: " + token)

print("PASS cost evidence known-count no-overread source guard")
