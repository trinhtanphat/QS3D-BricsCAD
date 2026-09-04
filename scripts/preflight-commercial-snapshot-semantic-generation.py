#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Commercial/CommercialContracts.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/CommercialSnapshotSemanticGenerationSmoke.cs"
runbook = ROOT / "docs/FEATURE-RUNBOOKS/commercial-snapshot-semantic-generation.md"
errors = []

for path in (source, smoke, runbook):
    if not path.is_file():
        errors.append("missing commercial snapshot semantic-generation file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    constructor = text[text.find("public CommercialAuditRecord("):text.find("public string EventId", text.find("public CommercialAuditRecord("))]
    snapshot_start = text.find("internal static IReadOnlyList<T> Snapshot<T>(")
    snapshot_end = text.find("internal static void RequireCanProcessNext", snapshot_start)
    snapshot = text[snapshot_start:snapshot_end] if snapshot_start >= 0 and snapshot_end > snapshot_start else ""

    for token in (
        "CommercialRevisionStateEquals",
        "CommercialGuard.Snapshot(sourceRevisions, nameof(sourceRevisions), 64, CommercialRevisionStateEquals)",
    ):
        if token not in constructor and token not in text:
            errors.append("CommercialAuditRecord must bind source-revision snapshots to semantic equality: " + token)

    required = (
        "Func<T, T, bool> semanticEquals",
        "RequireStableSnapshotKnownCount(source, knownCount, paramName, maximum);",
        "RequireStableSnapshotGeneration(source, knownCount, result, semanticEquals, paramName, maximum);",
        "return new ReadOnlyCollection<T>(result.ToArray());",
    )
    cursor = 0
    for token in required:
        position = snapshot.find(token, cursor)
        if position < 0:
            errors.append("CommercialGuard.Snapshot missing ordered semantic-generation contract token: " + token)
            break
        cursor = position + len(token)

    helper_start = text.find("private static void RequireStableSnapshotGeneration<T>(")
    helper_end = text.find("private static void RequireStableSnapshotKnownCountDuringTraversal<T>(", helper_start)
    helper = text[helper_start:helper_end] if helper_start >= 0 and helper_end > helper_start else ""
    for token in (
        "if (!admittedCount.HasValue || semanticEquals == null)",
        "using (var enumerator = source.GetEnumerator())",
        "RequireStableSnapshotKnownCountDuringTraversal(source, admittedCount, paramName, maximum);",
        "if (!enumerator.MoveNext())",
        "if (index >= snapshot.Count)",
        "var current = enumerator.Current;",
        "if (current == null || !semanticEquals(snapshot[index], current))",
        "if (index != snapshot.Count)",
        "RequireStableSnapshotKnownCount(source, admittedCount, paramName, maximum);",
    ):
        if token not in helper:
            errors.append("Commercial snapshot replay helper missing token: " + token)

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "CountedSemanticGenerationDriftFailsClosed",
        "CountedEquivalentValueGenerationSucceeds",
        "StreamingSourceRemainsSinglePass",
        "EnumerationCount",
        "ReplayCollection",
    ):
        if token not in text:
            errors.append("Commercial snapshot semantic-generation smoke missing token: " + token)

print("QS3D commercial snapshot semantic-generation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: counted commercial source-revision snapshots replay semantic generation before immutable publication while streams remain one-pass.")
