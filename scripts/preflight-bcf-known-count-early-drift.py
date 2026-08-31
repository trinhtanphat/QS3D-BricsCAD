#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/BcfIssueExchange.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BcfIssueExchangeKnownCountEarlyDriftSmoke.cs"
LEGACY_BOUND_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BcfIssueExchangeCollectionBoundSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/bcf-known-count-early-drift.md"


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, marker: str, label: str) -> int:
    index = text.find(marker)
    if index < 0:
        fail(f"missing {label}: {marker}")
    return index


for path, label in (
    (SOURCE, "BCF production source"),
    (SMOKE, "BCF early-drift smoke"),
    (LEGACY_BOUND_SMOKE, "existing BCF streaming-bound smoke"),
    (RUNBOOK, "BCF early-drift runbook"),
):
    if not path.is_file():
        fail(f"missing {label}: {path.relative_to(ROOT)}")

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
legacy_bound_smoke = LEGACY_BOUND_SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

helper = require(source, "internal static List<T> MaterializeBounded<T>(", "shared bounded materializer")
materializer = source[helper:]
corroboration = require(materializer, "out var corroboratedKnownCount", "known-Count corroboration output")
overrun = require(materializer, "if (corroboratedKnownCount && knownCount.HasValue && observedCount >= knownCount.Value)", "early corroborated known-Count overrun guard")
bound = require(materializer, "if (observedCount >= maximumCount)", "streaming maximum guard")
append = require(materializer, "items.Add(value);", "materialization append")
under_yield = require(materializer, "if (knownCount.HasValue && observedCount != knownCount.Value)", "post-traversal under-yield guard")

if "out int knownCountSources" in materializer:
    # The post-traversal Count-stability extension exposes the number of deterministic
    # Count surfaces to its caller. Preserve #4349's evidence accounting while allowing
    # the implementation-local counter to have a distinct name from the out parameter.
    source_count = require(materializer, "var observedKnownCountSources = 0;", "known-Count evidence source counter")
    source_count_assignment = require(materializer, "knownCountSources = observedKnownCountSources;", "known-Count evidence source output")
    corroborated_assignment = require(materializer, "corroboratedKnownCount = observedKnownCountSources > 1;", "corroborated known-Count assignment")
    if not (
        corroboration < overrun < bound < append < under_yield < source_count <
        source_count_assignment < corroborated_assignment
    ):
        fail("BCF materialization ordering must preserve corroborated Count guard, streaming bound, append, under-yield check, and extended evidence accounting")
else:
    source_count = require(materializer, "var knownCountSources = 0;", "known-Count evidence source counter")
    corroborated_assignment = require(materializer, "corroboratedKnownCount = knownCountSources > 1;", "corroborated known-Count assignment")
    if not (corroboration < overrun < bound < append < under_yield < source_count < corroborated_assignment):
        fail("BCF materialization ordering must preserve corroborated Count guard, streaming bound, append, under-yield check, and evidence accounting")

if "observedCount++;\n                if (observedCount > maximumCount)" in materializer:
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

require(smoke, "CorroboratedDishonestCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection", "corroborated test collection")
require(smoke, "BCF collection Count does not match enumerated item count.", "exact Count-drift diagnostic assertion")
require(smoke, "RequireMoveNextCalls", "early traversal-stop assertion")

# Preserve the pre-existing contract that a single IReadOnlyCollection Count witness
# cannot suppress the independent package streaming cap.
require(legacy_bound_smoke, "DishonestCountCollection<T> : IReadOnlyCollection<T>", "single-source dishonest Count regression")
require(legacy_bound_smoke, "BCF topic streaming bound did not stop on item 257.", "topic streaming-bound regression")
require(legacy_bound_smoke, "BCF comment streaming bound did not stop on item 1025.", "comment streaming-bound regression")
require(legacy_bound_smoke, "BCF component streaming bound did not stop on item 1001.", "component streaming-bound regression")

for phrase in (
    "topics",
    "viewpoints",
    "comments",
    "components",
    "corroborated",
    "single-interface",
    "PENDING_LOCAL",
    "no licensed BricsCAD runtime",
):
    require(runbook, phrase, f"runbook boundary {phrase}")

print("PASS: BCF corroborated known-Count early drift guard")
