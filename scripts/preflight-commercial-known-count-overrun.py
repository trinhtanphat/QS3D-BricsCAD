from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Commercial/CommercialContracts.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/CommercialKnownCountOverrunSmoke.cs").read_text(encoding="utf-8")
legacy_smoke = (ROOT / "tests/QS3D.Core.SmokeTests/CommercialAuditLogCountTraversalSmoke.cs").read_text(encoding="utf-8")
snapshot_legacy_smoke = (ROOT / "tests/QS3D.Core.SmokeTests/CommercialGuardSnapshotCountIntegritySmoke.cs").read_text(encoding="utf-8")

required_source = [
    "CommercialGuard.RequireCanProcessNext(knownCount, snapshot.Count, \"Commercial audit batch source\")",
    "private static IReadOnlyList<T> SnapshotWithAdmittedCount<T>(",
    "RequireCanProcessNext(admittedCount, result.Count, paramName)",
    "internal static void RequireCanProcessNext(int? knownCount, int observedCount, string label)",
    "observedCount >= knownCount.Value",
    "known Count was exceeded during traversal",
    "known Count does not match completed traversal cardinality",
    "var snapshot = SnapshotWithAdmittedCount(source, paramName, maximum, admittedCount);",
]

required_smoke = [
    "AuditBatchOverrunPrecedesUnexpectedRecordValidation",
    "AuditBatchUnderTraversalRemainsFailureAtomic",
    "SourceRevisionOverrunPrecedesUnexpectedItemValidation",
    "SourceRevisionUnderTraversalStillFailsAfterTraversal",
    "HonestCountedAndStreamingInputsRemainAccepted",
    "new MisreportedReadOnlyCollection<CommercialAuditRecord>(1, Record(\"EVENT-1\"), null!)",
    "new MisreportedReadOnlyCollection<CommercialRevisionRef>(1, Revision(\"REV-1\"), null!)",
    "[ModuleInitializer]",
]

required_legacy_smoke = [
    "OverEnumerationRejects",
    "Contains(\"known Count was exceeded during traversal\", error.Message)",
]

required_snapshot_legacy_smoke = [
    "KnownCountMismatchesFailClosed",
    "ExpectInvalidOperation(() => Record(under), \"known Count does not match completed traversal cardinality\")",
    "ExpectInvalidOperation(() => Record(over), \"known Count was exceeded during traversal\")",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
missing += [token for token in required_legacy_smoke if token not in legacy_smoke]
missing += [token for token in required_snapshot_legacy_smoke if token not in snapshot_legacy_smoke]
if missing:
    raise SystemExit("Commercial known-Count overrun preflight failed; missing: " + ", ".join(missing))

# Ordering is the contract: Count overrun must be checked before null/semantic work.
audit_guard = source.index("CommercialGuard.RequireCanProcessNext(knownCount, snapshot.Count")
audit_null = source.index("Commercial audit batch contains a null record")
helper_start = source.index("private static IReadOnlyList<T> SnapshotWithAdmittedCount<T>(")
revision_guard = source.index("RequireCanProcessNext(admittedCount, result.Count, paramName)", helper_start)
revision_null = source.index("contains a null item", revision_guard)
if not audit_guard < audit_null:
    raise SystemExit("Commercial audit known-Count overrun guard must precede record semantic validation.")
if not revision_guard < revision_null:
    raise SystemExit("Commercial snapshot known-Count overrun guard must precede item semantic validation.")

stable_start = source.index("internal static IReadOnlyList<T> SnapshotStableGeneration<T>(")
stable_end = source.index("internal static void RequireCanProcessNext", stable_start)
stable = source[stable_start:stable_end]
if "var snapshot = Snapshot(source, paramName, maximum);" in stable:
    raise SystemExit("Commercial stable-generation snapshot must not re-admit Count before bounded traversal.")

print("PASS commercial known-Count overrun ordering with single admitted-count materialization")
