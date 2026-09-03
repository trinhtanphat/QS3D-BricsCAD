#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Commercial/CommercialContracts.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/commercial-audit-known-count-stability.md"


def fail(message: str) -> None:
    print("ERROR: " + message, file=sys.stderr)
    raise SystemExit(1)


def require(text: str, marker: str, label: str) -> int:
    index = text.find(marker)
    if index < 0:
        fail(label + " missing marker: " + marker)
    return index


def require_order(segment: str, markers: tuple[str, ...], label: str) -> None:
    cursor = -1
    for marker in markers:
        position = segment.find(marker, cursor + 1)
        if position < 0:
            fail(label + " missing ordered marker: " + marker)
        cursor = position


for path, label in ((SOURCE, "production source"), (RUNBOOK, "runbook")):
    if not path.is_file():
        fail("missing " + label + ": " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

append_start = require(source, "public void AppendBatch(IEnumerable<CommercialAuditRecord> records)", "AppendBatch")
append_end = require(source[append_start:], "private HashSet<string> ExistingEventIds()", "AppendBatch end") + append_start
append = source[append_start:append_end]
require_order(append, (
    "using (var enumerator = records.GetEnumerator())",
    "while (true)",
    "RequireStableKnownCountDuringTraversal(records, knownCount);",
    "if (!enumerator.MoveNext())",
    "RequireStableKnownCountDuringTraversal(records, knownCount);",
    "CommercialGuard.RequireCanProcessNext(knownCount, snapshot.Count, \"Commercial audit batch source\");",
    "var record = enumerator.Current;",
), "Commercial audit batch traversal")

snapshot_start = require(source, "internal static IReadOnlyList<T> Snapshot<T>(IEnumerable<T> source, string paramName, int maximum)", "CommercialGuard.Snapshot")
snapshot_end = require(source[snapshot_start:], "internal static void RequireCanProcessNext", "CommercialGuard.Snapshot end") + snapshot_start
snapshot = source[snapshot_start:snapshot_end]
require_order(snapshot, (
    "using (var enumerator = source.GetEnumerator())",
    "while (true)",
    "RequireStableSnapshotKnownCountDuringTraversal(source, knownCount, paramName, maximum);",
    "if (!enumerator.MoveNext())",
    "RequireStableSnapshotKnownCountDuringTraversal(source, knownCount, paramName, maximum);",
    "RequireCanProcessNext(knownCount, result.Count, paramName);",
    "var item = enumerator.Current;",
), "Commercial snapshot traversal")

for phrase in (
    "before every MoveNext",
    "after every successful MoveNext",
    "before Current",
    "transient Count drift",
    "streaming inputs",
    "NOT_APPLICABLE",
):
    require(runbook, phrase, "runbook contract")

print("PASS: commercial audit traversal-wide known-Count stability guard")
