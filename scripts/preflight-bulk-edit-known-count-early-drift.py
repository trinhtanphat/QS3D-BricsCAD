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
object_using = require(object_segment, "using (var enumerator = elements.GetEnumerator())", "object explicit enumerator")
object_pre = require(object_segment, "RequireKnownCountStable(elements, knownCount, knownCountSources, \"Bulk edit target collection\");", "object pre-MoveNext Count rebound", object_using)
object_move = require(object_segment, "if (!enumerator.MoveNext())", "object MoveNext", object_pre)
object_post = require(object_segment, "RequireKnownCountStable(elements, knownCount, knownCountSources, \"Bulk edit target collection\");", "object post-MoveNext Count rebound", object_move + 1)
object_guard = require(object_segment, "RequireCanObserveNext(knownCount, inputCount, \"Bulk edit target collection\");", "object early Count guard", object_post)
object_current = require(object_segment, "var element = enumerator.Current;", "object Current read", object_guard)
object_null = require(object_segment, "if (element == null)", "object null validation", object_current)
if not (object_using < object_pre < object_move < object_post < object_guard < object_current < object_null):
    fail("object-target traversal must reject Count overrun before reading unexpected IEnumerator.Current and before target validation")

id_start = require(source, "private static IReadOnlyList<string> MaterializeBounded", "target-id materializer")
id_end = require(source, "private static int? SnapshotKnownCount", "target-id materializer end", id_start)
id_segment = source[id_start:id_end]
id_using = require(id_segment, "using (var enumerator = values.GetEnumerator())", "id explicit enumerator")
id_pre = require(id_segment, "RequireKnownCountStable(values, knownCount, knownCountSources, label);", "id pre-MoveNext Count rebound", id_using)
id_move = require(id_segment, "if (!enumerator.MoveNext())", "id MoveNext", id_pre)
id_post = require(id_segment, "RequireKnownCountStable(values, knownCount, knownCountSources, label);", "id post-MoveNext Count rebound", id_move + 1)
id_guard = require(id_segment, "RequireCanObserveNext(knownCount, inputCount, label);", "id early Count guard", id_post)
id_current = require(id_segment, "var value = enumerator.Current;", "id Current read", id_guard)
id_append = require(id_segment, "result.Add(value);", "id append", id_current)
if not (id_using < id_pre < id_move < id_post < id_guard < id_current < id_append):
    fail("target-id traversal must reject Count overrun before reading/materializing unexpected IEnumerator.Current")

if "foreach (var element in elements)" in source:
    fail("caller-controlled object targets must not regress to foreach")
if "foreach (var value in values)" in source:
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
