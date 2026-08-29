#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Commercial/CommercialContracts.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/CommercialCountStabilitySmoke.cs"
errors = []

for path in (source, smoke):
    if not path.is_file():
        errors.append("missing Commercial Count-stability file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")

    append_start = text.find("public void AppendBatch(IEnumerable<CommercialAuditRecord> records)")
    append_end = text.find("private HashSet<string> ExistingEventIds()", append_start)
    append = text[append_start:append_end] if append_start >= 0 and append_end > append_start else ""
    append_required = (
        "var knownCount = TryGetKnownCount(records",
        "foreach (var record in records)",
        "snapshot.Count != knownCount.Value",
        "RequireStableKnownCount(records, knownCount);",
        "_events.AddRange(snapshot);",
    )
    append_positions = [append.find(token) for token in append_required]
    if not append or any(pos < 0 for pos in append_positions) or append_positions != sorted(append_positions):
        errors.append("CommercialAuditLog.AppendBatch must rebind deterministic Count after exact traversal and before audit publication.")
    if "RequireCanProcessNext(knownCount, snapshot.Count" not in append:
        errors.append("CommercialAuditLog.AppendBatch must reject known-count overrun before retaining an extra record.")

    snapshot_start = text.find("internal static IReadOnlyList<T> Snapshot<T>(")
    snapshot_end = text.find("internal static void RequireCanProcessNext", snapshot_start)
    snapshot = text[snapshot_start:snapshot_end] if snapshot_start >= 0 and snapshot_end > snapshot_start else ""
    snapshot_required = (
        "var knownCount = SnapshotKnownCount(source, paramName, maximum);",
        "foreach (var item in source)",
        "result.Count != knownCount.Value",
        "RequireStableSnapshotKnownCount(source, knownCount, paramName, maximum);",
        "return new ReadOnlyCollection<T>(result.ToArray());",
    )
    snapshot_positions = [snapshot.find(token) for token in snapshot_required]
    if not snapshot or any(pos < 0 for pos in snapshot_positions) or snapshot_positions != sorted(snapshot_positions):
        errors.append("CommercialGuard.Snapshot must rebind Count after exact traversal and before returning the immutable snapshot.")
    if "RequireCanProcessNext(knownCount, result.Count" not in snapshot:
        errors.append("CommercialGuard.Snapshot must preserve fail-closed known-count overrun handling.")

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

print("QS3D Commercial collection Count-stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Commercial audit and snapshot materializers rebind deterministic Count evidence before publication.")
