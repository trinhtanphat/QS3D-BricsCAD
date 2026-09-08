from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Commercial/CommercialContracts.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/CommercialTransientKnownCountStabilitySmoke.cs").read_text(encoding="utf-8")
legacy_guard = (ROOT / "scripts/preflight-commercial-known-count-overrun.py").read_text(encoding="utf-8")

required_smoke = [
    "AuditBatchTransientGrowthRejectsBeforeCurrent",
    "SourceRevisionTransientNegativeRejectsBeforeCurrent",
    "SourceRevisionTransientConflictRejectsBeforeCurrent",
    "StableMultiSurfaceCountRemainsAccepted",
    "CurrentReads",
    "[ModuleInitializer]",
]

required_source = [
    "RequireStableKnownCountDuringTraversal(records, knownCount)",
    "private static IReadOnlyList<T> SnapshotWithAdmittedCount<T>(",
    "RequireStableSnapshotKnownCountDuringTraversal(source, admittedCount, paramName, maximum)",
    "while (true)",
    "if (!enumerator.MoveNext())",
    "CommercialGuard.RequireCanProcessNext(knownCount, snapshot.Count, \"Commercial audit batch source\")",
    "RequireCanProcessNext(admittedCount, result.Count, paramName)",
    "var snapshot = SnapshotWithAdmittedCount(source, paramName, maximum, admittedCount);",
]

missing = [token for token in required_smoke if token not in smoke]
missing += [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Commercial transient known-Count stability preflight failed; missing: " + ", ".join(missing))

# Strong ordering contract: traversal-time Count must be rebound before MoveNext,
# rebound again after every successful MoveNext, then checked for overrun before Current.
audit_method = source.index("public void AppendBatch")
audit_loop = source.index("while (true)", audit_method)
audit_pre = source.index("RequireStableKnownCountDuringTraversal(records, knownCount)", audit_loop)
audit_move = source.index("if (!enumerator.MoveNext())", audit_pre)
audit_post = source.index("RequireStableKnownCountDuringTraversal(records, knownCount)", audit_move)
audit_overrun = source.index("CommercialGuard.RequireCanProcessNext(knownCount, snapshot.Count", audit_post)
audit_current = source.index("var record = enumerator.Current", audit_overrun)
if not audit_loop < audit_pre < audit_move < audit_post < audit_overrun < audit_current:
    raise SystemExit("Commercial audit traversal must rebind Count before and after MoveNext, then guard before semantic Current.")

snapshot_method = source.index("private static IReadOnlyList<T> SnapshotWithAdmittedCount<T>(")
snapshot_loop = source.index("while (true)", snapshot_method)
snapshot_pre = source.index("RequireStableSnapshotKnownCountDuringTraversal(source, admittedCount, paramName, maximum)", snapshot_loop)
snapshot_move = source.index("if (!enumerator.MoveNext())", snapshot_pre)
snapshot_post = source.index("RequireStableSnapshotKnownCountDuringTraversal(source, admittedCount, paramName, maximum)", snapshot_move)
snapshot_overrun = source.index("RequireCanProcessNext(admittedCount, result.Count, paramName)", snapshot_post)
snapshot_current = source.index("var item = enumerator.Current", snapshot_overrun)
if not snapshot_loop < snapshot_pre < snapshot_move < snapshot_post < snapshot_overrun < snapshot_current:
    raise SystemExit("Commercial shared snapshot traversal must rebind the originally admitted Count before and after MoveNext, then guard before semantic Current.")

stable_method = source.index("internal static IReadOnlyList<T> SnapshotStableGeneration<T>(")
stable_end = source.index("internal static void RequireCanProcessNext", stable_method)
stable = source[stable_method:stable_end]
if "var snapshot = Snapshot(source, paramName, maximum);" in stable:
    raise SystemExit("Commercial stable-generation snapshot must not perform a second Count admission.")

# Historical N+1/null precedence must remain explicitly pinned.
for token in [
    "Commercial audit known-Count overrun guard must precede record semantic validation",
    "Commercial snapshot known-Count overrun guard must precede item semantic validation",
]:
    if token not in legacy_guard:
        raise SystemExit("Historical commercial known-count guard was weakened; missing: " + token)

print("PASS commercial transient known-Count stability source contract with single admitted-count traversal")
