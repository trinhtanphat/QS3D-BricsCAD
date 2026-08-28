#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/BulkEditService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BulkEditKnownCountEarlyDriftSmoke.cs"
BOUND_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BulkEditTargetInputBoundSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/bulk-edit-known-count-early-drift.md"


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, marker: str, label: str) -> int:
    index = text.find(marker)
    if index < 0:
        fail(f"missing {label}: {marker}")
    return index


for path, label in (
    (SOURCE, "BulkEditService production source"),
    (SMOKE, "Bulk edit known-Count smoke"),
    (BOUND_SMOKE, "Bulk edit streaming-bound smoke"),
    (RUNBOOK, "Bulk edit known-Count runbook"),
):
    if not path.is_file():
        fail(f"missing {label}: {path.relative_to(ROOT)}")

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
bound_smoke = BOUND_SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

helper = require(source, "private static void RequireCanObserveNext(", "early Count guard helper")
require(source[helper:], "observedCount >= knownCount.Value", "known-Count overrun predicate")
require(source[helper:], "input count changed during enumeration.", "count-drift diagnostic")

object_loop = require(source, "foreach (var element in elements)", "object-target traversal")
object_guard = require(source[object_loop:], "RequireCanObserveNext(knownCount, inputCount, \"Bulk edit target collection\");", "object-target early Count guard")
object_null = require(source[object_loop:], "if (element == null)", "object-target null validation")
if object_guard > object_null:
    fail("object-target Count drift must fail before unexpected target validation")

id_helper = require(source, "private static IReadOnlyList<string> MaterializeBounded", "target-id materializer")
id_guard = require(source[id_helper:], "RequireCanObserveNext(knownCount, inputCount, label);", "target-id early Count guard")
id_append = require(source[id_helper:], "result.Add(value);", "target-id append")
if id_guard > id_append:
    fail("target-id Count drift must fail before unexpected target materialization")

require(source, "RequireObservedCount(knownCount, inputCount, \"Bulk edit target collection\");", "object under-yield check")
require(source[id_helper:], "RequireObservedCount(knownCount, inputCount, label);", "id under-yield check")
require(source, "if (inputCount >= MaxTargetInputCount)", "independent streaming maximum")
require(source, "reports conflicting known input counts.", "known-Count conflict preflight")

for method in (
    "RejectObjectTargetOverrunBeforeNullValidation",
    "RejectIdTargetOverrunBeforeIdValidation",
    "RejectObjectTargetUnderYieldAfterTraversal",
    "RejectIdTargetUnderYieldAfterTraversal",
    "PreserveHonestCountedObjectTargets",
    "PreserveHonestCountedIdTargets",
):
    require(smoke, method, f"smoke regression {method}")

require(smoke, "DishonestCountCollection<T> : IReadOnlyCollection<T>", "counted traversal fixture")
require(smoke, "MoveNextCalls", "bounded traversal assertion")
require(bound_smoke, "LazyOversizeObjectTargetsStopAtMaxPlusOneReadOnly", "object streaming maximum regression")
require(bound_smoke, "LazyOversizeIdTargetsStopAtMaxPlusOneReadOnly", "id streaming maximum regression")
require(bound_smoke, "Equal(10001, observed);", "streaming max-plus-one traversal assertion")

for phrase in (
    "object targets",
    "target IDs",
    "10,000",
    "under-yield",
    "all-or-nothing",
    "no licensed BricsCAD runtime",
):
    require(runbook, phrase, f"runbook boundary {phrase}")

print("PASS: Bulk edit known-Count early drift guard")
