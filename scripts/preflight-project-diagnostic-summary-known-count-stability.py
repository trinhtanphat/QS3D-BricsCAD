#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/ProjectDiagnosticSummaryExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummaryKnownCountStabilitySmoke.cs"


def fail(message: str) -> None:
    print(f"ERROR: project diagnostic summary known-Count stability preflight failed: {message}")
    sys.exit(1)


def require(text: str, token: str, label: str) -> int:
    index = text.find(token)
    if index < 0:
        fail(f"missing {label}: {token}")
    return index


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

start = require(source, "private static List<ModelHealthIssue> MaterializeIssues", "MaterializeIssues")
end = require(source[start:], "private static int? ValidateKnownIssueCounts", "known-count helper") + start
region = source[start:end]

loop = require(region, "while (true)", "explicit traversal")
pre = require(region[loop:], "ValidateKnownIssueCounts(issues, expectedCount);", "pre-MoveNext Count rebound") + loop
move = require(region[pre:], "var hasNext = enumerator.MoveNext();", "MoveNext") + pre
post = require(region[move:], "ValidateKnownIssueCounts(issues, expectedCount);", "post-MoveNext Count rebound") + move
overrun = require(region[post:], "if (expectedCount.HasValue && result.Count >= expectedCount.Value)", "known-count over-yield admission") + post
current = require(region[overrun:], "var current = enumerator.Current;", "Current read") + overrun
after_current = require(region[current:], "ValidateKnownIssueCounts(issues, expectedCount);", "post-Current Count rebound") + current
add = require(region[after_current:], "result.Add(current);", "retention") + after_current
under = require(region[add:], "result.Count != expectedCount.Value", "under-yield rejection") + add

if not (loop < pre < move < post < overrun < current < after_current < add < under):
    fail("traversal ordering must be Count -> MoveNext -> Count -> over-yield -> Current -> Count -> retain -> under-yield")

for token, label in [
    ("ICollection<ModelHealthIssue>", "generic ICollection Count surface"),
    ("IReadOnlyCollection<ModelHealthIssue>", "read-only Count surface"),
    ("System.Collections.ICollection", "non-generic ICollection Count surface"),
    ("invalid negative count", "negative Count rejection"),
    ("conflicting known counts", "conflicting Count rejection"),
    ("Count changed during traversal", "transient Count drift rejection"),
    ("more items than its admitted known Count", "over-yield rejection"),
    ("fewer items than its admitted known Count", "under-yield rejection"),
]:
    require(source, token, label)

for test_name in [
    "CountDriftDuringMoveNextFailsClosed",
    "CountDriftDuringCurrentFailsClosed",
    "StableKnownCountOverYieldRejectsBeforeUnexpectedCurrent",
    "StableKnownCountUnderYieldFailsClosed",
    "StableCountedAndStreamingSourcesRemainAccepted",
]:
    require(smoke, test_name, f"smoke case {test_name}")

require(smoke, "Equal(0, source.CurrentReads", "pre-Current drift assertion")
require(smoke, "Equal(1, source.CurrentReads", "N+1 / Current drift assertion")

print("PASS project diagnostic summary known-Count stability source guard")
