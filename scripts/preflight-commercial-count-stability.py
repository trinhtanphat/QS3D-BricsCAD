#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Commercial/CommercialContracts.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/CommercialCountStabilitySmoke.cs"
no_overread_smoke = ROOT / "tests/QS3D.Core.SmokeTests/CommercialCountNoOverreadSmoke.cs"
current_smoke = ROOT / "tests/QS3D.Core.SmokeTests/CommercialCurrentCountAcceptanceSmoke.cs"
errors = []


def ordered_positions(segment, tokens):
    positions = []
    cursor = 0
    for token in tokens:
        position = segment.find(token, cursor)
        positions.append(position)
        if position < 0:
            break
        cursor = position + len(token)
    return positions


for path in (source, smoke, no_overread_smoke, current_smoke):
    if not path.is_file():
        errors.append("missing Commercial Count-stability file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")

    append_start = text.find("public void AppendBatch(IEnumerable<CommercialAuditRecord> records)")
    append_end = text.find("private HashSet<string> ExistingEventIds()", append_start)
    append = text[append_start:append_end] if append_start >= 0 and append_end > append_start else ""
    append_required = (
        "var knownCount = TryGetKnownCount(records",
        "using (var enumerator = records.GetEnumerator())",
        "while (true)",
        "RequireStableKnownCountDuringTraversal(records, knownCount);",
        "if (!enumerator.MoveNext())",
        "RequireStableKnownCountDuringTraversal(records, knownCount);",
        "CommercialGuard.RequireCanProcessNext(knownCount, snapshot.Count",
        "var record = enumerator.Current;",
        "RequireStableKnownCountDuringTraversal(records, knownCount);",
        "if (record == null)",
        "snapshot.Count != knownCount.Value",
        "RequireStableKnownCount(records, knownCount);",
        "_events.AddRange(snapshot);",
    )
    append_positions = ordered_positions(append, append_required)
    if not append or len(append_positions) != len(append_required) or any(pos < 0 for pos in append_positions):
        errors.append("CommercialAuditLog.AppendBatch must rebind Count before/after MoveNext and immediately after Current before audit acceptance/publication.")
    if "foreach (var record in records)" in append:
        errors.append("CommercialAuditLog.AppendBatch must not use foreach for caller-controlled counted traversal.")

    snapshot_start = text.find("internal static IReadOnlyList<T> Snapshot<T>(")
    snapshot_end = text.find("internal static void RequireCanProcessNext", snapshot_start)
    snapshot = text[snapshot_start:snapshot_end] if snapshot_start >= 0 and snapshot_end > snapshot_start else ""
    snapshot_required = (
        "var knownCount = SnapshotKnownCount(source, paramName, maximum);",
        "using (var enumerator = source.GetEnumerator())",
        "while (true)",
        "RequireStableSnapshotKnownCountDuringTraversal(source, knownCount, paramName, maximum);",
        "if (!enumerator.MoveNext())",
        "RequireStableSnapshotKnownCountDuringTraversal(source, knownCount, paramName, maximum);",
        "RequireCanProcessNext(knownCount, result.Count",
        "var item = enumerator.Current;",
        "RequireStableSnapshotKnownCountDuringTraversal(source, knownCount, paramName, maximum);",
        "if (item == null)",
        "result.Count != knownCount.Value",
        "RequireStableSnapshotKnownCount(source, knownCount, paramName, maximum);",
        "return new ReadOnlyCollection<T>(result.ToArray());",
    )
    snapshot_positions = ordered_positions(snapshot, snapshot_required)
    if not snapshot or len(snapshot_positions) != len(snapshot_required) or any(pos < 0 for pos in snapshot_positions):
        errors.append("CommercialGuard.Snapshot must rebind Count before/after MoveNext and immediately after Current before item acceptance/immutable return.")
    if "foreach (var item in source)" in snapshot:
        errors.append("CommercialGuard.Snapshot must not use foreach for caller-controlled counted traversal.")

    stable_start = text.find("private static void RequireStableSnapshotKnownCount<T>(")
    stable_end = text.find("private static int? SnapshotKnownCount<T>", stable_start)
    stable = text[stable_start:stable_end] if stable_start >= 0 and stable_end > stable_start else ""
    if "SnapshotKnownCount(source, paramName, maximum)" not in stable or "reboundCount.Value != admittedCount.Value" not in stable:
        errors.append("CommercialGuard post-traversal validation must re-read all supported Count surfaces and compare with admitted evidence.")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "AuditBatchGenericCountDriftFailsAtomically",
        "AuditBatchReadOnlyCountDriftFailsAtomically",
        "AuditBatchNonGenericCountDriftFailsAtomically",
        "AuditBatchNegativePostTraversalCountFailsAtomically",
        "AuditBatchConflictingPostTraversalCountsFailAtomically",
        "AuditBatchUnderYieldFailsAtomically",
        "AuditBatchOverrunFailsAtomically",
        "RevisionSnapshotGenericCountDriftFailsClosed",
        "RevisionSnapshotReadOnlyCountDriftFailsClosed",
        "RevisionSnapshotNonGenericCountDriftFailsClosed",
        "RevisionSnapshotNegativePostTraversalCountFailsClosed",
        "RevisionSnapshotConflictingPostTraversalCountsFailClosed",
        "StableCountedInputsSucceed",
        "StreamingInputsSucceed",
        "failed audit batch mutated the published audit log",
    ):
        if token not in text:
            errors.append("Commercial Count-stability smoke missing regression token: " + token)

if no_overread_smoke.is_file():
    text = no_overread_smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "AuditKnownCountOverrunStopsBeforeUnexpectedCurrent",
        "AuditZeroCountOverrunNeverReadsCurrent",
        "RevisionKnownCountOverrunStopsBeforeUnexpectedCurrent",
        "RevisionZeroCountOverrunNeverReadsCurrent",
        "MoveNextCalls",
        "CurrentReads",
        "ThrowOnUnexpectedCurrent",
    ):
        if token not in text:
            errors.append("Commercial Count no-overread smoke missing regression token: " + token)

if current_smoke.is_file():
    text = current_smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "AuditBatchRejectsCurrentInducedCountDriftBeforeNullAcceptance",
        "RevisionSnapshotRejectsCurrentInducedCountDriftBeforeNullAcceptance",
        "StableCountedControlsRemainAccepted",
        "known Count changed during traversal",
        "reached ordinary item acceptance before Count stability was rebound",
        "CurrentReads",
        "partially publish audit events",
    ):
        if token not in text:
            errors.append("Commercial Current-count acceptance smoke missing regression token: " + token)

print("QS3D Commercial collection Count-stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Commercial audit and snapshot materializers rebind Count before/after MoveNext and after Current before semantic acceptance/publication.")
