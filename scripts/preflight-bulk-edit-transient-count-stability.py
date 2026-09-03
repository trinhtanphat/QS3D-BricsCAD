#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/BulkEditService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BulkEditTransientCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/bulk-edit-transient-count-stability.md"


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, marker: str, label: str) -> int:
    index = text.find(marker)
    if index < 0:
        fail(f"missing {label}: {marker}")
    return index


for path, label in ((SOURCE, "production source"), (SMOKE, "transient smoke"), (RUNBOOK, "runbook")):
    if not path.is_file():
        fail(f"missing {label}: {path.relative_to(ROOT)}")

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

for token in (
    "SnapshotKnownCount(elements, \"Bulk edit target collection\", out var knownCountSources)",
    "SnapshotKnownCount(values, label, out var knownCountSources)",
    "private static int? SnapshotKnownCount<T>(IEnumerable<T> values, string label, out int knownCountSources)",
    "private static void RequireKnownCountStable<T>(IEnumerable<T> values, int? expectedKnownCount, int expectedKnownCountSources, string label)",
    "expectedKnownCount != currentKnownCount || expectedKnownCountSources != currentKnownCountSources",
    "if (genericCount.HasValue) knownCountSources |= 1;",
    "if (readOnlyCount.HasValue) knownCountSources |= 2;",
    "if (nonGenericCount.HasValue) knownCountSources |= 4;",
):
    require(source, token, "Count stability contract")


def require_order(segment: str, markers: tuple[str, ...], label: str) -> None:
    cursor = -1
    for marker in markers:
        position = segment.find(marker, cursor + 1)
        if position < 0:
            fail(f"{label} missing ordered marker: {marker}")
        if position <= cursor:
            fail(f"{label} marker is out of order: {marker}")
        cursor = position


object_start = require(source, "private static IReadOnlyList<ProjectElement> OwnedDistinct(", "object materializer")
object_end = require(source[object_start:], "private static void RequireCurrentElementOwnership", "object materializer end") + object_start
object_segment = source[object_start:object_end]
require_order(
    object_segment,
    (
        "using (var enumerator = elements.GetEnumerator())",
        "RequireKnownCountStable(elements, knownCount, knownCountSources, \"Bulk edit target collection\");",
        "if (!enumerator.MoveNext())",
        "RequireKnownCountStable(elements, knownCount, knownCountSources, \"Bulk edit target collection\");",
        "RequireCanObserveNext(knownCount, inputCount, \"Bulk edit target collection\");",
        "var element = enumerator.Current;",
    ),
    "object-target traversal",
)

id_start = require(source, "private static IReadOnlyList<string> MaterializeBounded", "id materializer")
id_end = require(source[id_start:], "private static int? SnapshotKnownCount", "id materializer end") + id_start
id_segment = source[id_start:id_end]
require_order(
    id_segment,
    (
        "using (var enumerator = values.GetEnumerator())",
        "RequireKnownCountStable(values, knownCount, knownCountSources, label);",
        "if (!enumerator.MoveNext())",
        "RequireKnownCountStable(values, knownCount, knownCountSources, label);",
        "RequireCanObserveNext(knownCount, inputCount, label);",
        "var value = enumerator.Current;",
    ),
    "target-id traversal",
)

if "foreach (var element in elements)" in object_segment or "foreach (var value in values)" in id_segment:
    fail("caller-controlled bulk-edit traversal must not use foreach")

for method in (
    "RejectObjectTransientGrowthBeforeSecondMoveNext",
    "RejectIdTransientShrinkBeforeSecondMoveNext",
    "RejectObjectTransientNegativeCountBeforeSecondMoveNext",
    "RejectIdTransientConflictingCountsBeforeSecondMoveNext",
    "PreserveStableMultiInterfaceObjectTargets",
    "PreserveStableMultiInterfaceIdTargets",
):
    require(smoke, method, f"smoke regression {method}")

for token in (
    "ICollection<T>, IReadOnlyCollection<T>, ICollection",
    "MoveNextCalls",
    "CurrentReads",
    "Equal(1, source.MoveNextCalls",
    "Equal(1, source.CurrentReads",
):
    require(smoke, token, "hostile Count fixture contract")

for phrase in (
    "before every MoveNext",
    "after every successful MoveNext",
    "before IEnumerator.Current",
    "transient growth",
    "transient shrink",
    "negative Count",
    "conflicting Count",
    "streaming",
    "NOT_APPLICABLE",
):
    require(runbook, phrase, f"runbook contract {phrase}")

print("PASS: Bulk edit transient known-Count stability guard")
