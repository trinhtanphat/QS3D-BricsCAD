#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/BcfIssueExchange.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BcfIssueExchangeKnownCountEarlyDriftSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/bcf-known-count-early-drift.md"


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, marker: str, label: str) -> int:
    index = text.find(marker)
    if index < 0:
        fail(f"missing {label}: {marker}")
    return index


if not SOURCE.is_file():
    fail(f"missing BCF production source: {SOURCE.relative_to(ROOT)}")
if not SMOKE.is_file():
    fail(f"missing BCF early-drift smoke: {SMOKE.relative_to(ROOT)}")
if not RUNBOOK.is_file():
    fail(f"missing BCF early-drift runbook: {RUNBOOK.relative_to(ROOT)}")

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

helper = require(source, "internal static List<T> MaterializeBounded<T>(", "shared bounded materializer")
overrun = require(source[helper:], "if (knownCount.HasValue && observedCount >= knownCount.Value)", "early known-Count overrun guard")
bound = require(source[helper:], "if (observedCount >= maximumCount)", "streaming maximum guard")
append = require(source[helper:], "items.Add(value);", "materialization append")
under_yield = require(source[helper:], "if (knownCount.HasValue && observedCount != knownCount.Value)", "post-traversal under-yield guard")

if not (overrun < bound < append < under_yield):
    fail("BCF materialization ordering must be known-Count overrun -> streaming bound -> append -> post-traversal cardinality check")

if "observedCount++;\n                if (observedCount > maximumCount)" in source[helper:]:
    fail("legacy post-increment BCF bound ordering reappeared")

for method in (
    "RejectTopicOverrunBeforeNullValidation",
    "RejectViewpointOverrunBeforeNullValidation",
    "RejectCommentOverrunBeforeNullValidation",
    "RejectComponentOverrunBeforeNullValidation",
    "RejectTopicUnderYieldAfterTraversal",
    "PreservePureStreamingTopics",
):
    require(smoke, method, f"smoke regression {method}")

require(smoke, "BCF collection Count does not match enumerated item count.", "exact Count-drift diagnostic assertion")
require(smoke, "RequireMoveNextCalls", "early traversal-stop assertion")

for phrase in (
    "topics",
    "viewpoints",
    "comments",
    "components",
    "PENDING_LOCAL",
    "no licensed BricsCAD runtime",
):
    require(runbook, phrase, f"runbook boundary {phrase}")

print("PASS: BCF known-Count early drift guard")
