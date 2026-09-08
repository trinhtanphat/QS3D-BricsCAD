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
    constructor_start = text.find("public CommercialAuditRecord(")
    constructor_end = text.find("public string EventId", constructor_start)
    constructor = text[constructor_start:constructor_end] if constructor_start >= 0 and constructor_end > constructor_start else ""
    snapshot_start = text.find("internal static IReadOnlyList<T> Snapshot<T>(")
    helper_start = text.find("private static IReadOnlyList<T> SnapshotWithAdmittedCount<T>(", snapshot_start)
    stable_snapshot_start = text.find("internal static IReadOnlyList<T> SnapshotStableGeneration<T>(", helper_start)
    snapshot = text[snapshot_start:helper_start] if snapshot_start >= 0 and helper_start > snapshot_start else ""
    helper = text[helper_start:stable_snapshot_start] if helper_start >= 0 and stable_snapshot_start > helper_start else ""
    stable_snapshot_end = text.find("internal static void RequireCanProcessNext", stable_snapshot_start)
    stable_snapshot = text[stable_snapshot_start:stable_snapshot_end] if stable_snapshot_start >= 0 and stable_snapshot_end > stable_snapshot_start else ""

    constructor_required = (
        "SourceRevisions = CommercialGuard.SnapshotStableGeneration(",
        "sourceRevisions,",
        "nameof(sourceRevisions),",
        "64,",
        "CommercialRevisionStateEquals);",
        "private static bool CommercialRevisionStateEquals",
        "string.Equals(left.SourceKind, right.SourceKind, StringComparison.Ordinal)",
        "string.Equals(left.SourceId, right.SourceId, StringComparison.Ordinal)",
        "string.Equals(left.RevisionId, right.RevisionId, StringComparison.Ordinal)",
    )
    cursor = 0
    for token in constructor_required:
        position = constructor.find(token, cursor)
        if position < 0:
            errors.append("CommercialAuditRecord must bind source-revision snapshots to exact semantic equality: " + token)
            break
        cursor = position + len(token)

    snapshot_required = (
        "IEnumerable<T> source,",
        "string paramName,",
        "int maximum)",
        "var admittedCount = SnapshotKnownCount(source, paramName, maximum);",
        "return SnapshotWithAdmittedCount(source, paramName, maximum, admittedCount);",
    )
    cursor = 0
    for token in snapshot_required:
        position = snapshot.find(token, cursor)
        if position < 0:
            errors.append("CommercialGuard.Snapshot missing single-admission wrapper contract token: " + token)
            break
        cursor = position + len(token)
    if "Func<T, T, bool> semanticEquals" in snapshot or "RequireStableSnapshotGeneration(" in snapshot:
        errors.append("CommercialGuard.Snapshot must remain the generic snapshot primitive without semantic replay policy.")

    helper_required = (
        "int? admittedCount)",
        "RequireStableSnapshotKnownCount(source, admittedCount, paramName, maximum);",
        "using (var enumerator = source.GetEnumerator())",
        "RequireStableSnapshotKnownCountDuringTraversal(source, admittedCount, paramName, maximum);",
        "RequireCanProcessNext(admittedCount, result.Count, paramName);",
        "RequireStableSnapshotKnownCount(source, admittedCount, paramName, maximum);",
        "return new ReadOnlyCollection<T>(result.ToArray());",
    )
    cursor = 0
    for token in helper_required:
        position = helper.find(token, cursor)
        if position < 0:
            errors.append("CommercialGuard.SnapshotWithAdmittedCount missing ordered bounded materialization token: " + token)
            break
        cursor = position + len(token)

    stable_required = (
        "Func<T, T, bool> semanticEquals",
        "if (semanticEquals == null) throw new ArgumentNullException(nameof(semanticEquals));",
        "var admittedCount = SnapshotKnownCount(source, paramName, maximum);",
        "var snapshot = SnapshotWithAdmittedCount(source, paramName, maximum, admittedCount);",
        "RequireStableSnapshotKnownCount(source, admittedCount, paramName, maximum);",
        "RequireStableSnapshotGeneration(source, admittedCount, snapshot, semanticEquals, paramName, maximum);",
        "return snapshot;",
    )
    cursor = 0
    for token in stable_required:
        position = stable_snapshot.find(token, cursor)
        if position < 0:
            errors.append("CommercialGuard.SnapshotStableGeneration missing ordered single-admission semantic-generation contract token: " + token)
            break
        cursor = position + len(token)
    if "var snapshot = Snapshot(source, paramName, maximum);" in stable_snapshot:
        errors.append("CommercialGuard.SnapshotStableGeneration must not re-admit Count through Snapshot before semantic replay.")

    replay_start = text.find("private static void RequireStableSnapshotGeneration<T>(")
    replay_end = text.find("private static void RequireStableSnapshotKnownCountDuringTraversal<T>(", replay_start)
    replay = text[replay_start:replay_end] if replay_start >= 0 and replay_end > replay_start else ""
    replay_required = (
        "if (!admittedCount.HasValue || semanticEquals == null)",
        "using (var enumerator = source.GetEnumerator())",
        "RequireStableSnapshotKnownCountDuringTraversal(source, admittedCount, paramName, maximum);",
        "if (!enumerator.MoveNext())",
        "RequireStableSnapshotKnownCountDuringTraversal(source, admittedCount, paramName, maximum);",
        "if (index >= snapshot.Count)",
        "var current = enumerator.Current;",
        "RequireStableSnapshotKnownCountDuringTraversal(source, admittedCount, paramName, maximum);",
        "if (current == null || !semanticEquals(snapshot[index], current))",
        "index++;",
        "if (index != snapshot.Count)",
        "RequireStableSnapshotKnownCount(source, admittedCount, paramName, maximum);",
    )
    cursor = 0
    for token in replay_required:
        position = replay.find(token, cursor)
        if position < 0:
            errors.append("Commercial snapshot replay helper missing ordered token: " + token)
            break
        cursor = position + len(token)

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "CountedSemanticGenerationDriftFailsClosed",
        "CountedEquivalentValueGenerationSucceeds",
        "StreamingSourceRemainsSinglePass",
        "EnumerationCount",
        "ReplayCollection",
        "counted semantic drift must be detected by replaying the admitted generation",
        "pure streaming source must not be replayed",
    ):
        if token not in text:
            errors.append("Commercial snapshot semantic-generation smoke missing token: " + token)

print("QS3D commercial snapshot semantic-generation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: counted commercial snapshots use one Count admission for bounded materialization, then replay semantic generation before immutable publication; streams remain single-pass.")
