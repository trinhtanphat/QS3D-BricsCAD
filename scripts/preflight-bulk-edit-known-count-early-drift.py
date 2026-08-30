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


def require(text: str, marker: str, label: str, start: int = 0) -> int:
    index = text.find(marker, start)
    if index < 0:
        fail(f"missing {label}: {marker}")
    return index


def require_success_path(segment: str, rebound: str, guard: str, current: str, validation: str, label: str) -> None:
    cursor = require(segment, "using (var enumerator = ", label + " explicit enumerator")
    cursor = require(segment, rebound, label + " pre-MoveNext Count rebound", cursor)
    cursor = require(segment, "if (!enumerator.MoveNext())", label + " MoveNext", cursor)
    # Skip the false/terminal branch deliberately. The rebound after its break is
    # the one protecting a successful MoveNext before caller-controlled Current.
    cursor = require(segment, "break;", label + " terminal branch", cursor)
    cursor = require(segment, rebound, label + " post-successful-MoveNext Count rebound", cursor)
    cursor = require(segment, guard, label + " early Count guard", cursor)
    cursor = require(segment, current, label + " Current read", cursor)
    require(segment, validation, label + " first semantic validation", cursor)


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
require(source, "observedCount >= knownCount.Value", "known-Count overrun predicate", helper)
require(source, "input count changed during enumeration.", "count-drift diagnostic", helper)

object_start = require(source, "private static IReadOnlyList<ProjectElement> OwnedDistinct(", "object-target materializer")
object_end = require(source, "private static void RequireCurrentElementOwnership", "object-target materializer end", object_start)
object_segment = source[object_start:object_end]
require_success_path(
    object_segment,
    "RequireKnownCountStable(elements, knownCount, knownCountSources, \"Bulk edit target collection\");",
    "RequireCanObserveNext(knownCount, inputCount, \"Bulk edit target collection\");",
    "var element = enumerator.Current;",
    "if (element == null)",
    "object-target traversal",
)

id_start = require(source, "private static IReadOnlyList<string> MaterializeBounded", "target-id materializer")
id_end = require(source, "private static int? SnapshotKnownCount", "target-id materializer end", id_start)
id_segment = source[id_start:id_end]
require_success_path(
    id_segment,
    "RequireKnownCountStable(values, knownCount, knownCountSources, label);",
    "RequireCanObserveNext(knownCount, inputCount, label);",
    "var value = enumerator.Current;",
    "result.Add(value);",
    "target-id traversal",
)

if "foreach (var element in elements)" in object_segment:
    fail("caller-controlled object targets must not regress to foreach")
if "foreach (var value in values)" in id_segment:
    fail("caller-controlled target IDs must not regress to foreach")

require(source, "RequireObservedCount(knownCount, inputCount, \"Bulk edit target collection\");", "object under-yield check")
require(source, "RequireObservedCount(knownCount, inputCount, label);", "id under-yield check", id_start)
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
require(smoke, "CurrentReads", "no-extra-Current assertion")
require(smoke, "Equal(1, source.CurrentReads, \"object overrun Current reads\");", "object overrun Current bound")
require(smoke, "Equal(1, source.CurrentReads, \"id overrun Current reads\");", "id overrun Current bound")
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
